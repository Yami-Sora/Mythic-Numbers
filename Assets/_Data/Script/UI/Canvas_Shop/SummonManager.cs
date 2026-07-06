using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;

public class SummonManager : MonoBehaviour
{
    public static SummonManager Instance { get; private set; }

    [SerializeField] private Transform normalPackBtn;
    [SerializeField] private Transform normalPackX10Btn;
    [SerializeField] private Transform premiumPackBtn;
    [SerializeField] private Transform premiumPackX10Btn;
    [SerializeField] private Transform gemPackBtn;
    [SerializeField] private Transform gemPackX10Btn;
    [SerializeField] private Transform gemPremiumPackBtn;
    [SerializeField] private Transform gemPremiumPackX10Btn;

    private Dictionary<string, ItemDataSO> _itemDatabase = new Dictionary<string, ItemDataSO>();
    private bool _isBuying = false;

    private void Awake() 
    { 
        Instance = this; 
        LoadItemDatabase();
    }

    private void LoadItemDatabase()
    {
        ItemDataSO[] items = Resources.LoadAll<ItemDataSO>("ItemDataSO");
        foreach (var it in items)
        {
            if (!string.IsNullOrEmpty(it.itemID) && !_itemDatabase.ContainsKey(it.itemID))
            {
                _itemDatabase.Add(it.itemID, it);
            }
        }
        Debug.Log($"[Summon] Đã nạp {_itemDatabase.Count} vật phẩm vào database triệu hồi.");
    }

    public void BuyNormalPack() => BuyAndOpenPack("Pack_Normal", "GD", 100, 1, normalPackBtn);
    public void BuyNormalPackX10() => BuyAndOpenPack("Pack_Normal_x10", "GD", 1000, 10, normalPackX10Btn);
    
    public void BuyPremiumPack() => BuyAndOpenPack("Pack_Premium", "GM", 50, 1, premiumPackBtn);
    public void BuyPremiumPackX10() => BuyAndOpenPack("Pack_Premium_x10", "GM", 500, 10, premiumPackX10Btn);

    public void BuyGemPack() => BuyAndOpenPack("Pack_Gem_Lv1", "GD", 100, 1, gemPackBtn);
    public void BuyGemPackX10() => BuyAndOpenPack("Pack_Gem_Lv1_x10", "GD", 1000, 10, gemPackX10Btn);
    
    public void BuyGemPremiumPack() => BuyAndOpenPack("Pack_Gem_Premium_Lv1", "GM", 50, 1, gemPremiumPackBtn);
    public void BuyGemPremiumPackX10() => BuyAndOpenPack("Pack_Gem_Premium_Lv1_x10", "GM", 500, 10, gemPremiumPackX10Btn);

    private void BuyAndOpenPack(string catalogItemId, string currencyCode, int price, int quantity, Transform btnTransform)
    {
        if (_isBuying) return;

        if (PlayFabDataManager.Instance != null)
        {
            int currentBalance = PlayFabDataManager.Instance.GetCurrencyBalance(currencyCode);
            if (currentBalance < price)
            {
                string msg = currencyCode == "GD" ? "Không đủ Vàng!" : "Không đủ Linh Ngọc!";
                if (btnTransform != null) btnTransform.DOShakePosition(0.5f, 10, 10);
                if (VFXManager.Instance != null && btnTransform != null) VFXManager.Instance.SpawnFloatingText(msg, btnTransform);
                return;
            }
        }

        _isBuying = true;

        var purchaseReq = new PurchaseItemRequest
        {
            CatalogVersion = "MainCatalog",
            ItemId = catalogItemId,
            VirtualCurrency = currencyCode,
            Price = price
        };

        if (btnTransform != null) btnTransform.DOScale(0.9f, 0.1f).SetLoops(2, LoopType.Yoyo);

        PlayFabClientAPI.PurchaseItem(purchaseReq,
            buyRes => {
                if (PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.FetchVirtualCurrencies();
                
                var results = ProcessItemsSilently(buyRes.Items);
                List<ItemInstance> containersToOpen = buyRes.Items.FindAll(i => !string.IsNullOrEmpty(i.ItemInstanceId) && i.ItemClass == "Container");

                if (containersToOpen.Count > 0)
                {
                    StartCoroutine(UnlockMultipleContainers(containersToOpen, results.cards, results.gems));
                }
                else
                {
                    ShowFinalRewards(results.cards, results.gems);
                }
            },
            error => HandlePurchaseError(error, currencyCode, btnTransform)
        );
    }

    private IEnumerator UnlockMultipleContainers(List<ItemInstance> containers, List<CardDataSO> cards, List<ItemDataSO> gems)
    {
        List<CardDataSO> allCards = new List<CardDataSO>(cards);
        List<ItemDataSO> allGems = new List<ItemDataSO>(gems);
        List<ItemInstance> nextBatch = new List<ItemInstance>();

        foreach (var container in containers)
        {
            bool currentRequestDone = false;
            var unlockReq = new UnlockContainerInstanceRequest { CatalogVersion = "MainCatalog", ContainerItemInstanceId = container.ItemInstanceId };

            PlayFabClientAPI.UnlockContainerInstance(unlockReq,
                unlockRes => {
                    var res = ProcessItemsSilently(unlockRes.GrantedItems);
                    allCards.AddRange(res.cards);
                    allGems.AddRange(res.gems);
                    
                    var sub = unlockRes.GrantedItems.FindAll(i => i.ItemClass == "Container");
                    if (sub.Count > 0) nextBatch.AddRange(sub);
                    currentRequestDone = true;
                },
                error => {
                    Debug.LogError($"[Summon] Lỗi khui hộp: {error.ErrorMessage}");
                    currentRequestDone = true;
                }
            );

            while (!currentRequestDone) yield return null;
            yield return new WaitForSeconds(0.05f);
        }

        if (nextBatch.Count > 0)
            yield return StartCoroutine(UnlockMultipleContainers(nextBatch, allCards, allGems));
        else
            ShowFinalRewards(allCards, allGems);
    }

    private void HandlePurchaseError(PlayFabError error, string currency, Transform btn)
    {
        _isBuying = false;
        
        // Xác định xem có phải lỗi thiếu tiền không
        bool isInsufficient = (error == null) || (error.Error == PlayFabErrorCode.InsufficientFunds);
        string logMsg = error != null ? error.ErrorMessage : "Thiếu tiền (Client check)";
        
        Debug.LogError($"[Summon] Lỗi triệu hồi: {logMsg}");
        
        if (btn != null) 
        {
            btn.DOShakePosition(0.5f, 10f);
            if (VFXManager.Instance != null) 
            {
                string floatingMsg = isInsufficient ? "KHÔNG ĐỦ TIỀN!" : "LỖI TRIỆU HỒI!";
                VFXManager.Instance.SpawnFloatingText(floatingMsg, btn);
            }
        }
    }

    private (List<CardDataSO> cards, List<ItemDataSO> gems) ProcessItemsSilently(List<ItemInstance> grantedItems)
    {
        List<CardDataSO> cards = new List<CardDataSO>();
        List<ItemDataSO> gems = new List<ItemDataSO>();
        bool isDirty = false;

        foreach (var item in grantedItems)
        {
            // 1. ƯU TIÊN KIỂM TRA THEO CHUỖI ID (Dành cho Gem/Prop có ID như "01", "02")
            if (_itemDatabase.TryGetValue(item.ItemId, out ItemDataSO itemData))
            {
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.AddItem(itemData, 1, true);
                    isDirty = true;
                }
                if (itemData.type == ItemDataSO.ItemType.Gem) gems.Add(itemData);
            }
            // 2. NẾU KHÔNG THẤY THÌ MỚI THỬ PARSE SANG CARD (ID LÀ SỐ "1", "2")
            else if (int.TryParse(item.ItemId, out int cardId))
            {
                CardDataSO data = CardDatabase.Instance.GetCardData(cardId);
                if (data != null)
                {
                    if (CardListManager.Instance != null)
                    {
                        var inventoryCards = CardListManager.Instance.GetOwnedCards();
                        var existingCard = inventoryCards.Find(c => c.data.cardID == data.cardID);
                        if (existingCard != null) existingCard.currentShards += 50;
                        else inventoryCards.Add(new OwnedCard(data));
                        isDirty = true;
                    }
                    cards.Add(data);
                }
            }
        }

        if (isDirty && PlayFabDataManager.Instance != null) PlayFabDataManager.Instance.MarkDirty();
        return (cards, gems);
    }

    private void ShowFinalRewards(List<CardDataSO> allCards, List<ItemDataSO> allGems)
    {
        _isBuying = false;
        if (CardListManager.Instance != null) CardListManager.Instance.DisplayCards();
        if (InventoryManager.Instance != null) InventoryManager.Instance.RefreshUI();

        Debug.Log($"[Summon] Kết quả gacha: {allCards.Count} Card, {allGems.Count} Gem.");

        if (GachaPopupManager.Instance != null)
        {
            GachaPopupManager.Instance.ShowGachaRewards(allCards, allGems, "KẾT QUẢ TRIỆU HỒI");
        }
        else
        {
            GachaPopupManager gp = GameObject.FindFirstObjectByType<GachaPopupManager>(FindObjectsInactive.Include); 
            if (gp != null)
            {
                gp.ShowGachaRewards(allCards, allGems, "KẾT QUẢ TRIỆU HỒI");
            }
            else
            {
                Debug.LogWarning("[Summon] Không tìm thấy GachaPopupManager trong Scene! Đang dùng RewardPopup cũ làm fallback.");
                if (RewardPopupManager.Instance != null)
                {
                    if (allCards.Count > 0) RewardPopupManager.Instance.ShowCardRewards(allCards, "KẾT QUẢ TRIỆU HỒI");
                    else if (allGems.Count > 0) 
                    {
                        List<InventoryItem> gemItems = new List<InventoryItem>();
                        foreach(var g in allGems) gemItems.Add(new InventoryItem(g, 1));
                        RewardPopupManager.Instance.ShowRewards(gemItems, "KẾT QUẢ QUAY TINH THẠCH");
                    }
                }
            }
        }
    }

    public void CloseCanvas()
    {
        if (Canvas_NavigationManager.Instance != null)
            Canvas_NavigationManager.Instance.SwitchTab(Canvas_NavigationManager.TabType.Combat);
    }
}