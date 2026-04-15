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

    [Header("UI Effects")]
    [SerializeField] private GameObject floatingTextPrefab;
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
                    SpawnFloatingText($"Không đủ {msg}!", btnTransform);

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

        foreach (var item in grantedItems)
        {
            if (int.TryParse(item.ItemId, out int cardId))
            {
                CardDataSO data = CardDatabase.Instance.GetCardData(cardId);
                if (data != null)
                {
                    // KIỂM TRA NULL TRƯỚC KHI ADD BÀI VÀO LIST
                    if (CardListManager.Instance != null)
                    {
                        CardListManager.Instance.GetOwnedCards().Add(new OwnedCard(data));
                    }
                    else
                    {
                        Debug.LogWarning("[Shop] CardListManager đang ngủ, không add vào List hiển thị được (Nhưng vẫn sẽ lưu lên Cloud)!");
                    }
                    newCards.Add(data);
                }
            }
        }

        // F5 lại giao diện bộ bài (để thẻ mới hiện ra)
        if (CardListManager.Instance != null) CardListManager.Instance.DisplayCards();

        // ☁️ LƯU LÊN MÂY NGAY LẬP TỨC!
        if (PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.SaveGameData();

        // HIỂN THỊ UI BẢNG THƯỞNG GACHA
        if (RewardPopupManager.Instance != null)
        {
            RewardPopupManager.Instance.ShowCardRewards(newCards, "CHÚC MỪNG SẾP TRÚNG THƯỞNG");
        }
    }
    // Hàm tự chế Text bay mượt mà bằng DOTween
    private void SpawnFloatingText(string message, Transform spawnPos)
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogWarning("[Shop] Thiếu Prefab Floating Text rồi sếp ơi!");
            return;
        }

        // Đẻ cục text ra ngay tại vị trí cái nút bấm
        GameObject go = Instantiate(floatingTextPrefab, spawnPos.position, Quaternion.identity, spawnPos.parent);

        // --- ÉP SCALE VỀ 0.1f---
        // Ép tạm về 0 trước để làm hiệu ứng phóng to
        go.transform.localScale = Vector3.zero;

        TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
        if (txt != null)
        {
            txt.text = message;
            txt.color = new Color(1f, 0.2f, 0.2f, 1f);

            // COMBO DOTWEEN 3 TRONG 1:
            // 1. Phóng to từ 0 lên 0.1f (Bốp 1 phát ra luôn)
            go.transform.DOScale(new Vector3(0.1f, 0.1f, 0.1f), 0.2f).SetEase(Ease.OutBack);

            // 2. Bay lên trên 150px
            go.transform.DOMoveY(go.transform.position.y + 150f, 1f).SetEase(Ease.OutCubic);

            // 3. Mờ dần Alpha về 0 rồi tự hủy
            txt.DOFade(0f, 1f).SetEase(Ease.InQuad).OnComplete(() => Destroy(go));
        }
    }
}