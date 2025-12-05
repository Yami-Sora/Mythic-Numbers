using System.Collections.Generic;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(GameAI))]
public class GameManagerNet : NetworkBehaviour
{
    public static GameManagerNet Instance { get; private set; }

    [Header("Settings")]
    public bool playWithAI = false;
    [SerializeField] private NetworkObject cardPrefab;

    // --- CÁC MODULE CON ---
    private GameAI aiBrain;
    private LocalInputHandler _inputHandler; // Module xử lý input
    private ChangeDetector _changes;

    // --- NETWORKED DATA ---
    [Networked] public int CurrentTurn { get; set; }
    [Networked, Capacity(9)] public NetworkArray<NetworkId> BoardState { get; }
    [Networked] public int CurrentRuleIndex { get; set; }

    // --- LOGIC LUẬT ---
    private IRuleSet currentStrategy;
    private List<IRuleSet> allRules = new List<IRuleSet>() { new NormalRule(), new ReverseRule() };

    // --- SETUP ---
    public override void Spawned()
    {
        if (Instance != null && Instance != this)
        {
            if (Object.HasStateAuthority) Runner.Despawn(Object);
            return;
        }
        Instance = this;
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _inputHandler = new LocalInputHandler(this); // Khởi tạo Input Handler

        // Setup AI
        if (Runner.GameMode == GameMode.Single)
        {
            playWithAI = true;
            aiBrain = GetComponent<GameAI>();
            if (aiBrain != null) aiBrain.Init(this);
        }

        if (Object.HasStateAuthority)
        {
            CurrentRuleIndex = (Random.Range(0, 100) < 30) ? 1 : 0;
            CurrentTurn = 0;
            DealCards();
        }
        UpdateRuleStrategy();
    }

    // Hàm này giữ lại để tương thích với NetworkAppManager cũ, nhưng sẽ đẩy data sang GameUIManager
    public void SetSceneReferences(Transform[] slots, Transform left, Transform right, TMPro.TMP_Text turnText, Transform mainCanvas)
    {
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.SetupReferences(slots, left, right, turnText, mainCanvas);
            RefreshAllCards();
        }
    }

    public override void Render()
    {
        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(BoardState)) RefreshAllCards();
            if (change == nameof(CurrentRuleIndex)) UpdateRuleStrategy();
        }

        // Cập nhật UI thông qua Manager
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.UpdateTurnText(GetLocalPlayerID(), CurrentTurn, currentStrategy?.RuleName);
        }
    }

    // --- INPUT FORWARDING ---
    // Các button sẽ gọi vào đây, ta chuyển tiếp sang InputHandler
    public void SelectCard(CardNet card) => _inputHandler.SelectCard(card);
    public void OnSlotClicked(int slotIndex) => _inputHandler.OnSlotClicked(slotIndex);

    // --- GAME LOGIC ---
    void DealCards()
    {
        for (int i = 0; i < 5; i++)
        {
            SpawnCard(0, i); // Chia cho Player 1 (ownerID 0)
            SpawnCard(1, i);// Chia cho Player 2 (ownerID 1)
        }
    }

    void SpawnCard(int ownerID, int index)
    {
        int t = Random.Range(1, 10); int r = Random.Range(1, 10);
        int b = Random.Range(1, 10); int l = Random.Range(1, 10);

        var no = Runner.Spawn(cardPrefab, Vector3.zero, Quaternion.identity);
        CardNet card = no.GetComponent<CardNet>();
        card.Top = t; card.Right = r; card.Bottom = b; card.Left = l;
        card.OwnerID = ownerID; 
        card.HandIndex = index;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PlayCard(NetworkId cardId, int slotIndex, RpcInfo info = default)
    {
        // Validation cơ bản
        if (slotIndex < 0 || slotIndex >= 9 || BoardState[slotIndex].IsValid) return;

        NetworkObject cardObj = Runner.FindObject(cardId);
        if (cardObj == null) return;

        CardNet card = cardObj.GetComponent<CardNet>();
        if (card.HandIndex == -1) return;

        // Update Data
        BoardState.Set(slotIndex, cardId);
        card.HandIndex = -1;

        // Xử lý luật
        if (currentStrategy == null) currentStrategy = new NormalRule();
        currentStrategy.ResolveBattle(this, card, slotIndex);

        // Check Endgame & Đổi lượt
        if (GameRefereeNet.Instance != null) GameRefereeNet.Instance.CheckEndGame();
        CurrentTurn = 1 - CurrentTurn;

        // AI Logic
        if (playWithAI && CurrentTurn == 1 && Object.HasStateAuthority)
        {
            aiBrain?.StartTurn(1);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Restart()
    {
        if (playWithAI) aiBrain?.StopThinking();

        // Reset Logic
        CurrentRuleIndex = (Random.Range(0, 100) < 30) ? 1 : 0;
        CardNet[] allCards = FindObjectsByType<CardNet>(FindObjectsSortMode.None);
        foreach (var card in allCards) Runner.Despawn(card.Object);

        for (int i = 0; i < 9; i++) BoardState.Set(i, default);

        // Reset UI (Gọi qua RPC cho client biết, hoặc client tự detect qua BoardState)
        // Ở đây BoardState thay đổi sẽ kích hoạt Render, nhưng màu sắc ô cần reset thủ công
        // Ta có thể thêm RPC Client để reset màu, hoặc để đơn giản ta dựa vào BoardState change
        // Nhưng tốt nhất là Reset UI trên Client
        RPC_ResetUIOnClients();

        CurrentTurn = 0;
        DealCards();
    }

    // Helper để gọi UI reset trên tất cả máy
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ResetUIOnClients()
    {
        GameUIManager.Instance?.ResetBoardUI();
        _inputHandler?.Deselect();
    }

    // --- UTILS ---
    private void UpdateRuleStrategy()
    {
        currentStrategy = (CurrentRuleIndex >= 0 && CurrentRuleIndex < allRules.Count)
            ? allRules[CurrentRuleIndex] : allRules[0];
    }

    private void RefreshAllCards()
    {
        // Tìm tất cả bài và refresh
        CardNet[] allCards = FindObjectsByType<CardNet>(FindObjectsSortMode.None);
        foreach (var c in allCards) if (c && c.Object && c.Object.IsValid) c.RefreshState();
    }

    public int GetLocalPlayerID()
    {
        if (Runner.GameMode == GameMode.Single) return 0;
        return Runner.IsServer ? 0 : 1;
    }

    public IRuleSet GetCurrentRule() => currentStrategy;
}