using System.Collections.Generic;
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

    [Header("UI Effects")]
    [SerializeField] private GameObject floatingTextPrefab;
    // Canvas chứa các hiệu ứng bay (được gán từ NetworkAppManager)
    private Transform effectsCanvas;

    private Transform[] slots;      // Mảng 9 vị trí ô trên bàn cờ (3x3)
    private Transform leftHandPos;  // Vị trí tay trái (thường cho Player 2 / Client)
    private Transform rightHandPos; // Vị trí tay phải (thường cho Player 1 / Host)
    private TMP_Text turnText;

    private bool uiReady = false;

    private GameAI aiBrain;
    private ChangeDetector _changes;

    public Transform[] Slots => slots;
    public Transform LeftHandPos => leftHandPos;
    public Transform RightHandPos => rightHandPos;
    public bool IsUIReady => uiReady;

    [Networked] public int CurrentTurn { get; set; }

    [Networked, Capacity(9)] public NetworkArray<NetworkId> BoardState { get; }

    // Biến đồng bộ luật chơi (0: Normal, 1: Reverse...)
    [Networked] public int CurrentRuleIndex { get; set; }

    private CardNet selectedLocalCard;

    // --- STRATEGY PATTERN (QUẢN LÝ LUẬT) ---
    private IRuleSet currentStrategy;

    // Danh sách các luật có sẵn trong game
    private List<IRuleSet> allRules = new List<IRuleSet>()
    {
        new NormalRule(),
        new ReverseRule()
    };
    // --- SETUP UI ---
    // Hàm này được NetworkAppManager gọi để "bơm" tham chiếu UI vào GameManager
    public void SetSceneReferences(Transform[] slots, Transform leftHandPos, Transform rightHandPos, TMP_Text turnText, Transform mainCanvas)
    {
        this.slots = slots;
        this.leftHandPos = leftHandPos;
        this.rightHandPos = rightHandPos;
        this.turnText = turnText;
        this.effectsCanvas = mainCanvas;

        uiReady = true; // Đánh dấu đã sẵn sàng để các script khác bắt đầu chạy logic UI
        Debug.Log("[GameManager] UI Ready. Cập nhật lại trạng thái các lá bài...");

        // Cập nhật lại trạng thái cho tất cả các lá bài đang tồn tại
        // (Phòng trường hợp bài sinh ra trước khi UI load xong)
        RefreshAllCards();
    }

    public override void Spawned()
    {
        // Đảm bảo chỉ có 1 GameManagerNet tồn tại. 
        // Nếu có cái cũ (do chuyển scene lỗi), hủy cái cũ đi.
        if (Instance != null && Instance != this)
        {
            if (Object.HasStateAuthority) Runner.Despawn(Object);
            return;
        }
        Instance = this;
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);


        // Nếu chơi chế độ Single (Offline), tự động bật AI và lấy component AI
        if (Runner.GameMode == GameMode.Single)
        {
            playWithAI = true;
            aiBrain = GetComponent<GameAI>();
            if (aiBrain != null) aiBrain.Init(this); // Khởi tạo AI với tham chiếu đến GameManager này
        }

        // Chỉ Host (người có quyền StateAuthority) mới được chia bài và set lượt đầu
        if (Object.HasStateAuthority)
        {
            // Random luật chơi (30% ra Reverse)
            CurrentRuleIndex = (Random.Range(0, 100) < 30) ? 1 : 0;

            CurrentTurn = 0; // Player 1 đi trước
            DealCards();
        }
        // Áp dụng luật ngay khi sinh ra
        UpdateRuleStrategy();
    }
    private void UpdateRuleStrategy()
    {
        if (CurrentRuleIndex >= 0 && CurrentRuleIndex < allRules.Count)
        {
            currentStrategy = allRules[CurrentRuleIndex];
            Debug.Log($"[GameManager] Đang áp dụng luật: {currentStrategy.RuleName}");
        }
        else
        {
            currentStrategy = allRules[0]; // Fallback về Normal
        }
    }

    // Dùng Render thay vì Update/FixedUpdate để đồng bộ với tần số quét màn hình
    public override void Render()
    {
        // Kiểm tra và tự gán UI nếu chưa có
        if (!uiReady)
        {
            if (NetworkAppManager.Instance != null)
                NetworkAppManager.Instance.SetupGameManagerUI(this);
            return;
        }
        // Kiểm tra thay đổi dữ liệu mạng
        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(BoardState)) RefreshAllCards(); // Bàn cờ đổi -> Refresh bài
            if (change == nameof(CurrentRuleIndex)) UpdateRuleStrategy(); // Luật đổi -> Update chiến lược
        }
        this.DisplayTurnText();
    }
    protected void DisplayTurnText()
    {
        // Hiển thị Text theo góc nhìn người chơi
        int localPlayerId = GetLocalPlayerID();
        if (turnText != null)
        {
            string ruleName = currentStrategy != null ? $"[{currentStrategy.RuleName}]" : "";

            if (localPlayerId == 0)
                turnText.text = (CurrentTurn == 0) ? $"Lượt của bạn (Blue) {ruleName}" : $"Lượt đối thủ (Red) {ruleName}";
            else
                turnText.text = (CurrentTurn == 0) ? $"Lượt đối thủ (Blue) {ruleName}" : $"Lượt của bạn (Red) {ruleName}";
        }
    }
    private void RefreshAllCards()
    {
        CardNet[] allCards = FindObjectsByType<CardNet>(FindObjectsSortMode.None);
        foreach (var card in allCards)
        {
            if (card != null && card.Object != null && card.Object.IsValid)
                card.RefreshState();
        }
    }
    // --- LOGIC GAME ---
    void DealCards()
    {
        // Chia 5 lá cho mỗi người
        for (int i = 0; i < 5; i++)
        {
            SpawnCard(0, i); // Chia cho Player 1 (ownerID 0)
            SpawnCard(1, i); // Chia cho Player 2 (ownerID 1)
        }
    }

    // Hàm sinh ra 1 lá bài cụ thể
    void SpawnCard(int ownerID, int index)
    {
        // Random chỉ số ngẫu nhiên (1-10)
        // Random này chạy trên Host nên kết quả là đồng nhất
        int t = Random.Range(1, 10);
        int r = Random.Range(1, 10);
        int b = Random.Range(1, 10);
        int l = Random.Range(1, 10);

        // Sinh ra object bài qua mạng
        var no = Runner.Spawn(cardPrefab, Vector3.zero, Quaternion.identity);
        CardNet card = no.GetComponent<CardNet>();

        // Gán dữ liệu vào các biến Networked
        // Việc gán này sẽ tự động kích hoạt ChangeDetector bên phía Client -> Client tự cập nhật UI
        card.Top = t; card.Right = r; card.Bottom = b; card.Left = l;
        card.OwnerID = ownerID;
        card.HandIndex = index;
    }

    // --- CLIENT INPUT (Xử lý thao tác người chơi) ---

    // Hàm xử lý khi người chơi bấm vào một lá bài
    public void SelectCard(CardNet card)
    {
        // Kiểm tra xem bài này có phải của người chơi hiện tại không
        if (card.OwnerID != GetLocalPlayerID())
        {
            ShowFloatingText("Không phải bài của bạn!", card.transform.position);
            return;
        }

        // Logic Highlight (Chọn/Bỏ chọn)
        if (selectedLocalCard != null) selectedLocalCard.SetHighlight(false);
        selectedLocalCard = card;
        if (selectedLocalCard != null) selectedLocalCard.SetHighlight(true);
    }

    // Hàm xử lý khi người chơi bấm vào ô trống trên bàn
    public void OnSlotClicked(int slotIndex)
    {
        if (selectedLocalCard == null) return;
        if (GetLocalPlayerID() != CurrentTurn) return;

        // Tắt highlight và gửi lệnh đánh bài lên Server
        selectedLocalCard.SetHighlight(false);
        RPC_PlayCard(selectedLocalCard.Object.Id, slotIndex);
        selectedLocalCard = null; // Reset bài đã chọn
    }

    // Hàm sinh ra chữ bay (Floating Text)
    public void ShowFloatingText(string message, Vector3 position)
    {
        // Kiểm tra null an toàn
        if (floatingTextPrefab == null)
        {
            Debug.LogWarning("Chưa gán Floating Text Prefab!");
            return;
        }

        // Fail-safe: Tìm lại canvas nếu bị mất
        if (effectsCanvas == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) effectsCanvas = canvas.transform;
        }

        // Tạo text
        GameObject go = Instantiate(floatingTextPrefab);

        // Gán vị trí (World Position) ngay khi sinh ra
        go.transform.position = position;

        // Gán vào Canvas để hiển thị đúng lớp UI
        if (effectsCanvas != null)
        {
            // worldPositionStays = true để giữ vị trí tại chỗ lá bài
            go.transform.SetParent(effectsCanvas, true);
            go.transform.localScale = Vector3.one;
        }

        // Setup nội dung
        FloatingText ft = go.GetComponent<FloatingText>();
        if (ft != null) ft.Setup(message);
    }

    // --- SERVER RPC (Hàm chạy trên Server do Client gọi) ---
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PlayCard(NetworkId cardId, int slotIndex, RpcInfo info = default)
    {
        // 1. Validation (Kiểm tra tính hợp lệ)
        if (slotIndex < 0 || slotIndex >= 9) return;
        if (BoardState[slotIndex].IsValid) return;   // Ô đã có người đánh

        NetworkObject cardObj = Runner.FindObject(cardId);
        if (cardObj != null)
        {
            CardNet card = cardObj.GetComponent<CardNet>();
            if (card.HandIndex == -1) return;

            // 3. Thực hiện nước đi
            BoardState.Set(slotIndex, cardId);

            // Quan trọng: Đổi HandIndex thành -1 để báo hiệu bài đã rời tay
            // Việc này sẽ kích hoạt ChangeDetector trên Client -> Bài tự bay vào ô
            card.HandIndex = -1;

            // ✅ GỌI CHIẾN LƯỢC (STRATEGY) ĐỂ XỬ LÝ LUẬT
            if (currentStrategy != null)
            {
                currentStrategy.ResolveBattle(this, card, slotIndex);
            }
            else
            {
                Debug.LogError("Lỗi: Chưa có luật nào được áp dụng! Dùng fallback Normal.");
                new NormalRule().ResolveBattle(this, card, slotIndex);
            }

            // 5. Kiểm tra kết thúc game
            if (GameRefereeNet.Instance != null) GameRefereeNet.Instance.CheckEndGame();

            // 6. Đổi lượt
            CurrentTurn = 1 - CurrentTurn;

            // 7. Nếu đang chơi với AI và đến lượt AI (P2) -> Gọi AI đánh
            if (playWithAI && CurrentTurn == 1 && Object.HasStateAuthority)
            {
                if (aiBrain != null) aiBrain.StartTurn(1);// 1 là aiPlayerID
            }
        }
    }

    // Hàm xác định ID người chơi trên máy cục bộ
    public int GetLocalPlayerID()
    {
        // 1. Nếu là Offline -> Mình là Player 0 (Blue)
        if (Runner.GameMode == GameMode.Single) return 0;

        // 2. Nếu là Host Mode Online
        if (Runner.IsServer) return 0; // Host luôn là 0
        return 1; // Client luôn là 1 (Trong game 1v1)
    }

    // Hàm gọi Restart game
    public void RequestRestart() { RPC_Restart(); }

    // RPC Restart (Chạy trên Server)
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Restart()
    {
        if (playWithAI && aiBrain != null) aiBrain.StopThinking();
        // Random lại luật khi chơi ván mới
        CurrentRuleIndex = (Random.Range(0, 100) < 30) ? 1 : 0;
        // Xóa hết bài cũ
        CardNet[] allCards = FindObjectsByType<CardNet>(FindObjectsSortMode.None);
        foreach (var card in allCards) Runner.Despawn(card.Object);

        // Reset bàn cờ về trống
        for (int i = 0; i < 9; i++)
        {
            BoardState.Set(i, default);
            // Reset màu nền ô slot về trắng
            if (slots != null && slots[i] != null)
            {
                Image img = slots[i].GetComponent<Image>();
                if (img) img.color = Color.white;
            }
        }
        // Reset lượt và chia bài mới
        CurrentTurn = 0;
        DealCards();
    }
    public IRuleSet GetCurrentRule()
    {
        // Nếu chưa có strategy (lúc mới vào), lấy theo index mạng
        if (currentStrategy == null)
        {
            if (CurrentRuleIndex >= 0 && CurrentRuleIndex < allRules.Count)
                return allRules[CurrentRuleIndex];
            return null;
        }

        return currentStrategy;
    }
}