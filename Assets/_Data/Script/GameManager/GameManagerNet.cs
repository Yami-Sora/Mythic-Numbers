using Fusion;
using TMPro;
using UnityEngine;

public class GameManagerNet : NetworkBehaviour
{
    public static GameManagerNet Instance;

    [Header("References")]
    public NetworkObject cardPrefab;
    public Transform[] slots;      // Kéo 9 cái Slot_0 -> Slot_8 vào
    public Transform leftHandPos;  // Panel chứa bài địch
    public Transform rightHandPos; // Panel chứa bài mình
    public TMP_Text turnText;          // UI hiển thị lượt ai

    [Networked] public int CurrentTurn { get; set; } // 0 hoặc 1
    [Networked, Capacity(9)] public NetworkArray<NetworkId> BoardState { get; } // Lưu ID bài trên bàn

    private CardNet selectedLocalCard; // Bài đang chọn ở máy local

    private void Awake()
    {
        if (Instance != null) Debug.LogError("GameManagerNet.Instance already exist!");
        Instance = this;
    }

    public override void Spawned()
    {
        // --- TỰ ĐỘNG TÌM UI (AUTO SETUP) ---
        // Cách này giúp bạn không bao giờ bị mất liên kết khi dùng Prefab

        // 1. Tìm 9 ô Slot (Giả sử bạn đặt tên là Slot_0 đến Slot_8)
        slots = new Transform[9];
        for (int i = 0; i < 9; i++)
        {
            // Tìm object theo tên trong Scene
            GameObject slotObj = GameObject.Find("Slot_" + i);
            if (slotObj != null) slots[i] = slotObj.transform;
            else Debug.LogError($"Không tìm thấy Slot_{i}. Hãy kiểm tra lại tên!");
        }

        // 2. Tìm Hand Panel
        GameObject pLeft = GameObject.Find("Panel_Left");
        if (pLeft) leftHandPos = pLeft.transform;

        GameObject pRight = GameObject.Find("Panel_Right");
        if (pRight) rightHandPos = pRight.transform;

        // Chỉ Host (Server) mới được chia bài lúc đầu
        if (Object.HasStateAuthority)
        {
            CurrentTurn = 0; // Player 1 đi trước
            DealCards();
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Cập nhật UI Lượt đi
        if (turnText)
            turnText.text = (CurrentTurn == 0) ? "PLAYER 1 TURN (BLUE)" : "PLAYER 2 TURN (RED)";
    }

    void DealCards()
    {
        // Chia 5 lá cho P1 (ID 0) và P2 (ID 1)
        for (int i = 0; i < 5; i++)
        {
            SpawnCard(0, i);
            SpawnCard(1, i);
        }
    }

    void SpawnCard(int ownerID, int index)
    {
        // Tạo số ngẫu nhiên 1-9
        int t = Random.Range(1, 10);
        int r = Random.Range(1, 10);
        int b = Random.Range(1, 10);
        int l = Random.Range(1, 10);

        var no = Runner.Spawn(cardPrefab, Vector3.zero, Quaternion.identity);

        CardNet card = no.GetComponent<CardNet>();
        card.Top = t; card.Right = r; card.Bottom = b; card.Left = l;
        card.OwnerID = ownerID;
        card.HandIndex = index;

        // Gán quyền Input cho đúng người chơi để họ click được
        // (Lưu ý: Cần mapping PlayerRef chuẩn trong Photon, đây là code đơn giản hóa)
        //no.AssignInputAuthority(Runner.ActivePlayers.GetEnumerator().Current);
    }

    // --- LOGIC CLIENT (Chọn bài) ---
    public void SelectCard(CardNet card)
    {
        // Kiểm tra có đúng lượt của mình không
        // (Logic kiểm tra ID người chơi local so với CurrentTurn)
        // Tạm thời giả định PlayerRef.Local tương ứng với OwnerID để test

        // Tắt highlight con cũ (nếu có)
        if (selectedLocalCard != null)
        {
            selectedLocalCard.SetHighlight(false);
        }

        // Bật highlight con mới
        selectedLocalCard = card;
        if (selectedLocalCard != null)
        {
            selectedLocalCard.SetHighlight(true);
        }

        Debug.Log("Đã chọn bài: " + card.Top + "/" + card.Right);
    }

    // Sự kiện Click vào ô bàn cờ (Gán vào Button của Slot)
    public void OnSlotClicked(int slotIndex)
    {
        if (selectedLocalCard == null) return;
        // Tắt highlight trước khi gửi RPC
        selectedLocalCard.SetHighlight(false);

        // Gửi yêu cầu đánh bài lên Server (RPC)
        RPC_PlayCard(selectedLocalCard.Object.Id, slotIndex);
        selectedLocalCard = null;
    }

    // --- LOGIC SERVER (Xử lý đánh bài & Lật) ---
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PlayCard(NetworkId cardId, int slotIndex, RpcInfo info = default)
    {
        // 1. Kiểm tra xem ô đó đã có bài chưa
        if (BoardState[slotIndex].IsValid) return;

        // 2. Cập nhật dữ liệu bàn cờ
        BoardState.Set(slotIndex, cardId);

        // 3. Tìm object bài dựa trên NetworkId
        NetworkObject cardObj = Runner.FindObject(cardId);
        if (cardObj != null)
        {
            CardNet card = cardObj.GetComponent<CardNet>();
            card.HandIndex = -1; // Đánh dấu đã đánh xuống bàn

            // 4. Xử lý Lật Bài
            ResolveBattle(card, slotIndex);
        }
        // --- GỌI TRỌNG TÀI KIỂM TRA ---
        if (GameRefereeNet.Instance != null)
        {
            GameRefereeNet.Instance.CheckEndGame();
        }

        // 5. Đổi lượt
        CurrentTurn = 1 - CurrentTurn;
    }

    void ResolveBattle(CardNet playedCard, int index)
    {
        int row = index / 3;
        int col = index % 3;

        // Định nghĩa láng giềng: [Index, MyStat, EnemySide, R, C]
        // Logic y hệt bản Web TypeScript
        CheckNeighbor(index - 3, playedCard.Top, "Bottom", row - 1, col, playedCard.OwnerID);   // Top
        CheckNeighbor(index + 1, playedCard.Right, "Left", row, col + 1, playedCard.OwnerID);   // Right
        CheckNeighbor(index + 3, playedCard.Bottom, "Top", row + 1, col, playedCard.OwnerID);   // Bottom
        CheckNeighbor(index - 1, playedCard.Left, "Right", row, col - 1, playedCard.OwnerID);   // Left
    }

    void CheckNeighbor(int nIdx, int myStat, string enemySide, int r, int c, int myOwner)
    {
        // Check biên
        if (nIdx < 0 || nIdx >= 9 || r < 0 || r > 2 || c < 0 || c > 2) return;

        // Check bài
        NetworkId nId = BoardState[nIdx];
        if (!nId.IsValid) return; // Ô trống

        CardNet enemy = Runner.FindObject(nId).GetComponent<CardNet>();
        if (enemy.OwnerID == myOwner) return; // Cùng phe

        // Lấy chỉ số địch
        int enemyStat = 0;
        if (enemySide == "Top") enemyStat = enemy.Top;
        if (enemySide == "Bottom") enemyStat = enemy.Bottom;
        if (enemySide == "Left") enemyStat = enemy.Left;
        if (enemySide == "Right") enemyStat = enemy.Right;

        // So sánh & Lật
        if (myStat > enemyStat)
        {
            enemy.FlipOwner();
            Debug.Log("LẬT BÀI!");
        }
    }
    public int GetLocalPlayerID()
    {
        // Nếu mình là chủ phòng (Master Client) -> Là Player 1 (ID 0)
        // Nếu không -> Là Player 2 (ID 1)
        // Lưu ý: Đây là logic đơn giản cho 2 người. 
        //return Runner.IsSharedModeMasterClient ? 0 : 1;
         return CurrentTurn;
    }
    // --- LOGIC RESTART GAME (SOFT RESET) ---

    // 1. Hàm này được gọi từ nút Restart (AppController)
    public void RequestRestart()
    {
        // Gửi yêu cầu lên Server (chỉ Server mới có quyền Reset)
        RPC_Restart();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Restart()
    {
        Debug.Log("Server đang tiến hành Restart game...");

        // A. Xóa toàn bộ bài đang có trên tay và trên bàn
        // Tìm tất cả object có script CardNet
        CardNet[] allCards = FindObjectsByType<CardNet>(FindObjectsSortMode.None);
        foreach (var card in allCards)
        {
            // Dùng Runner.Despawn để xóa object qua mạng an toàn
            Runner.Despawn(card.Object);
        }

        // B. Reset dữ liệu bàn cờ về rỗng
        for (int i = 0; i < 9; i++)
        {
            BoardState.Set(i, default); // Xóa ID bài lưu trong ô
        }

        // C. Reset lượt đi và người thắng
        CurrentTurn = 0;
        // Nếu có biến Winner thì reset ở đây luôn

        // D. Chia bài mới
        DealCards();
    }
}