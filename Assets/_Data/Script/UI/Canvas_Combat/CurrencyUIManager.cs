using UnityEngine;
using TMPro;

public class CurrencyUIManager : MonoBehaviour
{
    public static CurrencyUIManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI txtGold;
    [SerializeField] private TextMeshProUGUI txtGem;

    private void Awake()
    {
        Instance = this;
    }

    // Hàm này sẽ được PlayFab gọi khi lấy được số dư
    public void UpdateBalances(int gold, int gem)
    {
        // ToString("N0") để nó hiển thị có dấu phẩy (VD: 1,000,000) nhìn cho VIP
        if (txtGold != null) txtGold.text = gold.ToString("N0");
        if (txtGem != null) txtGem.text = gem.ToString("N0");
    }
}