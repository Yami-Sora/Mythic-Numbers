using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;

public class Canvas_CombatManager : TabListenerBase
{
    public static Canvas_CombatManager Instance { get; private set; }

    [Header("UI Controls")]
    public Button btnEnterStage; 

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
        }
    }

    // ==========================================
    // LOGIC CHECK VÉ VÀ VÀO ẢI
    // ==========================================
    public void TryEnterStage()
    {
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
                        if (ShopManager.Instance != null && btnEnterStage != null)
                        {
                            ShopManager.Instance.SpawnFloatingText("Không đủ Năng lượng!", btnEnterStage.transform);
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