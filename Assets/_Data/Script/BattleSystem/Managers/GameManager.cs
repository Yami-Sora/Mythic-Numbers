using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GameAI))]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Settings")]
    public bool playWithAI = true; // Luôn mặc định là true cho Async PvP
    [SerializeField] private GameObject cardPrefab;

    // --- CÁC MODULE CON ---
    private GameAI aiBrain;
    private LocalInputHandler _inputHandler;
    private CardDealer _cardDealer;

    // --- GAME DATA ---
    public int CurrentTurn { get; set; }
    public CardObj[] BoardState { get; private set; } = new CardObj[9];
    public int CurrentRuleIndex { get; set; }

    private IRuleSet currentStrategy;

    // --- DECK DATA ---
    private CardDataCache[] p1Deck;
    private CardDataCache[] p2Deck;
    private bool isP1Ready;
    private bool isP2Ready;

    // --- SETUP ---
    [Header("In-Game Hands")]
    [SerializeField] private List<CardObj> p1Hand = new List<CardObj>();
    [SerializeField] private List<CardObj> p2Hand = new List<CardObj>();

    public List<CardObj> P1Hand => p1Hand;
    public List<CardObj> P2Hand => p2Hand;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Init modules ngay trong Awake() để sẵn sàng khi BattleFlowManager
        // gọi RestartGame() ngay sau SetActive(true) (trước khi Start() chạy)
        _inputHandler = new LocalInputHandler(this);
        _cardDealer = new CardDealer(cardPrefab);

        if (playWithAI)
        {
            aiBrain = GetComponent<GameAI>();
            if (aiBrain != null) aiBrain.Init(this);
        }
    }


    private void PrepareSinglePlayerDecks()
    {
        SinglePlayerDeckBuilder.BuildDecks(out p1Deck, out p2Deck);
    }

    private void Update()
    {
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
    public void OnCardInputDown(CardObj card) => _inputHandler?.OnPointerDown(card);
    public void OnCardInputUp(CardObj card) => _inputHandler?.OnPointerUp(card);
    public void OnCardInputExit(CardObj card) => _inputHandler?.OnPointerExit(card);

    public void PlayCard(CardObj card, int slotIndex)
    {
        // Validation cơ bản
        if (slotIndex < 0 || slotIndex >= 9 || BoardState[slotIndex] != null) return;
        if (card == null || card.HandIndex == -1) return;

        if (currentStrategy == null) UpdateRuleStrategy();

        // Kiểm tra luật Order
        if (!currentStrategy.CanPlayCard(this, card))
        {
            Debug.LogWarning($"[GameManager] Nước đi bị từ chối bởi luật: {currentStrategy.RuleName}");
            return;
        }

        // Update Data
        BoardState[slotIndex] = card;
        card.HandIndex = -1;

        // Xóa khỏi danh sách bài trên tay
        if (card.OwnerID == 0) p1Hand.Remove(card);
        else p2Hand.Remove(card);

        //Kích hoạt Pre - Battle Skill(Execute)
        if (card.CurrentSkill != null)
        {
            card.CurrentSkill.Execute(this, card, slotIndex);
        }
        var preBattleOwners = CaptureBoardOwners(card);
        
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
        
        RefreshAllCards();

        // Check Endgame & Đổi lượt
        if (GameReferee.Instance != null) GameReferee.Instance.CheckEndGame();
        CurrentTurn = 1 - CurrentTurn;

        // AI Logic
        if (playWithAI && CurrentTurn == 1)
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
            if (BoardState[i] != null)
            {
                BoardState[i].TickInvincibility();
            }
        }
    }

    public void RestartGame()
    {
        if (playWithAI) aiBrain?.StopThinking();

        CleanupBattle();

        CurrentTurn = 0;
        PrepareSinglePlayerDecks();
        _cardDealer.DealCards(p1Deck, p2Deck, out p1Hand, out p2Hand);

        UpdateRuleStrategy();
        ResetUI();
    }

    public void CleanupBattle()
    {
        // 1. Reset Logic Rule
        RandomizeRule();

        // 2. Xóa sạch object bài trong scene
        CardObj[] allCards = FindObjectsByType<CardObj>(FindObjectsSortMode.None);
        foreach (var card in allCards)
        {
            if (card != null) Destroy(card.gameObject);
        }

        // 3. Xóa dữ liệu logic bàn cờ
        for (int i = 0; i < 9; i++) BoardState[i] = null;

        // 4. Xóa danh sách bài trên tay
        p1Hand.Clear();
        p2Hand.Clear();

        // 5. Reset UI (Màu sắc slot, v.v.)
        ResetUI();
    }

    public void ResetUI()
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
        CardObj[] allCards = FindObjectsByType<CardObj>(FindObjectsSortMode.None);
        foreach (var card in allCards) 
            if (card != null && card.gameObject != null) card.RefreshState();
    }

    public int GetLocalPlayerID()
    {
        return 0; // Luôn là 0 trong chế độ Offline
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
            
            InGameUIManager.Instance.UpdateRulePanelText(baseRuleName, subRuleName, baseRuleDesc, subRuleDesc);
        }
    }

    // Lưu lại trạng thái chủ sở hữu của các bài xung quanh trước khi Battle
    private Dictionary<CardObj, int> CaptureBoardOwners(CardObj excludeCard)
    {
        Dictionary<CardObj, int> owners = new Dictionary<CardObj, int>();
        for (int i = 0; i < 9; i++)
        {
            if (BoardState[i] != null && BoardState[i] != excludeCard)
            {
                owners[BoardState[i]] = BoardState[i].OwnerID;
            }
        }
        return owners;
    }
    
    // So sánh trạng thái hiện tại với trạng thái cũ để đếm số bài bị đổi chủ
    private int CountFlippedCards(Dictionary<CardObj, int> preBattleOwners, int attackerOwnerID)
    {
        int count = 0;
        foreach (var kvp in preBattleOwners)
        {
            if (kvp.Key != null)
            {
                int currentOwner = kvp.Key.OwnerID;
                if (currentOwner != kvp.Value && currentOwner == attackerOwnerID)
                {
                    count++;
                }
            }
        }
        return count;
    }
}