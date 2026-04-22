using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;

// ==========================================
// CÁC CLASS DỮ LIỆU JSON (DATA WRAPPERS)
// ==========================================
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
    public int star;
    public int shards;
    public string[] gems = new string[PlayFabDataManager.MAX_GEMS];
}

[Serializable]
public class DeckSaveData
{
    public int[] deckCardIDs = new int[PlayFabDataManager.MAX_DECK_SIZE] { -1, -1, -1, -1, -1 };
}

// ==========================================
// MANAGER CHÍNH
// ==========================================
public class PlayFabDataManager : MonoBehaviour
{
    public static PlayFabDataManager Instance { get; private set; }

    [Header("Developer Settings")]
    public bool enableDevCheats = true;

    // --- KHAI BÁO HẰNG SỐ (Chống Magic Strings/Numbers) ---
    public const int MAX_GEMS = 12;
    public const int MAX_DECK_SIZE = 5;
    private const string KEY_USER_DATA = "UserSaveData";
    private const string KEY_PLAYER_DECK = "PlayerDeck";
    private const string CURRENCY_GOLD = "GD";
    private const string CURRENCY_GEM = "GM";
    private const string CURRENCY_STAMINA = "EN";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (enableDevCheats) HandleDevCheats();
    }

    // ==========================================
    // LƯU DỮ LIỆU (SAVE)
    // ==========================================
    public void SaveGameData()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        // [KHIÊN BẢO VỆ]: Nếu đang ở Combat mà gọi Save thì chặn ngay!
        // Chỉ cho phép Save khi các Manager ở MenuScene đang tồn tại.
        if (CardListManager.Instance == null || InventoryManager.Instance == null)
        {
            Debug.LogWarning("[PlayFab] Đang ở chế độ Offline/Combat, từ chối Save để bảo toàn tài sản cho sếp!");
            return;
        }

        PlayFabSaveData saveData = new PlayFabSaveData();

        // Lấy dữ liệu đã đóng gói từ các Manager
        saveData.inventory = GetInventorySaveData();
        saveData.cards = GetCardsSaveData();

        string json = JsonUtility.ToJson(saveData);
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { KEY_USER_DATA, json } }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log("<color=green>[PlayFab] Đã backup dữ liệu lên mây thành công!</color>"),
            error => Debug.LogError("[PlayFab] Lỗi Save: " + error.GenerateErrorReport())
        );
    }

    public void SaveCurrentDeck(OwnedCard[] deckToSave)
    {
        DeckSaveData dataWrapper = new DeckSaveData();

        for (int i = 0; i < MAX_DECK_SIZE; i++)
        {
            bool hasCard = deckToSave[i] != null && deckToSave[i].data != null;
            dataWrapper.deckCardIDs[i] = hasCard ? deckToSave[i].data.cardID : -1;
        }

        string jsonDeck = JsonUtility.ToJson(dataWrapper);
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { KEY_PLAYER_DECK, jsonDeck } }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log("<color=green>Sếp Yami ơi, Deck (ID) đã lên mây an toàn!</color>"),
            error => Debug.LogError("Toang rồi sếp: " + error.GenerateErrorReport())
        );
    }

    // ==========================================
    // TẢI DỮ LIỆU (LOAD)
    // ==========================================
    public void LoadGameData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            // 1. Phục hồi Inventory & Cards
            if (result.Data != null && result.Data.ContainsKey(KEY_USER_DATA))
            {
                string json = result.Data[KEY_USER_DATA].Value;
                PlayFabSaveData loadedData = JsonUtility.FromJson<PlayFabSaveData>(json);

                RestoreInventory(loadedData.inventory);
                RestoreCards(loadedData.cards);

                Debug.Log("<color=cyan>[PlayFab] Đã đồng bộ toàn bộ tài sản từ server!</color>");
            }

            // 2. Phục hồi Deck
            if (result.Data != null && result.Data.ContainsKey(KEY_PLAYER_DECK))
            {
                string deckJson = result.Data[KEY_PLAYER_DECK].Value;
                RestoreDeck(JsonUtility.FromJson<DeckSaveData>(deckJson));
            }

        }, error => Debug.LogError("Lỗi Load: " + error.GenerateErrorReport()));
    }


    // ==========================================
    // LẤY SỐ DƯ TIỀN TỆ & THỜI GIAN HỒI THỂ LỰC
    // ==========================================
    public void FetchVirtualCurrencies()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
        {
            int gold = result.VirtualCurrency.ContainsKey(CURRENCY_GOLD) ? result.VirtualCurrency[CURRENCY_GOLD] : 0;
            int gem = result.VirtualCurrency.ContainsKey(CURRENCY_GEM) ? result.VirtualCurrency[CURRENCY_GEM] : 0;

            // 1. Lấy số dư Thể Lực
            int stamina = result.VirtualCurrency.ContainsKey(CURRENCY_STAMINA) ? result.VirtualCurrency[CURRENCY_STAMINA] : 0;

            // 2. Lấy số giây còn lại để hồi 1 Thể lực (Từ Server trả về, chống hack)
            int secondsToRecharge = 0;
            if (result.VirtualCurrencyRechargeTimes != null && result.VirtualCurrencyRechargeTimes.ContainsKey(CURRENCY_STAMINA))
            {
                secondsToRecharge = result.VirtualCurrencyRechargeTimes[CURRENCY_STAMINA].SecondsToRecharge;
            }

            // 3. Bắn toàn bộ Data sang UI
            if (CurrencyUIManager.Instance != null)
            {
                CurrencyUIManager.Instance.UpdateBalances(gold, gem);
                CurrencyUIManager.Instance.UpdateStamina(stamina, secondsToRecharge); // Hàm mới lát mình viết
            }

            Debug.Log($"<color=yellow>[PlayFab] Tài sản: {gold} Vàng | {gem} Ngọc | {stamina}/200 Thể lực</color>");
        },
        error => Debug.LogError("[PlayFab] Lỗi lấy tiền tệ: " + error.GenerateErrorReport()));
    }

    // ==========================================
    // CÁC HÀM HELPER TÁCH NHỎ (Clean Code)
    // ==========================================

    private List<ItemSaveData> GetInventorySaveData()
    {
        var list = new List<ItemSaveData>();
        if (InventoryManager.Instance == null) return list;

        foreach (var item in InventoryManager.Instance.GetInventoryList())
        {
            if (item.data != null) list.Add(new ItemSaveData { id = item.data.itemID, amt = item.amount });
        }
        return list;
    }

    private List<CardSaveData> GetCardsSaveData()
    {
        var list = new List<CardSaveData>();
        if (CardListManager.Instance == null) return list;

        foreach (var card in CardListManager.Instance.GetOwnedCards())
        {
            CardSaveData cardSave = new CardSaveData
            {
                id = card.data.cardID,
                lvl = card.level,
                star = card.starLevel,  
                shards = card.currentShards 
            };
            for (int i = 0; i < MAX_GEMS; i++)
            {
                bool hasGem = card.equippedGems[i] != null && card.equippedGems[i].data != null;
                cardSave.gems[i] = hasGem ? card.equippedGems[i].data.itemID : "";
            }
            list.Add(cardSave);
        }
        return list;
    }

    private void RestoreInventory(List<ItemSaveData> savedInventory)
    {
        if (InventoryManager.Instance == null || ItemDatabase.Instance == null) return;

        InventoryManager.Instance.GetInventoryList().Clear();
        foreach (var itemSave in savedInventory)
        {
            var data = ItemDatabase.Instance.GetItemData(itemSave.id);
            if (data != null) InventoryManager.Instance.AddItem(data, itemSave.amt, isSilent: true);
        }
        InventoryManager.Instance.RefreshUI();
    }

    private void RestoreCards(List<CardSaveData> savedCards)
    {
        if (CardListManager.Instance == null || CardDatabase.Instance == null || ItemDatabase.Instance == null) return;

        var cardList = CardListManager.Instance.GetOwnedCards();
        cardList.Clear();

        foreach (var cardSave in savedCards)
        {
            var cardData = CardDatabase.Instance.GetCardData(cardSave.id);
            if (cardData == null) continue;

            OwnedCard newCard = new OwnedCard(cardData)
            {
                level = cardSave.lvl,
                starLevel = cardSave.star, 
                currentShards = cardSave.shards 
            };

            for (int i = 0; i < MAX_GEMS; i++)
            {
                if (!string.IsNullOrEmpty(cardSave.gems[i]))
                {
                    var gemData = ItemDatabase.Instance.GetItemData(cardSave.gems[i]);
                    if (gemData != null) newCard.equippedGems[i] = new InventoryItem(gemData, 1);
                }
            }
            cardList.Add(newCard);
        }
        CardListManager.Instance.DisplayCards();
    }

    private void RestoreDeck(DeckSaveData loadedDeck)
    {
        if (DeckManager.Instance == null || CardListManager.Instance == null) return;

        var khoBaiCuaSep = CardListManager.Instance.GetOwnedCards();

        for (int i = 0; i < MAX_DECK_SIZE; i++)
        {
            int idCanTim = loadedDeck.deckCardIDs[i];
            if (idCanTim != -1)
            {
                var theBaiMocGoc = khoBaiCuaSep.Find(c => c.data.cardID == idCanTim);
                DeckManager.Instance.currentDeck[i] = theBaiMocGoc;
            }
            else
            {
                DeckManager.Instance.currentDeck[i] = null;
            }
        }

        DeckManager.Instance.RefreshDeckUI();
        Debug.Log("<color=green>[PlayFab] Đã xếp lại Đội Hình chuẩn xác!</color>");
    }

    // ==========================================
    // HỆ THỐNG CHEAT CHO DEV
    // ==========================================

    private void HandleDevCheats()
    {
        // --- Phần cũ cho PC ---
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) ExecuteCheat1();
            if (Keyboard.current.digit2Key.wasPressedThisFrame) ExecuteCheat2();
            if (Keyboard.current.digit3Key.wasPressedThisFrame) ExecuteCheat3();
        }

        // --- Phần mới cho Mobile (Input System Package) ---
        var touch = Touchscreen.current?.primaryTouch;
        if (touch != null && touch.press.wasPressedThisFrame && touch.tapCount.ReadValue() == 2)
        {
            float touchX = touch.position.ReadValue().x;
            float screenWidth = Screen.width;

            if (touchX < screenWidth / 3f)
                ExecuteCheat1(); // Chạm bên trái
            else if (touchX < (screenWidth * 2f) / 3f)
                ExecuteCheat2(); // Chạm ở giữa
            else
                ExecuteCheat3(); // Chạm bên phải
        }
    }


    private void ExecuteCheat1()
    {
        Debug.Log("<color=yellow>[Cheat] Sếp Yami Double Tap Trái: +1000 Vàng!</color>");
        HackCurrency(CURRENCY_GOLD, 1000);
    }

    private void ExecuteCheat2()
    {
        Debug.Log("<color=red>[Cheat] Sếp Yami Double Tap Giữa: Đầu thai thôi!</color>");
        Reincarnate();
    }

    private void ExecuteCheat3()
    {
        Debug.Log("<color=cyan>[Cheat] Sếp Yami Double Tap Phải: +100 Linh Ngọc!</color>");
        HackCurrency(CURRENCY_GEM, 100);
    }

    private void HackCurrency(string currencyCode, int amount)
        {
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                Debug.LogWarning("<color=orange>[Cheat] Bình tĩnh sếp ơi! PlayFab đang kết nối!</color>");
                return;
            }

            var request = new AddUserVirtualCurrencyRequest { VirtualCurrency = currencyCode, Amount = amount };
            PlayFabClientAPI.AddUserVirtualCurrency(request,
                result =>
                {
                    Debug.Log($"<color=yellow>[Cheat] Đã bơm {amount} {currencyCode}. Balance: {result.Balance}</color>");
                    FetchVirtualCurrencies();
                },
                error => Debug.LogError("Lỗi hack tiền: " + error.GenerateErrorReport())
            );
        }

    private void Reincarnate()
    {
        Debug.Log("<color=red>[Cheat System] Đang tẩy tủy... Xóa ID cũ!</color>");
        AuthService.ResetDeviceId();

        if (GameServices.Instance != null) Destroy(GameServices.Instance.gameObject);
        if (Instance != null) Destroy(gameObject);

        PlayFabClientAPI.ForgetAllCredentials();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }
    // ==========================================
    // AUTO-LOAD KHI VỀ LẠI MENU
    // ==========================================
    private void OnEnable()
    {
        // Đăng ký sự kiện: Mỗi khi load một Scene mới thì gọi hàm OnSceneLoaded
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Hủy đăng ký khi object bị hủy (Tránh leak memory)
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Nếu cái scene vừa load xong mang tên "MenuScene" và đã đăng nhập...
        if (scene.name == "MenuScene" && PlayFabClientAPI.IsClientLoggedIn())
        {
            Debug.Log("<color=cyan>[PlayFab] Sếp Yami vừa hạ phàm về Menu, đang tải lại toàn bộ cơ ngơi...</color>");

            // Kéo thẻ bài, deck và túi đồ về
            LoadGameData();

            // Kéo luôn tiền tệ (Vàng, Ngọc) về cho chắc cú
            FetchVirtualCurrencies();
        }
    }
}