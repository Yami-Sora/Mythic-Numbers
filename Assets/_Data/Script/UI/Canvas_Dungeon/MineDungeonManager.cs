using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// Quản lý logic của Mine Dungeon (Gold, LN, Gem Mine):
/// Hiển thị ải hiện tại, lượt đi và xử lý vào ải.
/// Gắn vào GameObject Mine_Dungeon.
/// </summary>
public class MineDungeonManager : YamiMonoBehaviour
{
    public static MineDungeonManager Instance { get; private set; }

    [Header("Gold Dungeon UI")]
    [SerializeField] private TextMeshProUGUI txtGoldStage;
    [SerializeField] private TextMeshProUGUI txtGoldLimit;
    [SerializeField] private Button btnEnterGold;

    [Header("LN Dungeon UI")]
    [SerializeField] private TextMeshProUGUI txtLNStage;
    [SerializeField] private TextMeshProUGUI txtLNLimit;
    [SerializeField] private Button btnEnterLN;

    [Header("Gem Mine UI")]
    [SerializeField] private TextMeshProUGUI txtGemMineStage;
    [SerializeField] private TextMeshProUGUI txtGemMineLimit;
    [SerializeField] private Button btnEnterGemMine;

    private const int MAX_DAILY_ENTRIES = 2;
    private const string ITEM_ID_TICKET = "Dungeon_Entry_Ticket";

    protected override void Awake()
    {
        base.Awake();
        Instance = this;

        if (btnEnterGold != null) btnEnterGold.onClick.AddListener(() => TryEnterDungeon(PlayFabDataManager.GameMode.GoldDungeon));
        if (btnEnterLN != null) btnEnterLN.onClick.AddListener(() => TryEnterDungeon(PlayFabDataManager.GameMode.LNDungeon));
        if (btnEnterGemMine != null) btnEnterGemMine.onClick.AddListener(() => TryEnterDungeon(PlayFabDataManager.GameMode.GemMine));
    }

    public void OpenMine()
    {
        RefreshUI();
    }

    public void ClosePanel()
    {
        if (Canvas_DungeonManager.Instance != null)
            Canvas_DungeonManager.Instance.Close_All_Panels();
    }

    public void RefreshUI()
    {
        if (PlayFabDataManager.Instance == null) return;
        var data = PlayFabDataManager.Instance;

        if (txtGoldStage != null) txtGoldStage.text = $"Ải hiện tại: {data.GoldDungeonStage}";
        if (txtGoldLimit != null) txtGoldLimit.text = $"Lượt đi: {data.GoldEntriesToday}/{MAX_DAILY_ENTRIES}";
        if (btnEnterGold != null) btnEnterGold.interactable = (data.GoldEntriesToday < MAX_DAILY_ENTRIES);

        if (txtLNStage != null) txtLNStage.text = $"Ải hiện tại: {data.LNDungeonStage}";
        if (txtLNLimit != null) txtLNLimit.text = $"Lượt đi: {data.LNEntriesToday}/{MAX_DAILY_ENTRIES}";
        if (btnEnterLN != null) btnEnterLN.interactable = (data.LNEntriesToday < MAX_DAILY_ENTRIES);

        if (txtGemMineStage != null) txtGemMineStage.text = $"Ải hiện tại: {data.GemMineStage}";
        if (txtGemMineLimit != null) txtGemMineLimit.text = $"Lượt đi: {data.GemMineEntriesToday}/{MAX_DAILY_ENTRIES}";
        if (btnEnterGemMine != null) btnEnterGemMine.interactable = (data.GemMineEntriesToday < MAX_DAILY_ENTRIES);
    }

    public void TryEnterDungeon(PlayFabDataManager.GameMode mode)
    {
        if (PlayFabDataManager.Instance == null) return;
        var data = PlayFabDataManager.Instance;

        int currentEntries = 0;
        if (mode == PlayFabDataManager.GameMode.GoldDungeon) currentEntries = data.GoldEntriesToday;
        else if (mode == PlayFabDataManager.GameMode.LNDungeon) currentEntries = data.LNEntriesToday;
        else if (mode == PlayFabDataManager.GameMode.GemMine) currentEntries = data.GemMineEntriesToday;

        if (currentEntries >= MAX_DAILY_ENTRIES)
        {
            VFXManager.Instance.SpawnFloatingText("Hết lượt đi ải hôm nay sếp ơi!", GetButtonTransform(mode), Color.red);
            return;
        }

        if (DeckManager.Instance != null)
        {
            int cardCount = 0;
            foreach (var card in DeckManager.Instance.currentDeck)
                if (card != null && card.data != null) cardCount++;

            if (cardCount < PlayFabConstants.MAX_DECK_SIZE)
            {
                VFXManager.Instance.SpawnFloatingText($"Cần đủ {PlayFabConstants.MAX_DECK_SIZE} lá bài!", GetButtonTransform(mode), Color.red);
                return;
            }
        }

        VFXManager.Instance.SpawnFloatingText("Đang kiểm tra vé...", GetButtonTransform(mode), Color.yellow);

        PlayFabClientAPI.PurchaseItem(new PurchaseItemRequest
        {
            CatalogVersion = "MainCatalog",
            ItemId = ITEM_ID_TICKET,
            Price = 20,
            VirtualCurrency = "EN"
        }, result =>
        {
            data.CurrentMode = mode;
            data.MarkDungeonDirty();
            data.SaveDungeonDataOnly();

            VFXManager.Instance.SpawnFloatingText("Lên đường!", GetButtonTransform(mode), Color.green);

            var launcher = FindFirstObjectByType<NetworkLauncher>();
            if (launcher != null) launcher.OnPlayOfflineClicked();

        }, error =>
        {
            if (error.Error == PlayFabErrorCode.InsufficientFunds)
                VFXManager.Instance.SpawnFloatingText("Không đủ Năng lượng!", GetButtonTransform(mode), Color.red);
            else
                VFXManager.Instance.SpawnFloatingText("Lỗi server: " + error.Error, GetButtonTransform(mode), Color.red);
        });
    }

    private Transform GetButtonTransform(PlayFabDataManager.GameMode mode)
    {
        if (mode == PlayFabDataManager.GameMode.GoldDungeon) return btnEnterGold.transform;
        if (mode == PlayFabDataManager.GameMode.LNDungeon) return btnEnterLN.transform;
        return btnEnterGemMine.transform;
    }
}
