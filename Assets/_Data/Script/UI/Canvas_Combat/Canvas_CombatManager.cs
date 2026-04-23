using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;

using TMPro;

public class Canvas_CombatManager : TabListenerBase
{
    public static Canvas_CombatManager Instance { get; private set; }

    [Header("UI Controls")]
    public Button btnEnterStage; 
    [SerializeField] private TextMeshProUGUI txtCurrentStage;
    [SerializeField] private TextMeshProUGUI txtStageReward;

    protected override void Awake()
    {
        Instance = this;
        if (btnEnterStage != null)
        {
            btnEnterStage.onClick.RemoveAllListeners();
            btnEnterStage.onClick.AddListener(TryEnterStage);
        }
    }

    // Override lại hàm cha, sếp thích làm mới UI lúc bấm vào Tab Ải thì viết ở đây
    protected override void OnTabChanged(Canvas_NavigationManager.TabType targetTab)
    {
        // Code ví dụ: Nếu là Tab Combat thì bật nút lên
        if (targetTab == Canvas_NavigationManager.TabType.Combat)
        {
            if (btnEnterStage != null) btnEnterStage.interactable = true;
            RefreshStageUI();
        }
    }

    private void RefreshStageUI()
    {
        if (PlayFabDataManager.Instance == null) return;

        int stage = PlayFabDataManager.Instance.CurrentStage;
        if (txtCurrentStage != null) txtCurrentStage.text = $"Ải hiện tại: {stage}";

        // Tăng 5% Vàng mỗi ải
        int baseGold = 50;
        int goldReward = Mathf.RoundToInt(baseGold * (1 + 0.05f * (stage - 1)));
        
        string rewardText = $"Thưởng: {goldReward} Vàng";
        if (stage % 10 == 0) rewardText += " + 1 Linh Ngọc (Boss)";

        if (txtStageReward != null) txtStageReward.text = rewardText;
    }

    // ==========================================
    // LOGIC CHECK VÉ VÀ VÀO ẢI
    // ==========================================
    public void TryEnterStage()
    {
        // 0. Kiểm tra đội hình đủ 5 lá chưa sếp ơi!
        if (DeckManager.Instance != null)
        {
            int cardCount = 0;
            foreach (var card in DeckManager.Instance.currentDeck)
            {
                if (card != null && card.data != null) cardCount++;
            }

            if (cardCount < 5)
            {
                Debug.LogWarning("[Combat] Đội hình chưa đủ 5 lá, không cho đi ải!");
                if (EffectManager.Instance != null && btnEnterStage != null)
                {
                    EffectManager.Instance.SpawnFloatingText("Cần đủ 5 lá bài!", btnEnterStage.transform);
                }
                return; 
            }
        }

        // 1. Khóa mẹ cái nút lại ngay lập tức!
        // Chống mấy tay spam click liên tục trừ 20-30 thể lực 1 lúc
        if (btnEnterStage != null) btnEnterStage.interactable = false;

        Debug.Log("[Combat] Đang xin phép Server cho đi ải...");
        var request = new PurchaseItemRequest
        {
            CatalogVersion = "MainCatalog",
            ItemId = "Stage_Entry_Ticket", // Mã Item sếp cấu hình trên PlayFab
            VirtualCurrency = "EN",
            Price = 10
        };

        PlayFabClientAPI.PurchaseItem(request,
            result => {
                Debug.Log("<color=green>[Combat] Trừ 10 EN thành công! Load map đấm nhau thôi!</color>");

                // Trừ tiền xong nhớ hú thằng PlayFabDataManager lấy lại số dư mới để UI nó nhảy số
                if (PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.FetchVirtualCurrencies();

                // Lưu lại trạng thái PlayMode (PvE)
                PlayerPrefs.SetInt("GameMode", 0); // 0 = PvE, 1 = PvP (nếu có sau này)
                PlayerPrefs.Save();

                // CHỈ khi trừ thể lực thành công thì mới cho nhảy sang Scene chiến đấu!
                var launcher = FindFirstObjectByType<NetworkLauncher>();
                if (launcher != null) 
                {
                    launcher.OnPlayOfflineClicked();
                }
            },
            error => {
                // 2. Lỗi thì mở khóa lại nút để sếp còn bấm được
                if (btnEnterStage != null) btnEnterStage.interactable = true;

                if (error.Error == PlayFabErrorCode.InsufficientFunds)
                {
                    Debug.LogWarning("Sếp ơi, hết pin (thể lực) rồi, không vào ải được!");

                    // Sửa lại đoạn báo lỗi trong TryEnterStage:
                    if (error.Error == PlayFabErrorCode.InsufficientFunds)
                    {
                        Debug.LogWarning("Sếp ơi, hết pin (thể lực) rồi!");

                        // Gọi con hàng bên ShopManager sang bắn text bay cho xịn
                        if (EffectManager.Instance != null && btnEnterStage != null)
                        {
                            EffectManager.Instance.SpawnFloatingText("Không đủ Năng lượng!", btnEnterStage.transform);
                        }
                    }
                }
                else
                {
                    Debug.LogError("Lỗi mạng ròi sếp: " + error.ErrorMessage);
                }
            }
        );
    }
}