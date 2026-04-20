using System.Collections.Generic;
using DG.Tweening;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    // Thêm reference tới cái nút để làm hiệu ứng rung khi hết tiền
    [SerializeField] private Transform normalPackBtn;
    [SerializeField] private Transform premiumPackBtn;

    private void Awake() { Instance = this; }

    public void BuyNormalPack() => BuyAndOpenPack("Pack_Normal", "GD", 100, normalPackBtn);
    public void BuyPremiumPack() => BuyAndOpenPack("Pack_Premium", "GM", 50, premiumPackBtn);

    private void BuyAndOpenPack(string catalogItemId, string currencyCode, int price, Transform btnTransform)
    {
        var purchaseReq = new PurchaseItemRequest
        {
            CatalogVersion = "MainCatalog",
            ItemId = catalogItemId,
            VirtualCurrency = currencyCode,
            Price = price
        };

        PlayFabClientAPI.PurchaseItem(purchaseReq,
            buyRes => {
                if (PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.FetchVirtualCurrencies();
                UnlockPack(buyRes.Items[0].ItemInstanceId);
            },
            error => {
                if (error.Error == PlayFabErrorCode.InsufficientFunds)
                {
                    string msg = currencyCode == "GD" ? "Vàng" : "Linh Ngọc";

                    // 1. Nút rung bần bật
                    if (btnTransform != null) btnTransform.DOShakePosition(0.5f, 10, 10);

                    // 2. GỌI TEXT BAY DOTWEEN NGAY TẠI ĐÂY NÈ!
                    if (EffectManager.Instance != null)
                    {
                        EffectManager.Instance.SpawnFloatingText($"Không đủ {msg}!", btnTransform);
                    }

                    Debug.LogWarning($"[Shop] Nghèo mà đòi đú Gacha! Không đủ {msg}.");
                }
            }
        );
    }

    private void UnlockPack(string containerInstanceId)
    {
        var unlockReq = new UnlockContainerInstanceRequest
        {
            CatalogVersion = "MainCatalog",
            ContainerItemInstanceId = containerInstanceId
        };

        PlayFabClientAPI.UnlockContainerInstance(unlockReq,
            unlockRes => ProcessGrantedItems(unlockRes.GrantedItems),
            error => Debug.LogError("[Shop] Lỗi khui hộp: " + error.ErrorMessage)
        );
    }

    // --- XỬ LÝ KẾT QUẢ QUAY GACHA ---
    private void ProcessGrantedItems(List<ItemInstance> grantedItems)
    {
        List<CardDataSO> newCards = new List<CardDataSO>();
        bool isAnyCardUpdated = false;

        foreach (var item in grantedItems)
        {
            if (int.TryParse(item.ItemId, out int cardId))
            {
                CardDataSO data = CardDatabase.Instance.GetCardData(cardId);
                if (data != null)
                {
                    if (CardListManager.Instance != null)
                    {
                        var inventoryCards = CardListManager.Instance.GetOwnedCards();

                        // [UPDATE LOGIC]: Dò tìm xem thẻ này đã có mặt trong Kho chưa?
                        var existingCard = inventoryCards.Find(c => c.data.cardID == data.cardID);

                        if (existingCard != null)
                        {
                            // NẾU TRÙNG: Chuyển hóa thành 50 mảnh
                            existingCard.currentShards += 50;
                            Debug.Log($"[Shop] Quay trúng thẻ trùng: {data.cardName}. Hóa thành 50 mảnh!");
                        }
                        else
                        {
                            // NẾU MỚI TINH: Đẻ thẻ mới nhét vào kho
                            inventoryCards.Add(new OwnedCard(data));
                            Debug.Log($"[Shop] Nhân phẩm bùng nổ! Nhận thẻ mới: {data.cardName}");
                        }
                        isAnyCardUpdated = true;
                    }
                    else
                    {
                        Debug.LogWarning("[Shop] CardListManager đang ngủ!");
                    }

                    // Vẫn đẩy thẻ vào list UI để popup hiện lên cho sướng mắt
                    newCards.Add(data);
                }
            }
        }

        // F5 lại giao diện bộ bài (để thẻ mới hoặc số mảnh nhảy)
        if (isAnyCardUpdated && CardListManager.Instance != null)
        {
            CardListManager.Instance.DisplayCards();
            if (PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.SaveGameData();
        }

        // HIỂN THỊ UI BẢNG THƯỞNG GACHA
        if (RewardPopupManager.Instance != null)
        {
            RewardPopupManager.Instance.ShowCardRewards(newCards, "CHÚC MỪNG SẾP TRÚNG THƯỞNG");
        }
    }
}