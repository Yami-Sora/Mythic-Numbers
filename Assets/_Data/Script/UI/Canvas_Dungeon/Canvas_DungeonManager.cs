using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class Canvas_DungeonManager : TabListenerBase
{
    public static Canvas_DungeonManager Instance { get; private set; }

    [Header("Gold Dungeon UI")]
    public TextMeshProUGUI txtGoldStage;
    public TextMeshProUGUI txtGoldLimit;
    public Button btnEnterGold;

    [Header("Gem Dungeon UI")]
    public TextMeshProUGUI txtGemStage;
    public TextMeshProUGUI txtGemLimit;
    public Button btnEnterGem;

    private const int MAX_DAILY_ENTRIES = 2;
    private const string ITEM_ID_TICKET = "Dungeon_Entry_Ticket";

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
        
        if (btnEnterGold != null) btnEnterGold.onClick.AddListener(() => TryEnterDungeon(PlayFabDataManager.GameMode.GoldDungeon));
        if (btnEnterGem != null) btnEnterGem.onClick.AddListener(() => TryEnterDungeon(PlayFabDataManager.GameMode.GemDungeon));
    }

    protected override void OnTabChanged(Canvas_NavigationManager.TabType targetTab)
    {
        if (targetTab == Canvas_NavigationManager.TabType.Dungeon)
        {
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        if (PlayFabDataManager.Instance == null) return;

        var data = PlayFabDataManager.Instance;

        // Update Gold Dungeon UI
        if (txtGoldStage != null) txtGoldStage.text = $"Ải hiện tại: {data.GoldDungeonStage}";
        if (txtGoldLimit != null) txtGoldLimit.text = $"Lượt đi: {data.GoldEntriesToday}/{MAX_DAILY_ENTRIES}";
        if (btnEnterGold != null) btnEnterGold.interactable = (data.GoldEntriesToday < MAX_DAILY_ENTRIES);

        // Update Gem Dungeon UI
        if (txtGemStage != null) txtGemStage.text = $"Ải hiện tại: {data.GemDungeonStage}";
        if (txtGemLimit != null) txtGemLimit.text = $"Lượt đi: {data.GemEntriesToday}/{MAX_DAILY_ENTRIES}";
        if (btnEnterGem != null) btnEnterGem.interactable = (data.GemEntriesToday < MAX_DAILY_ENTRIES);
    }

    public void TryEnterDungeon(PlayFabDataManager.GameMode mode)
    {
        if (PlayFabDataManager.Instance == null) return;
        var data = PlayFabDataManager.Instance;

        // 1. Kiểm tra số lượt (Anti-Cheat)
        int currentEntries = (mode == PlayFabDataManager.GameMode.GoldDungeon) ? data.GoldEntriesToday : data.GemEntriesToday;
        if (currentEntries >= MAX_DAILY_ENTRIES)
        {
            VFXManager.Instance.SpawnFloatingText("Hết lượt đi ải hôm nay sếp ơi!", GetButtonTransform(mode), Color.red);
            return;
        }

        // 2. Kiểm tra bộ bài (Tận dụng logic DeckManager)
        if (DeckManager.Instance != null)
        {
            int cardCount = 0;
            foreach (var card in DeckManager.Instance.currentDeck) 
                if (card != null && card.data != null && card.data.cardID > 0) cardCount++;
            
            if (cardCount < PlayFabConstants.MAX_DECK_SIZE)
            {
                VFXManager.Instance.SpawnFloatingText($"Cần đủ {PlayFabConstants.MAX_DECK_SIZE} lá bài!", GetButtonTransform(mode), Color.red);
                return;
            }
        }

        // 3. Gọi PlayFab trừ tiền (Vé vào cổng)
        VFXManager.Instance.SpawnFloatingText("Đang kiểm tra vé...", GetButtonTransform(mode), Color.yellow);
        
        PlayFabClientAPI.PurchaseItem(new PurchaseItemRequest
        {
            CatalogVersion = "MainCatalog",
            ItemId = ITEM_ID_TICKET,
            Price = 20,
            VirtualCurrency = "EN"
        }, result => {
            // Thành công!
            if (mode == PlayFabDataManager.GameMode.GoldDungeon) data.GoldEntriesToday++;
            else data.GemEntriesToday++;

            data.CurrentMode = mode;
            data.SaveDungeonDataOnly(); // Lưu lại số lượt mới

            VFXManager.Instance.SpawnFloatingText("Lên đường!", GetButtonTransform(mode), Color.green);
            
            // Chuyển cảnh sang trận đấu (Sử dụng NetworkLauncher giống Canvas_Combat)
            var launcher = FindFirstObjectByType<NetworkLauncher>();
            if (launcher != null) 
            {
                launcher.OnPlayOfflineClicked();
            }

        }, error => {
            if (error.Error == PlayFabErrorCode.InsufficientFunds)
                VFXManager.Instance.SpawnFloatingText("Không đủ Năng lượng!", GetButtonTransform(mode), Color.red);
            else
                VFXManager.Instance.SpawnFloatingText("Lỗi server: " + error.Error, GetButtonTransform(mode), Color.red);
        });
    }

    private Transform GetButtonTransform(PlayFabDataManager.GameMode mode)
    {
        if (mode == PlayFabDataManager.GameMode.GoldDungeon) return btnEnterGold.transform;
        return btnEnterGem.transform;
    }
}
