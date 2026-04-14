using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.InputSystem;
using System;

// Các Class Save Data để nén JSON (Giữ nguyên như bản cũ)
[Serializable]
public class PlayFabSaveData
{
    public List<ItemSaveData> inventory = new List<ItemSaveData>();
    public List<CardSaveData> cards = new List<CardSaveData>();
}

[Serializable]
public class ItemSaveData { public string id; public int amt; }

[Serializable]
public class CardSaveData
{
    public int id;
    public int lvl;
    public string[] gems = new string[12];
}

public class PlayFabDataManager : MonoBehaviour
{
    public static PlayFabDataManager Instance { get; private set; }
    public bool enableDevCheats = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Sống xuyên Scene [cite: 600]
    }

    private void Update()
    {
        if (!enableDevCheats) return;

        // Check xem sếp có đang chạm vào bàn phím không
        if (Keyboard.current != null)
        {
            // Phím số 1: Bơm máu (Vàng) cho đại gia
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                // Dòng log này sẽ hiện màu vàng rực rỡ trong Console để sếp biết là code đã nhận!
                Debug.Log("<color=yellow>[Cheat System] Sếp Yami đã nhấn phím 1: Đang nạp 1000 Vàng...</color>");
                HackCurrency("GD", 1000);
            }

            // Phím số 2: Reset cuộc đời (Tạo acc mới)
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                // Dòng log này cảnh báo đỏ chót cho nó nguy hiểm
                Debug.Log("<color=red>[Cheat System] Sếp Yami đã nhấn phím 2: Khởi động quy trình đầu thai!</color>");
                Reincarnate();
            }
            // Phím số 3: Bơm Linh Ngọc cho đại gia
            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                // Dòng log này sẽ hiện màu vàng rực rỡ trong Console để sếp biết là code đã nhận!
                Debug.Log("<color=yellow>[Cheat System] Sếp Yami đã nhấn phím 3: Đang nạp 1000 Linh Ngọc...</color>");
                HackCurrency("GM", 100);
            }
        }
    }

    // ==========================================
    // LƯU DỮ LIỆU (SAVE)
    // ==========================================
    public void SaveGameData()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        PlayFabSaveData saveData = new PlayFabSaveData();

        // 1. Nén Inventory
        if (InventoryManager.Instance != null)
        {
            foreach (var item in InventoryManager.Instance.GetInventoryList())
            {
                if (item.data != null)
                    saveData.inventory.Add(new ItemSaveData { id = item.data.itemID, amt = item.amount });
            }
        }

        // 2. Nén OwnedCards & Ngọc đã khảm 
        if (CardListManager.Instance != null)
        {
            foreach (var card in CardListManager.Instance.GetOwnedCards())
            {
                CardSaveData cardSave = new CardSaveData { id = card.data.cardID, lvl = card.level };
                for (int i = 0; i < 12; i++)
                {
                    cardSave.gems[i] = (card.equippedGems[i] != null && card.equippedGems[i].data != null)
                                        ? card.equippedGems[i].data.itemID : "";
                }
                saveData.cards.Add(cardSave);
            }
        }

        string json = JsonUtility.ToJson(saveData);
        var request = new UpdateUserDataRequest { Data = new Dictionary<string, string> { { "UserSaveData", json } } };

        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log("<color=green>[PlayFab] Đã backup dữ liệu lên mây thành công!</color>"),
            error => Debug.LogError("[PlayFab] Lỗi Save: " + error.GenerateErrorReport()));
    }

    // ==========================================
    // TẢI DỮ LIỆU (LOAD)
    // ==========================================
    public void LoadGameData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            if (result.Data != null && result.Data.ContainsKey("UserSaveData"))
            {
                string json = result.Data["UserSaveData"].Value;
                PlayFabSaveData loadedData = JsonUtility.FromJson<PlayFabSaveData>(json);

                // --- 1. Hồi sinh TÚI ĐỒ (Inventory) --- 
                if (InventoryManager.Instance != null && ItemDatabase.Instance != null)
                {
                    InventoryManager.Instance.GetInventoryList().Clear();
                    foreach (var itemSave in loadedData.inventory)
                    {
                        var data = ItemDatabase.Instance.GetItemData(itemSave.id);
                        if (data != null) InventoryManager.Instance.AddItem(data, itemSave.amt, isSilent: true);
                    }
                    InventoryManager.Instance.RefreshUI();
                }

                // --- 2. Hồi sinh THẺ BÀI (Cards) --- 
                if (CardListManager.Instance != null && CardDatabase.Instance != null)
                {
                    var cardList = CardListManager.Instance.GetOwnedCards();
                    cardList.Clear();

                    foreach (var cardSave in loadedData.cards)
                    {
                        var cardData = CardDatabase.Instance.GetCardData(cardSave.id);
                        if (cardData != null)
                        {
                            OwnedCard newCard = new OwnedCard(cardData);
                            newCard.level = cardSave.lvl;

                            // Hồi sinh đống ngọc đã gắn trên thẻ này 
                            for (int i = 0; i < 12; i++)
                            {
                                if (!string.IsNullOrEmpty(cardSave.gems[i]))
                                {
                                    var gemData = ItemDatabase.Instance.GetItemData(cardSave.gems[i]);
                                    if (gemData != null) newCard.equippedGems[i] = new InventoryItem(gemData, 1);
                                }
                            }
                            cardList.Add(newCard);
                        }
                    }
                    CardListManager.Instance.DisplayCards();
                }
                Debug.Log("<color=cyan>[PlayFab] Đã đồng bộ toàn bộ tài sản từ server!</color>");
            }
        }, error => Debug.LogError("Lỗi Load: " + error.GenerateErrorReport()));
    }

    // --- HÀM HACK TIỀN CHO SẾP TEST (1) --- 
    private void HackCurrency(string currencyCode, int amount)
    {
        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            Debug.LogWarning("<color=orange>[Cheat] Bình tĩnh sếp ơi! PlayFab đang kết nối, đợi nó báo Xanh lá rồi hẵng hack!</color>");
            return;
        }

        var request = new AddUserVirtualCurrencyRequest { VirtualCurrency = currencyCode, Amount = amount };
        PlayFabClientAPI.AddUserVirtualCurrency(request,
            result =>
            {
                // Đợi server báo cộng tiền thành công xong...
                Debug.Log($"<color=yellow>[Cheat] Đã bơm {amount} {currencyCode}. Balance: {result.Balance}</color>");

                // ... THÌ MỚI GỌI HÀM KÉO TIỀN VỀ UI TẠI ĐÂY!
                FetchVirtualCurrencies();
            },
            error => Debug.LogError("Lỗi hack tiền: " + error.GenerateErrorReport())
        );
    }

    // --- HÀM ĐẦU THAI (2) --- 
    // --- HÀM ĐẦU THAI (2) --- 
    private void Reincarnate()
    {
        Debug.Log("<color=red>[Cheat System] Đang tẩy tủy... Xóa ID cũ!</color>");

        // 1. Xóa GUID trong bộ nhớ máy
        AuthService.ResetDeviceId();

        // 2. Tiêu diệt các thế lực "Bất tử" (DontDestroyOnLoad)
        if (GameServices.Instance != null) Destroy(GameServices.Instance.gameObject);
        if (PlayFabDataManager.Instance != null) Destroy(PlayFabDataManager.Instance.gameObject);

        // 3. Xóa trí nhớ của PlayFab SDK (Chống dính Session cũ)
        PlayFabClientAPI.ForgetAllCredentials();

        // 4. Mở cổng luân hồi
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }
    // ==========================================
    // LẤY SỐ DƯ TIỀN TỆ TỪ SERVER
    // ==========================================
    public void FetchVirtualCurrencies()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
        {
            int gold = 0;
            int gem = 0;

            // Đọc số dư từ Dictionary VirtualCurrency của PlayFab
            if (result.VirtualCurrency.ContainsKey("GD")) gold = result.VirtualCurrency["GD"];
            if (result.VirtualCurrency.ContainsKey("GM")) gem = result.VirtualCurrency["GM"];

            // Bắn ra UI
            if (CurrencyUIManager.Instance != null)
            {
                CurrencyUIManager.Instance.UpdateBalances(gold, gem);
            }

            Debug.Log($"<color=yellow>[PlayFab] Tài sản: {gold} Vàng | {gem} Linh Ngọc</color>");
        },
        error => Debug.LogError("[PlayFab] Lỗi lấy tiền tệ: " + error.GenerateErrorReport()));
    }
}