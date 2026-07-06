using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class ShopPopupManager : MonoBehaviour
{
    public static ShopPopupManager Instance { get; private set; }

    [Header("--- UI Panels ---")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button btnClose;

    [Header("--- Shop Items / Buttons ---")]
    [SerializeField] private Button btnBuyGoldWithGem;
    [SerializeField] private Button btnRechargeGemTier1; // Nạp Ngọc Gói 1
    [SerializeField] private Button btnRechargeGemTier2; // Nạp Ngọc Gói 2

    [Header("--- Exchange Rates & Prices ---")]
    [SerializeField] private int goldAmountForExchange = 5000;
    [SerializeField] private int gemCostForGold = 100;

    private void Awake()
    {
        Instance = this;

        if (btnClose != null) btnClose.onClick.AddListener(Hide);
        if (btnBuyGoldWithGem != null) btnBuyGoldWithGem.onClick.AddListener(BuyGoldWithGem);
        
        // Dummy recharge button handlers (Sẽ liên kết SDK thanh toán ở các phase sau)
        if (btnRechargeGemTier1 != null) btnRechargeGemTier1.onClick.AddListener(() => SimulateRechargeGem(60));
        if (btnRechargeGemTier2 != null) btnRechargeGemTier2.onClick.AddListener(() => SimulateRechargeGem(300));
    }

    public void Show()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            RefreshData();
        }
    }

    public void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void RefreshData()
    {
        if (PlayFabDataManager.Instance != null)
        {
            PlayFabDataManager.Instance.FetchVirtualCurrencies();
        }
    }

    private void BuyGoldWithGem()
    {
        if (PlayFabDataManager.Instance == null) return;

        int currentGem = PlayFabDataManager.Instance.GetCurrencyBalance("GM");
        if (currentGem < gemCostForGold)
        {
            ShowFloatingMessage("Không đủ Linh Ngọc!");
            return;
        }

        if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(true);

        // Trừ Linh Ngọc (GM)
        PlayFabClientAPI.SubtractUserVirtualCurrency(new SubtractUserVirtualCurrencyRequest
        {
            VirtualCurrency = "GM",
            Amount = gemCostForGold
        }, 
        subResult => {
            // Cộng Vàng (GD)
            PlayFabClientAPI.AddUserVirtualCurrency(new AddUserVirtualCurrencyRequest
            {
                VirtualCurrency = "GD",
                Amount = goldAmountForExchange
            }, 
            addResult => {
                if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(false);
                PlayFabDataManager.Instance.FetchVirtualCurrencies();
                ShowFloatingMessage($"Đã đổi {goldAmountForExchange} Vàng thành công!");
            }, 
            error => {
                if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(false);
                Debug.LogError($"[Shop] Lỗi cộng Vàng: {error.ErrorMessage}");
                ShowFloatingMessage("Đổi vàng thất bại!");
            });
        }, 
        error => {
            if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(false);
            Debug.LogError($"[Shop] Lỗi trừ Ngọc: {error.ErrorMessage}");
            ShowFloatingMessage("Đổi vàng thất bại!");
        });
    }

    private void SimulateRechargeGem(int amount)
    {
        if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(true);

        // Giả lập nạp ngọc thông qua API AddUserVirtualCurrency
        PlayFabClientAPI.AddUserVirtualCurrency(new AddUserVirtualCurrencyRequest
        {
            VirtualCurrency = "GM",
            Amount = amount
        }, 
        result => {
            if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(false);
            if (PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.FetchVirtualCurrencies();
            ShowFloatingMessage($"Nạp thành công {amount} Linh Ngọc!");
        }, 
        error => {
            if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(false);
            Debug.LogError($"[Shop] Lỗi nạp ngọc: {error.ErrorMessage}");
            ShowFloatingMessage("Nạp ngọc thất bại!");
        });
    }

    private void ShowFloatingMessage(string message)
    {
        if (VFXManager.Instance != null && btnBuyGoldWithGem != null)
        {
            VFXManager.Instance.SpawnFloatingText(message, btnBuyGoldWithGem.transform);
        }
        else
        {
            Debug.Log($"[Shop Message] {message}");
        }
    }
}