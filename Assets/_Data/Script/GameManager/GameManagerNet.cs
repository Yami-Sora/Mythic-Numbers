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

    private IRuleSet currentStrategy;

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
            RandomizeRule();
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
        _inputHandler?.Update();
    }

    // --- INPUT FORWARDING ---
    // Các button sẽ gọi vào đây, ta chuyển tiếp sang InputHandler
    public void OnSlotClicked(int slotIndex) => _inputHandler.OnSlotClicked(slotIndex);
    public void OnCardInputDown(CardNet card) => _inputHandler?.OnPointerDown(card);
    public void OnCardInputUp(CardNet card) => _inputHandler?.OnPointerUp(card);
    public void OnCardInputExit(CardNet card) => _inputHandler?.OnPointerExit(card);

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
        if (CardDatabase.Instance == null)
        {
            Debug.LogError("CardDatabase not found! Make sure it exists in the scene.");
            return;
        }

        CardDataSO data = CardDatabase.Instance.GetRandomCard();

        if (data == null) return;

        // Spawn Network Object
        var no = Runner.Spawn(cardPrefab, Vector3.zero, Quaternion.identity);
        CardNet card = no.GetComponent<CardNet>();

        // Gán dữ liệu Networked
        card.OwnerID = ownerID;
        card.HandIndex = index;
        card.CardID = data.id;

        card.Top = data.top;
        card.Right = data.right;
        card.Bottom = data.bottom;
        card.Left = data.left;
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

        if (currentStrategy == null) UpdateRuleStrategy();

        // Kiểm tra luật Order
        if (!currentStrategy.CanPlayCard(this, card))
        {
            Debug.LogWarning($"[Server] Nước đi bị từ chối bởi luật: {currentStrategy.RuleName}");
            return;
        }

        // Update Data
        BoardState.Set(slotIndex, cardId);
        card.HandIndex = -1;

        if (card.CurrentSkill != null)
        {
            Debug.LogWarning($"[GameManager] Kích hoạt Skill: {card.CurrentSkill.name} của bài {card.CardID}");

            // Gọi hàm Execute trong ScriptableObject của Skill
            card.CurrentSkill.Execute(this, card, slotIndex);
        }
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
        // Gọi hàm này để giảm thời gian hiệu lực của các lá bài đang Vô Địch (Invincible) trên bàn
        UpdateBoardEffectsTick();
    }
    private void UpdateBoardEffectsTick()
    {
        for (int i = 0; i < 9; i++)
        {
            if (BoardState[i].IsValid)
            {
                NetworkObject no = Runner.FindObject(BoardState[i]);
                if (no != null)
                {
                    CardNet card = no.GetComponent<CardNet>();
                    // Gọi hàm TickInvincibility trên CardNet để giảm thời gian hiệu lực
                    if (card != null) card.TickInvincibility();
                }
            }
        }
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Restart()
    {
        if (playWithAI) aiBrain?.StopThinking();

        // Reset Logic
        RandomizeRule();
        CardNet[] allCards = FindObjectsByType<CardNet>(FindObjectsSortMode.None);
        foreach (var card in allCards) Runner.Despawn(card.Object);

        for (int i = 0; i < 9; i++) BoardState.Set(i, default);

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
        IRuleSet baseRule = null;
        if (CurrentRuleIndex == 0 || CurrentRuleIndex == 2)
            baseRule = new NormalRule();
        else
            baseRule = new ReverseRule();

        if (CurrentRuleIndex == 2 || CurrentRuleIndex == 3)
            currentStrategy = new OrderRuleDecorator(baseRule);
        else
            currentStrategy = baseRule;

        Debug.Log($"[GameManager] Applied Strategy: {currentStrategy.RuleName}");
    }

    private void RefreshAllCards()
    {
        // Tìm tất cả bài và refresh
        CardNet[] allCards = FindObjectsByType<CardNet>(FindObjectsSortMode.None);
        foreach (var card in allCards) 
            if (card && card.Object && card.Object.IsValid) card.RefreshState();
    }

    public int GetLocalPlayerID()
    {
        if (Runner.GameMode == GameMode.Single) return 0;
        return Runner.IsServer ? 0 : 1;
    }
    private void RandomizeRule()
    {
        bool isReverse = Random.Range(0, 100) < 50;
        bool hasOrder = Random.Range(0, 100) < 70; // 70% có Order

        if (!isReverse && !hasOrder) CurrentRuleIndex = 0;
        else if (isReverse && !hasOrder) CurrentRuleIndex = 1;
        else if (!isReverse && hasOrder) CurrentRuleIndex = 2;
        else if (isReverse && hasOrder) CurrentRuleIndex = 3;

        Debug.Log($"[GameManager] RuleIndex: {CurrentRuleIndex}");
    }
    public IRuleSet GetCurrentRule() => currentStrategy;
    public void SetCurrenRule()
    {
        if (GameUIManager.Instance != null && currentStrategy != null)
        {
            string baseRuleName = "";
            string baseRuleDesc = "";
            string subRuleName = "";
            string subRuleDesc = "";

            if (currentStrategy is RuleDecorator decorator)
            {
                subRuleName = decorator.RuleName;
                subRuleDesc = decorator.RuleDescription;

                // -- Lấy thông tin Luật Chính (Ruột) thông qua InnerRule --
                baseRuleName = decorator.InnerRule.RuleName;
                baseRuleDesc = decorator.InnerRule.RuleDescription;
            }
            else
            {
                baseRuleName = currentStrategy.RuleName;
                baseRuleDesc = currentStrategy.RuleDescription;
                subRuleName = "";
                subRuleDesc = "";
            }
            
            // Gửi tất cả thông tin sang UI Manager
            CanvasManager.Instance.UpdateRulePanelText(baseRuleName, subRuleName, baseRuleDesc, subRuleDesc);
        }
    }
}