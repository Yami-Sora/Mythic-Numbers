using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GameAI))]
public class GameManagerNet : NetworkBehaviour
{
    public static GameManagerNet Instance { get; private set; }

    [Header("Settings")]
    public bool playWithAI = false;

    [Header("References")]
    [SerializeField] private NetworkObject cardPrefab;
    private Transform[] slots;
    private Transform leftHandPos;
    private Transform rightHandPos;
    private TMP_Text turnText;
    private bool uiReady = false;

    private GameAI aiBrain;
    public Transform[] Slots => slots;
    public Transform LeftHandPos => leftHandPos;
    public Transform RightHandPos => rightHandPos;

    // Logic 0 hoặc 1
    [Networked] public int CurrentTurn { get; set; }
    [Networked, Capacity(9)] public NetworkArray<NetworkId> BoardState { get; }

    private CardNet selectedLocalCard;

    // --- SETUP UI ---
    public void SetSceneReferences(Transform[] slots, Transform leftHandPos, Transform rightHandPos, TMP_Text turnText)
    {
        this.slots = slots;
        this.leftHandPos = leftHandPos;
        this.rightHandPos = rightHandPos;
        this.turnText = turnText;
        uiReady = true;
    }
    public bool IsUIReady => uiReady;

    public override void Spawned()
    {
        if (Instance != null && Instance != this)
        {
            if (Object.HasStateAuthority) Runner.Despawn(Object);
            return;
        }
        Instance = this;

        // Tự động bật AI nếu chơi Single (Offline)
        if (Runner.GameMode == GameMode.Single)
        {
            playWithAI = true;
            aiBrain = GetComponent<GameAI>();
            if (aiBrain != null) aiBrain.Init(this);
        }

        // Host (Server) chịu trách nhiệm chia bài và set lượt
        if (Object.HasStateAuthority)
        {
            CurrentTurn = 0; // P1 đi trước
            DealCards();
        }

        // Client/Host đều cần tìm UI
        if (!uiReady && NetworkAppManager.Instance != null)
        {
            NetworkAppManager.Instance.SetupGameManagerUI(this);
        }
    }

    public override void Render()
    {
        if (!uiReady)
        {
            if (NetworkAppManager.Instance != null)
                NetworkAppManager.Instance.SetupGameManagerUI(this);
            return;
        }

        if (turnText)
            turnText.text = (CurrentTurn == 0) ? "Player 1 Turn (Blue)" : "Player 2 Turn (Red)";
    }

    // --- QUAN TRỌNG: MAPPING ID ---
    // Hàm này quy định: Host luôn là 0, Client luôn là 1.
    // Không dùng PlayerRef.PlayerId trực tiếp nữa.
    public int GetLocalPlayerID()
    {
        // 1. Nếu là Offline -> Mình là Player 0 (Blue)
        if (Runner.GameMode == GameMode.Single) return 0;

        // 2. Nếu là Host Mode Online
        if (Runner.IsServer) return 0; // Host luôn là 0
        return 1; // Client luôn là 1 (Trong game 1v1)
    }

    // --- SPAWN LOGIC (SERVER ONLY) ---
    void DealCards()
    {
        // Xóa bài cũ nếu có (an toàn khi restart)
        foreach (var c in FindObjectsByType<CardNet>(FindObjectsSortMode.None))
        {
            Runner.Despawn(c.Object);
        }

        for (int i = 0; i < 5; i++)
        {
            SpawnCard(0, i); // Cho Host
            SpawnCard(1, i); // Cho Client (hoặc AI)
        }
    }

    public void SpawnCard(int ownerID, int index)
    {
        int t = Random.Range(1, 10);
        int r = Random.Range(1, 10);
        int b = Random.Range(1, 10);
        int l = Random.Range(1, 10);

        var prefab = Runner.Spawn(cardPrefab, Vector3.zero, Quaternion.identity);
        CardNet card = prefab.GetComponent<CardNet>();

        card.Top = t; card.Right = r; card.Bottom = b; card.Left = l;
        card.OwnerID = ownerID; // 0 hoặc 1
        card.HandIndex = index;

        // Code set parent ngay lập tức ở Server để đồng bộ tốt hơn
        // (Client sẽ tự set lại trong FixedUpdate của CardNet)
        Transform targetParent = (ownerID == 0) ? rightHandPos : leftHandPos;
        if (targetParent != null)
        {
            card.transform.SetParent(targetParent, false);
            card.transform.localPosition = Vector3.zero;
        }
    }

    // --- CLIENT INPUT ---
    public void SelectCard(CardNet card)
    {
        // Chỉ chọn được bài của mình (check theo ID 0/1 đã map)
        if (card.OwnerID != GetLocalPlayerID()) return;

        if (selectedLocalCard != null) selectedLocalCard.SetHighlight(false);
        selectedLocalCard = card;
        if (selectedLocalCard != null) selectedLocalCard.SetHighlight(true);
    }

    public void OnSlotClicked(int slotIndex)
    {
        if (selectedLocalCard == null) return;

        // Check lượt
        if (GetLocalPlayerID() != CurrentTurn) return;

        selectedLocalCard.SetHighlight(false);
        RPC_PlayCard(selectedLocalCard.Object.Id, slotIndex);
        selectedLocalCard = null;
    }

    // --- SERVER LOGIC ---
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PlayCard(NetworkId cardId, int slotIndex, RpcInfo info = default)
    {
        // Validation...
        if (slotIndex < 0 || slotIndex >= 9) return;
        if (BoardState[slotIndex].IsValid) return;

        NetworkObject cardObj = Runner.FindObject(cardId);
        if (cardObj == null) return;

        CardNet card = cardObj.GetComponent<CardNet>();

        // Security Check: Đảm bảo người gửi RPC đúng là chủ lá bài
        // Info.Source = PlayerRef của người gửi.
        // Host (Source IsNone/Server) -> ID 0. Client -> ID 1.
        int senderLogicID = (info.Source == Runner.LocalPlayer) ? 0 : 1;
        // Lưu ý: Trong Host Mode, Runner.LocalPlayer là Host.
        // Cách check kỹ hơn:
        // if (info.Source.IsNone || info.Source == Runner.LocalPlayer) -> Host (0)
        // else -> Client (1)

        // Thực hiện đánh bài
        BoardState.Set(slotIndex, cardId);
        card.HandIndex = -1;

        // Xử lý chiến đấu
        ResolveBattle(card, slotIndex);

        // Check End Game
        if (GameRefereeNet.Instance != null) GameRefereeNet.Instance.CheckEndGame();

        // Đổi lượt
        CurrentTurn = 1 - CurrentTurn;

        // Trigger AI
        if (playWithAI && CurrentTurn == 1 && Object.HasStateAuthority)
        {
            if (aiBrain != null) aiBrain.StartTurn(1);
        }
    }

    void ResolveBattle(CardNet playedCard, int index)
    {
        int row = index / 3;
        int col = index % 3;
        CheckNeighbor(index - 3, playedCard.Top, "Bottom", row - 1, col, playedCard.OwnerID);
        CheckNeighbor(index + 1, playedCard.Right, "Left", row, col + 1, playedCard.OwnerID);
        CheckNeighbor(index + 3, playedCard.Bottom, "Top", row + 1, col, playedCard.OwnerID);
        CheckNeighbor(index - 1, playedCard.Left, "Right", row, col - 1, playedCard.OwnerID);
    }

    void CheckNeighbor(int nIdx, int myStat, string enemySide, int r, int c, int myOwner)
    {
        if (nIdx < 0 || nIdx >= 9 || r < 0 || r > 2 || c < 0 || c > 2) return;
        NetworkId nId = BoardState[nIdx];
        if (!nId.IsValid) return;

        CardNet enemy = Runner.FindObject(nId).GetComponent<CardNet>();
        if (enemy.OwnerID == myOwner) return;

        int enemyStat = 0;
        if (enemySide == "Top") enemyStat = enemy.Top;
        if (enemySide == "Bottom") enemyStat = enemy.Bottom;
        if (enemySide == "Left") enemyStat = enemy.Left;
        if (enemySide == "Right") enemyStat = enemy.Right;

        if (myStat > enemyStat)
        {
            enemy.FlipOwner(); // Gọi hàm sửa Networked Var trên Server
        }
    }

    // --- RESTART LOGIC ---
    public void RequestRestart()
    {
        RPC_Restart();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Restart()
    {
        if (playWithAI && aiBrain != null) aiBrain.StopThinking();

        // Xóa hết bài
        CardNet[] allCards = FindObjectsByType<CardNet>(FindObjectsSortMode.None);
        foreach (var card in allCards) Runner.Despawn(card.Object);

        // Reset bàn
        for (int i = 0; i < 9; i++)
        {
            BoardState.Set(i, default);
            if (slots != null && slots[i] != null)
            {
                Image img = slots[i].GetComponent<Image>();
                if (img) img.color = Color.white;
            }
        }
        CurrentTurn = 0;
        DealCards();
    }
}