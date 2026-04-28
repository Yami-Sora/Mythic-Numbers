using UnityEngine;

public class ArenaChallengePopup : BaseDailyBuyPopup
{
    public static ArenaChallengePopup Instance { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
        
        // Thiết lập cấu hình riêng cho Arena
        playFabDataKey = "DailyArenaBuys";
        pricePerUnit = 5;
        maxDailyLimit = 10;
        
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public override void Open()
    {
        if (txtTitle != null) txtTitle.text = "Lượt khiêu chiến";
        if (txtDescription != null) txtDescription.text = "Xác nhận tiêu Linh Ngọc mua Lượt khiêu chiến?";
        base.Open();
    }

    protected override void OnPurchaseSuccess(int amount, int remainingBalance)
    {
        Debug.Log($"<color=green>[Arena]</color> Đã mua {amount} lượt khiêu chiến.");
        if (ArenaManager.Instance != null)
        {
            ArenaManager.Instance.AddChallenges(amount);
        }
    }
}
