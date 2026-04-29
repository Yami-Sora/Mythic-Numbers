using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;

// ==========================================
// MANAGER CHÍNH (ORCHESTRATOR)
// ==========================================
public class PlayFabDataManager : MonoBehaviour
{
    public static PlayFabDataManager Instance { get; private set; }

    #region [1] STATE & SETTINGS
    [Header("Player Data")]
    public int CurrentStage = 1; 
    public bool isDataLoaded = false;
    public string PlayerName { get; private set; } = "Anonymous"; 
    public int PlayerLevel = 1;
    public string PlayerTitle { get; set; } = "Tân Thủ";
    public long PlayerExp = 0;
    private bool isDirty = false; 
    private bool isDungeonDirty = false; 

    public enum GameMode { Story, GoldDungeon, LNDungeon, GemMine }
    public GameMode CurrentMode = GameMode.Story;

    // Dữ liệu Dungeon
    public int GoldDungeonStage = 1;
    public int LNDungeonStage = 1;
    public int GemMineStage = 1;
    public int GoldEntriesToday = 0;
    public int LNEntriesToday = 0;
    public int GemMineEntriesToday = 0;

    [Header("Developer Settings")]
    public bool enableDevCheats = true;

    // Bộ nhớ đệm tiền tệ (để check nhanh túi tiền)
    private int _goldBalance = 0;
    private int _lnBalance = 0;
    private PlayFabSaveData _cachedSaveData = new PlayFabSaveData(); // Cache để bảo toàn dữ liệu khi Save


    public int GetCurrencyBalance(string code)
    {
        if (code == PlayFabConstants.CURRENCY_GOLD) return _goldBalance;
        if (code == PlayFabConstants.CURRENCY_LN) return _lnBalance;
        return 0;
    }
    #endregion

    
    #region [PLAYER PROGRESSION]
    public long GetRequiredExp(int level)
    {
        if (level <= 1) return 100;
        // Mỗi level tăng 10% so với level trước: 100 * (1.1 ^ (level-1))
        return (long)(100 * Math.Pow(1.1, level - 1));
    }

    public void AddExp(long amount)
    {
        PlayerExp += amount;
        long req = GetRequiredExp(PlayerLevel);
        
        while (PlayerExp >= req)
        {
            PlayerExp -= req;
            PlayerLevel++;
            req = GetRequiredExp(PlayerLevel);
            Debug.Log($"<color=yellow>[Level Up] Chúc mừng sếp lên cấp {PlayerLevel}!</color>");
            
            // Cập nhật Stat lên PlayFab ngay khi lên cấp
            UpdatePlayerStatistics(PlayFabConstants.STAT_PLAYER_LEVEL, PlayerLevel);
        }

        MarkDirty();
        UpdatePlayerStatistics(PlayFabConstants.STAT_PLAYER_EXP, (int)PlayerExp);

        PlayerInfoUI.UpdateAllExpBar(PlayerLevel, PlayerExp, req);
    }

    public void UpdatePlayerStatistics(string statName, int value)
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate { StatisticName = statName, Value = value }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request, 
            res => Debug.Log($"<color=green>[PlayFab] Đã cập nhật Statistics {statName} = {value}</color>"),
            err => Debug.LogError("[PlayFab] Lỗi cập nhật Stat " + statName + ": " + err.GenerateErrorReport())
        );
    }

    public void FetchPlayerStatistics()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;


        var request = new GetPlayerStatisticsRequest
        {
            StatisticNames = new List<string> 
            { 
                PlayFabConstants.STAT_PLAYER_LEVEL, 
                PlayFabConstants.STAT_PLAYER_EXP, 
                PlayFabConstants.STAT_PLAYER_POWER 
            }
        };

        PlayFabClientAPI.GetPlayerStatistics(request, res =>
        {
            if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(false);

            if (res.Statistics != null)
            {
                foreach (var stat in res.Statistics)
                {
                    if (stat.StatisticName == PlayFabConstants.STAT_PLAYER_LEVEL) PlayerLevel = stat.Value;
                    if (stat.StatisticName == PlayFabConstants.STAT_PLAYER_EXP) PlayerExp = stat.Value;
                }

                PlayerInfoUI.UpdateAllExpBar(PlayerLevel, PlayerExp, GetRequiredExp(PlayerLevel));
            }
        }, err => {
            Debug.LogWarning("[PlayFab] Lỗi lấy Stats, đang thử lại sau 3s...");
            StartCoroutine(DelayRetry(FetchPlayerStatistics));
        });
    }
    #endregion

    #region [2] UNITY LIFECYCLE

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

    #endregion

    #region [3] QUẢN LÝ TRẠNG THÁI (DIRTY CHECKING)
    public void MarkDirty()
    {
        isDirty = true;
        Debug.Log("[PlayFab] Dữ liệu đã thay đổi, chuẩn bị Save khi chuyển Tab.");
    }

    public void MarkDungeonDirty()
    {
        isDungeonDirty = true;
    }

    #endregion

    #region [4] LƯU DỮ LIỆU (SAVE)
    public void SaveGameData()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;
        if (!isDataLoaded)
        {
            Debug.LogWarning("[PlayFab] Từ chối Save vì dữ liệu chưa được Load xong từ Server (tránh ghi đè dữ liệu rỗng)!");
            return;
        }

        if (!isDirty) return;

        // [NÂNG CẤP]: Thay vì chặn hoàn toàn, ta sẽ dùng cache để "vá" những phần Manager đang null
        if (CardListManager.Instance == null && InventoryManager.Instance == null && _cachedSaveData.inventory == null)
        {
            Debug.LogWarning("[PlayFab] Không có dữ liệu để Save (Managers null và Cache trống)!");
            return;
        }



        // 1. Cập nhật Inventory (Nếu Manager đang mở thì lấy từ Manager, không thì giữ nguyên cache)
        if (InventoryManager.Instance != null)
        {
            _cachedSaveData.inventory = GetInventorySaveData();
        }

        // 2. Cập nhật Cards (Nếu Manager đang mở thì lấy từ Manager, không thì giữ nguyên cache)
        if (CardListManager.Instance != null)
        {
            _cachedSaveData.cards = GetCardsSaveData();
        }

        // 3. Cập nhật các thông số khác
        _cachedSaveData.level = PlayerLevel;
        _cachedSaveData.exp = PlayerExp;

        string json = JsonUtility.ToJson(_cachedSaveData);
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> 
            { 
                { PlayFabConstants.KEY_USER_DATA, json },
                { PlayFabConstants.KEY_CURRENT_STAGE, CurrentStage.ToString() }
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => {
                if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(false);
                Debug.Log("<color=green>[PlayFab] Đã backup dữ liệu lên mây thành công!</color>");
                isDirty = false; // Reset cờ sau khi save thành công
            },
            error => {
                Debug.LogWarning("[PlayFab] Lỗi Save dữ liệu, đang thử lại sau 3s...");
                StartCoroutine(DelayRetry(SaveGameData));
            }
        );
    }

    public void SaveStageDataOnly()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;


        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> 
            { 
                { PlayFabConstants.KEY_CURRENT_STAGE, CurrentStage.ToString() }
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => {
                if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(false);
                Debug.Log($"<color=green>[PlayFab] Đã backup Ải {CurrentStage} lên mây thành công!</color>");
            },
            error => {
                Debug.LogWarning("[PlayFab] Lỗi Save Stage, đang thử lại sau 3s...");
                StartCoroutine(DelayRetry(SaveStageDataOnly));
            }
        );
    }

    public void SaveDungeonDataOnly()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        if (!isDungeonDirty)
        {
            // Debug.Log("[PlayFab] Dữ liệu Dungeon không đổi, không cần Save.");
            return;
        }

        DungeonSaveData dData = new DungeonSaveData
        {
            goldStage = GoldDungeonStage,
            lnStage = LNDungeonStage,
            gemMineStage = GemMineStage,
            goldEntries = GoldEntriesToday,
            lnEntries = LNEntriesToday,
            gemMineEntries = GemMineEntriesToday,
            lastDate = DateTime.Now.ToString("yyyy-MM-dd")
        };

        string json = JsonUtility.ToJson(dData);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> 
            { 
                { PlayFabConstants.KEY_DUNGEON_DATA, json }
            }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => {
                Debug.Log("<color=green>[PlayFab] Đã backup dữ liệu Dungeon lên mây!</color>");
                isDungeonDirty = false;
            },
            error => Debug.LogError("[PlayFab] Lỗi Save Dungeon: " + error.GenerateErrorReport())
        );
    }

    #endregion

    public void SaveCurrentDeck(OwnedCard[] deckToSave)
    {
        DeckSaveData dataWrapper = new DeckSaveData();

        for (int i = 0; i < PlayFabConstants.MAX_DECK_SIZE; i++)
        {
            bool hasCard = deckToSave[i] != null && deckToSave[i].data != null;
            dataWrapper.deckCardIDs[i] = hasCard ? deckToSave[i].data.cardID : -1;
        }

        string jsonDeck = JsonUtility.ToJson(dataWrapper);
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { PlayFabConstants.KEY_PLAYER_DECK, jsonDeck } }
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log("<color=green>Sếp Yami ơi, Deck (ID) đã lên mây an toàn!</color>"),
            error => Debug.LogError("Toang rồi sếp: " + error.GenerateErrorReport())
        );
    }

    #region [5] TẢI DỮ LIỆU (LOAD)
    public void LoadGameData()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;


        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(false);

            // 1. Phục hồi Inventory & Cards
            if (result.Data != null && result.Data.ContainsKey(PlayFabConstants.KEY_USER_DATA))
            {
                string json = result.Data[PlayFabConstants.KEY_USER_DATA].Value;
                _cachedSaveData = JsonUtility.FromJson<PlayFabSaveData>(json);

                RestoreInventory(_cachedSaveData.inventory);
                RestoreCards(_cachedSaveData.cards);
                this.PlayerLevel = _cachedSaveData.level > 0 ? _cachedSaveData.level : 1;
                this.PlayerExp = _cachedSaveData.exp;
            }

            // Kéo luôn Statistics về cho chuẩn
            FetchPlayerStatistics();

            // 1.5 Phục hồi CurrentStage từ Key riêng
            if (result.Data != null && result.Data.ContainsKey(PlayFabConstants.KEY_CURRENT_STAGE))
            {
                if (int.TryParse(result.Data[PlayFabConstants.KEY_CURRENT_STAGE].Value, out int stage))
                {
                    this.CurrentStage = stage > 0 ? stage : 1;
                }
            }
            else
            {
                this.CurrentStage = 1;
            }

            Debug.Log($"<color=cyan>[PlayFab] Đã đồng bộ toàn bộ tài sản từ server! Đang ở Ải: {CurrentStage}</color>");

            // 2. Phục hồi Deck
            if (result.Data != null && result.Data.ContainsKey(PlayFabConstants.KEY_PLAYER_DECK))
            {
                string deckJson = result.Data[PlayFabConstants.KEY_PLAYER_DECK].Value;
                RestoreDeck(JsonUtility.FromJson<DeckSaveData>(deckJson));
            }

            // 2. Phục hồi dữ liệu Dungeon & Kiểm tra Reset ngày
            if (result.Data != null && result.Data.ContainsKey(PlayFabConstants.KEY_DUNGEON_DATA))
            {
                string json = result.Data[PlayFabConstants.KEY_DUNGEON_DATA].Value;
                DungeonSaveData loadedData = JsonUtility.FromJson<DungeonSaveData>(json);
                
                GoldDungeonStage = Mathf.Max(1, loadedData.goldStage);
                LNDungeonStage = Mathf.Max(1, loadedData.lnStage);
                GemMineStage = Mathf.Max(1, loadedData.gemMineStage);

                string today = DateTime.Now.ToString("yyyy-MM-dd");
                if (loadedData.lastDate != today)
                {
                    GoldEntriesToday = 0;
                    LNEntriesToday = 0;
                    GemMineEntriesToday = 0;
                }
                else
                {
                    GoldEntriesToday = loadedData.goldEntries;
                    LNEntriesToday = loadedData.lnEntries;
                    GemMineEntriesToday = loadedData.gemMineEntries;
                }
            }

            isDataLoaded = true; // Đánh dấu đã Load xong toàn bộ dữ liệu

        }, error => {
            Debug.LogWarning("[PlayFab] Lỗi Load dữ liệu, đang thử lại sau 3s...");
            StartCoroutine(DelayRetry(LoadGameData));
        });
    }

    #endregion

    #region [6] LOGIC PHẦN THƯỞNG & TIỀN TỆ
    public void ClaimStageReward(Action<int, bool, int> onSuccess)
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        int goldReward = 0;
        int lnReward = 0;
        ItemDataSO droppedGem = null;
        bool isBoss = false;
        int currentStage = 0;

        if (CurrentMode == GameMode.Story)
        {
            currentStage = CurrentStage;
            goldReward = Mathf.RoundToInt(50 * (1 + 0.05f * (currentStage - 1)));
            isBoss = (currentStage % 10 == 0);
            lnReward = isBoss ? 1 : 0;
            CurrentStage++;
        }
        else if (CurrentMode == GameMode.GoldDungeon)
        {
            currentStage = GoldDungeonStage;
            goldReward = Mathf.RoundToInt(100 * (1 + 0.1f * (currentStage - 1)));
            lnReward = 0;
            GoldDungeonStage++;
            GoldEntriesToday++;
        }
        else if (CurrentMode == GameMode.LNDungeon)
        {
            currentStage = LNDungeonStage;
            goldReward = 0;
            lnReward = Mathf.RoundToInt(20 * (1 + 0.1f * (currentStage - 1)));
            LNDungeonStage++;
            LNEntriesToday++;
        }
        else if (CurrentMode == GameMode.GemMine)
        {
            currentStage = GemMineStage;
            goldReward = 0;
            lnReward = 0;

            // Logic rớt Gem: Mỗi 10 ải tăng 1 phẩm chất màu
            int colorIndex = ((currentStage - 1) / 10) + 1; // 1-White, 2-Green...
            colorIndex = Mathf.Clamp(colorIndex, 1, 7); // Max là Red (7)
            ColorLv targetColor = (ColorLv)colorIndex;

            droppedGem = ItemDatabase.Instance.GetRandomGemByColor(targetColor);
            GemMineStage++;
            GemMineEntriesToday++;
        }

        // Thực hiện cộng tiền lên Server
        var requests = new List<AddUserVirtualCurrencyRequest>();
        if (goldReward > 0) requests.Add(new AddUserVirtualCurrencyRequest { VirtualCurrency = PlayFabConstants.CURRENCY_GOLD, Amount = goldReward });
        if (lnReward > 0) requests.Add(new AddUserVirtualCurrencyRequest { VirtualCurrency = PlayFabConstants.CURRENCY_LN, Amount = lnReward });

        // Chạy tuần tự các request cộng tiền
        void ProcessRequest(int index)
        {
            if (index >= requests.Count)
            {
                if (CurrentMode == GameMode.Story) 
                {
                    SaveStageDataOnly();
                    isDirty = false; // Vì Stage đã save riêng rồi
                }
                else if (CurrentMode == GameMode.GemMine)
                {
                    if (droppedGem != null)
                    {
                        InventoryManager.Instance.AddItem(droppedGem, 1, isSilent: true);
                        MarkDirty();
                        SaveGameData();
                    }
                    MarkDungeonDirty();
                    SaveDungeonDataOnly();
                    isDungeonDirty = false;
                }
                else 
                {
                    MarkDungeonDirty();
                    SaveDungeonDataOnly();
                    isDungeonDirty = false;
                }

                FetchVirtualCurrencies();
            FetchPlayerProfile();
                onSuccess?.Invoke(goldReward, isBoss, lnReward);
                return;
            }

            PlayFabClientAPI.AddUserVirtualCurrency(requests[index], res => ProcessRequest(index + 1), err => ProcessRequest(index + 1));
        }

        ProcessRequest(0);
    }
    public void FetchVirtualCurrencies()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;


        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), result =>
        {
            if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(false);

            int gold = result.VirtualCurrency.ContainsKey(PlayFabConstants.CURRENCY_GOLD) ? result.VirtualCurrency[PlayFabConstants.CURRENCY_GOLD] : 0;
            int ln = result.VirtualCurrency.ContainsKey(PlayFabConstants.CURRENCY_LN) ? result.VirtualCurrency[PlayFabConstants.CURRENCY_LN] : 0;

            _goldBalance = gold;
            _lnBalance = ln;

            // 1. Lấy số dư Thể Lực
            int stamina = result.VirtualCurrency.ContainsKey(PlayFabConstants.CURRENCY_STAMINA) ? result.VirtualCurrency[PlayFabConstants.CURRENCY_STAMINA] : 0;

            // 2. Lấy số giây còn lại để hồi 1 Thể lực (Từ Server trả về, chống hack)
            int secondsToRecharge = 0;
            if (result.VirtualCurrencyRechargeTimes != null && result.VirtualCurrencyRechargeTimes.ContainsKey(PlayFabConstants.CURRENCY_STAMINA))
            {
                secondsToRecharge = result.VirtualCurrencyRechargeTimes[PlayFabConstants.CURRENCY_STAMINA].SecondsToRecharge;
            }

            // 3. Bắn toàn bộ Data sang UI
            if (CurrencyUIManager.Instance != null)
            {
                CurrencyUIManager.Instance.UpdateBalances(gold, ln);
                CurrencyUIManager.Instance.UpdateStamina(stamina, secondsToRecharge);
            }

            Debug.Log($"<color=yellow>[PlayFab] Tài sản: {gold} Vàng | {ln} Linh Ngọc | {stamina}/200 Thể lực</color>");
        },
        error => {
            Debug.LogWarning("[PlayFab] Lỗi lấy tiền tệ, đang thử lại sau 3s...");
            StartCoroutine(DelayRetry(FetchVirtualCurrencies));
        });
    }

    #endregion

    #region [7] HELPER METHODS (DATA CONVERSION)

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
            for (int i = 0; i < PlayFabConstants.MAX_GEMS; i++)
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

            for (int i = 0; i < PlayFabConstants.MAX_GEMS; i++)
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

        for (int i = 0; i < PlayFabConstants.MAX_DECK_SIZE; i++)
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
        LocalDeckContext.SetDeck(DeckManager.Instance.currentDeck);
        Debug.Log("<color=green>[PlayFab] Đã xếp lại Đội Hình chuẩn xác!</color>");
    }

    #endregion

    #region [8] HỆ THỐNG CHEAT (DEV ONLY)

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
        HackCurrency(PlayFabConstants.CURRENCY_GOLD, 1000);
    }

    private void ExecuteCheat2()
    {
        Debug.Log("<color=red>[Cheat] Sếp Yami Double Tap Giữa: Đầu thai thôi!</color>");
        Reincarnate();
    }

    private void ExecuteCheat3()
    {
        Debug.Log("<color=cyan>[Cheat] Sếp Yami Double Tap Phải: +100 Linh Ngọc!</color>");
        HackCurrency(PlayFabConstants.CURRENCY_LN, 100);
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
                FetchPlayerProfile();
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
    #endregion

    #region [9] AUTO-LOAD LOGIC
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
            FetchPlayerProfile();
        }
    }
    #endregion

    public void FetchPlayerProfile()
    {
        if (!PlayFab.PlayFabClientAPI.IsClientLoggedIn()) return;

        // Hiện Loading để chặn sếp bấm lung tung khi đang load profile


        PlayFab.PlayFabClientAPI.GetAccountInfo(new PlayFab.ClientModels.GetAccountInfoRequest(), res =>
        {
            if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(false);

            if (res.AccountInfo != null && res.AccountInfo.TitleInfo != null && !string.IsNullOrEmpty(res.AccountInfo.TitleInfo.DisplayName))
            {
                PlayerName = res.AccountInfo.TitleInfo.DisplayName;
            }
            else
            {
                PlayerName = "Sếp Yami"; 
            }

            PlayerInfoUI.UpdateAllPlayerName(PlayerName);
            PlayerInfoUI.UpdateAllExpBar(PlayerLevel, PlayerExp, GetRequiredExp(PlayerLevel));
        }, err => {
            Debug.LogWarning("[PlayFab] Lỗi lấy Profile, đang thử lại sau 3s...");
            StartCoroutine(DelayRetry(FetchPlayerProfile));
        });
    }

    private IEnumerator DelayRetry(Action action)
    {
        if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(true);
        yield return new WaitForSeconds(3f);
        action?.Invoke();
    }
}