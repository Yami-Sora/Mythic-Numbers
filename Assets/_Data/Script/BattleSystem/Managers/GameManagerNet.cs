using System.Collections;
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
    private LocalInputHandler _inputHandler;
    private ChangeDetector _changes;
    private CardDealer _cardDealer;

    // --- NETWORKED DATA ---
    [Networked] public int CurrentTurn { get; set; }
    [Networked, Capacity(9)] public NetworkArray<NetworkId> BoardState { get; }
    [Networked] public int CurrentRuleIndex { get; set; }

    private IRuleSet currentStrategy;

    // --- DECK DATA ---
    private CardDataCache[] p1Deck;
    private CardDataCache[] p2Deck;
    private bool isP1Ready;
    private bool isP2Ready;

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

        _cardDealer = new CardDealer(Runner, cardPrefab);

        if (Object.HasStateAuthority)
        {
            RandomizeRule();
            CurrentTurn = 0;
            
            if (Runner.GameMode == GameMode.Single)
            {
                PrepareSinglePlayerDecks();
                _cardDealer.DealCards(p1Deck, p2Deck);
            }
            else
            {
                SubmitLocalDeck();
            }
        }
        else
        {
            SubmitLocalDeck();
        }
        UpdateRuleStrategy();
    }

    private void PrepareSinglePlayerDecks()
    {
        p1Deck = new CardDataCache[5];
        for (int i=0; i<5; i++) p1Deck[i] = LocalDeckContext.CurrentDeck[i];

        p2Deck = new CardDataCache[5];

        // --- [ARENA CHECK] ---
        // Nếu có bộ bài đối thủ được nạp từ Arena (Snapshot), dùng luôn bộ bài đó
        if (LocalDeckContext.HasOpponentDeck)
        {
            Debug.Log("<color=orange>[Battle] Trận đấu Arena: Sử dụng bộ bài Snapshot của đối thủ!</color>");
            for (int i = 0; i < 5; i++) p2Deck[i] = LocalDeckContext.OpponentDeck[i];
            
            // Reset flag để không ảnh hưởng trận sau
            LocalDeckContext.HasOpponentDeck = false;
            return;
        }
        
        // --- [AI SCALING PVE] ---
        // Nếu không phải Arena, đây là trận đấu Story hoặc Dungeon bình thường
        int stage = 1;
        float powerStep = 0.1f; 
        
        if (PlayFabDataManager.Instance != null)
        {
            if (PlayFabDataManager.Instance.CurrentMode == PlayFabDataManager.GameMode.GoldDungeon)
            {
                stage = PlayFabDataManager.Instance.GoldDungeonStage;
                powerStep = 0.2f;
            }
            else if (PlayFabDataManager.Instance.CurrentMode == PlayFabDataManager.GameMode.LNDungeon)
            {
                stage = PlayFabDataManager.Instance.LNDungeonStage;
                powerStep = 0.2f;
            }
            else if (PlayFabDataManager.Instance.CurrentMode == PlayFabDataManager.GameMode.GemMine)
            {
                stage = PlayFabDataManager.Instance.GemMineStage;
                powerStep = 0.2f;
            }
            else
            {
                stage = PlayFabDataManager.Instance.CurrentStage;
                powerStep = 0.1f;
            }
        }
        
        float multiplier = 1.0f + (stage - 1) * powerStep;

        for (int i=0; i<5; i++)
        {
            var rCard = CardDatabase.Instance.GetRandomCard();
            if (rCard != null)
            {
                p2Deck[i] = new CardDataCache {
                    CardID = rCard.cardID,
                    Top = Mathf.RoundToInt(rCard.top * multiplier),
                    Right = Mathf.RoundToInt(rCard.right * multiplier),
                    Bottom = Mathf.RoundToInt(rCard.bottom * multiplier),
                    Left = Mathf.RoundToInt(rCard.left * multiplier),
                    StarLevel = 0
                };
            }
        }
    }

    private void SubmitLocalDeck()
    {
        if (!LocalDeckContext.HasDeck) 
        {
            Debug.LogWarning("[GameManager] Không tìm thấy Local Deck!");
        }
        
        int[] ids = new int[5];
        int[] tops = new int[5];
        int[] rights = new int[5];
        int[] bottoms = new int[5];
        int[] lefts = new int[5];

        for (int i=0; i<5; i++)
        {
            ids[i] = LocalDeckContext.CurrentDeck[i].CardID;
            tops[i] = LocalDeckContext.CurrentDeck[i].Top;
            rights[i] = LocalDeckContext.CurrentDeck[i].Right;
            bottoms[i] = LocalDeckContext.CurrentDeck[i].Bottom;
            lefts[i] = LocalDeckContext.CurrentDeck[i].Left;
        }

        RPC_SubmitDeck(ids, tops, rights, bottoms, lefts);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SubmitDeck(int[] ids, int[] tops, int[] rights, int[] bottoms, int[] lefts, RpcInfo info = default)
    {
        int playerId = (info.Source == Runner.LocalPlayer) ? 0 : 1;
        CardDataCache[] submittedDeck = new CardDataCache[5];
        for (int i=0; i<5; i++)
        {
            submittedDeck[i] = new CardDataCache {
                CardID = ids[i],
                Top = tops[i],
                Right = rights[i],
                Bottom = bottoms[i],
                Left = lefts[i]
            };
        }

        if (playerId == 0)
        {
            p1Deck = submittedDeck;
            isP1Ready = true;
        }
        else
        {
            p2Deck = submittedDeck;
            isP2Ready = true;
        }

        CheckAndStartMatch();
    }

    private void CheckAndStartMatch()
    {
        if (isP1Ready && isP2Ready)
        {
            _cardDealer.DealCards(p1Deck, p2Deck);
        }
    }

    // Hàm này giữ lại để tương thích với NetworkAppManager cũ, nhưng sẽ đẩy data sang GameUIManager
    public void SetSceneReferences(Transform[] slots, Transform left, Transform right, TMPro.TMP_Text turnText, Transform mainCanvas)
    {
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.SetupReferences(slots, left, right, turnText, mainCanvas);
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
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.UpdateTurnText(GetLocalPlayerID(), CurrentTurn, currentStrategy?.RuleName);
        }
        _inputHandler?.Update();
    }

    // --- INPUT FORWARDING ---
    // Các button sẽ gọi vào đây, ta chuyển tiếp sang InputHandler
    public void OnSlotClicked(int slotIndex) => _inputHandler.OnSlotClicked(slotIndex);
    public void OnCardInputDown(CardNet card) => _inputHandler?.OnPointerDown(card);
    public void OnCardInputUp(CardNet card) => _inputHandler?.OnPointerUp(card);
    public void OnCardInputExit(CardNet card) => _inputHandler?.OnPointerExit(card);

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


        //Kích hoạt Pre - Battle Skill(Execute)
        if (card.CurrentSkill != null)
        {
            card.CurrentSkill.Execute(this, card, slotIndex);
        }
        var preBattleOwners = CaptureBoardOwners(cardId);
        // Xử lý luật
        if (currentStrategy == null) currentStrategy = new NormalRule();
        currentStrategy.ResolveBattle(this, card, slotIndex);

        // Tính số lượng bài bị lật (Flip Count)
        int flippedCount = CountFlippedCards(preBattleOwners, card.OwnerID);

        // Kích hoạt Post-Battle Skill (OnAfterBattle)
        if (card.CurrentSkill != null)
        {
            card.CurrentSkill.OnAfterBattle(this, card, flippedCount);
        }
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
        
        if (Runner.GameMode == GameMode.Single)
        {
            PrepareSinglePlayerDecks();
            _cardDealer.DealCards(p1Deck, p2Deck);
        }
        else
        {
            isP1Ready = false;
            isP2Ready = false;
            SubmitLocalDeck();
            RPC_RequestClientSubmitDeck();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_RequestClientSubmitDeck()
    {
        SubmitLocalDeck();
    }

    // Helper để gọi UI reset trên tất cả máy
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ResetUIOnClients()
    {
        InGameUIManager.Instance?.ResetBoardUI();
        _inputHandler?.Deselect();
    }

    // --- UTILS ---
    private void UpdateRuleStrategy()
    {
        currentStrategy = RuleStrategyFactory.Create(CurrentRuleIndex);
        Debug.Log($"[GameManager] Applied Strategy: {currentStrategy.RuleName}");
    }

    public void RefreshAllCards()
    {
        // Tìm tất cả bài và refresh vị trí theo BoardState
        CardNet[] allCards = FindObjectsByType<CardNet>(FindObjectsSortMode.None);
        foreach (var card in allCards) 
            if (card && card.Object && card.Object.IsValid) card.RefreshState();
    }

    /// <summary> RPC để đồng bộ vị trí bài khi player join/reconnect – client cần state chính xác </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ForceRefreshCards()
    {
        RefreshAllCards();
        // Refresh lại sau 0.5s – đảm bảo client đã nhận hết NetworkObjects (bài) trước khi đồng bộ
        StartCoroutine(DelayedRefreshForReconnect());
    }

    private IEnumerator DelayedRefreshForReconnect()
    {
        yield return new WaitForSeconds(0.5f);
        RefreshAllCards();
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
        if (InGameUIManager.Instance != null && currentStrategy != null)
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
            InGameUIManager.Instance.UpdateRulePanelText(baseRuleName, subRuleName, baseRuleDesc, subRuleDesc);
        }
    }

    // Lưu lại trạng thái chủ sở hữu của các bài xung quanh trước khi Battle
    private Dictionary<NetworkId, int> CaptureBoardOwners(NetworkId excludeCardId)
    {
        Dictionary<NetworkId, int> owners = new Dictionary<NetworkId, int>();
        for (int i = 0; i < 9; i++)
        {
            if (BoardState[i].IsValid && BoardState[i] != excludeCardId)
            {
                var obj = Runner.FindObject(BoardState[i]);
                if (obj != null)
                {
                    owners[BoardState[i]] = obj.GetComponent<CardNet>().OwnerID;
                }
            }
        }
        return owners;
    }
    
    // So sánh trạng thái hiện tại với trạng thái cũ để đếm số bài bị đổi chủ
    private int CountFlippedCards(Dictionary<NetworkId, int> preBattleOwners, int attackerOwnerID)
    {
        int count = 0;
        foreach (var kvp in preBattleOwners)
        {
            var obj = Runner.FindObject(kvp.Key);
            if (obj != null)
            {
                int currentOwner = obj.GetComponent<CardNet>().OwnerID;
                // Nếu chủ sở hữu thay đổi (khác value cũ) VÀ giờ thuộc về người tấn công -> Đã bị lật
                if (currentOwner != kvp.Value && currentOwner == attackerOwnerID)
                {
                    count++;
                }
            }
        }
        return count;
    }
}