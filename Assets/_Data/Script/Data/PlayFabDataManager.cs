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
    public string PlayerName { get; private set; } = ""; 
    public int PlayerLevel = 1;
    public string PlayerTitle { get; set; } = "Tân Thủ";
    public long PlayerExp = 0;
    public int PlayerPower { get; private set; } = 0; 
    public int PlayerRank = 0; // Hạng Arena hiện tại
    public int Elo = 0;
    public int Wins = 0;
    public int Losses = 0;
    public int TotalGames = 0;
    public string ArenaDisplayName; // Tên hiển thị trong Arena
    private bool isDirty = false; 
    private bool isDungeonDirty = false; 

    public enum GameMode { Story, GoldDungeon, LNDungeon, GemMine, Arena }
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
                PlayFabConstants.STAT_PLAYER_POWER,
                "elo", "wins", "losses", "totalGames" // Lấy thêm điểm Arena
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
                    
                    // Gán cho các biến Arena mới
                    if (stat.StatisticName == "elo") Elo = stat.Value;
                    if (stat.StatisticName == "wins") Wins = stat.Value;
                    if (stat.StatisticName == "losses") Losses = stat.Value;
                    if (stat.StatisticName == "totalGames") TotalGames = stat.Value;
                }

                PlayerInfoUI.UpdateAllExpBar(PlayerLevel, PlayerExp, GetRequiredExp(PlayerLevel));
                PlayerInfoUI.UpdateAllElo(Elo); // Cập nhật Elo lên toàn bộ UI
            }
        }, err => {
            Debug.LogWarning("[PlayFab] Lỗi lấy Stats, đang thử lại sau 3s...");
            StartCoroutine(DelayRetry(FetchPlayerStatistics));
        });
    }

    public void FetchMyRank()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        var request = new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = "elo",
            MaxResultsCount = 1 
        };

        PlayFabClientAPI.GetLeaderboardAroundPlayer(request, result =>
        {
            var me = result.Leaderboard?.Find(e => e.PlayFabId == PlayFabSettings.staticPlayer.PlayFabId);
            if (me != null)
            {
                this.PlayerRank = me.Position + 1;
                this.Elo = me.StatValue;
                this.ArenaDisplayName = PlayerInfoUI.GetSafeName(!string.IsNullOrEmpty(me.DisplayName) ? me.DisplayName : this.PlayerName);
                
                PlayerInfoUI.UpdateAllArenaInfo(PlayerRank, ArenaDisplayName, Elo, PlayerTitle, CalculateTotalPower());
                Debug.Log($"<color=white>[PlayFab] Đã dò thấy hạng: {PlayerRank}</color>");
            }
        }, error =>
        {
            Debug.LogWarning("[PlayFab] Không thể dò hạng: " + error.GenerateErrorReport());
        });
    }

    public void FetchAccountInfo()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(), result =>
        {
            if (result.AccountInfo != null && result.AccountInfo.TitleInfo != null)
            {
                string dName = result.AccountInfo.TitleInfo.DisplayName;
                if (!string.IsNullOrEmpty(dName))
                {
                    this.ArenaDisplayName = dName;
                    PlayerInfoUI.UpdateAllArenaInfo(PlayerRank, ArenaDisplayName, Elo, PlayerTitle, CalculateTotalPower());
                    Debug.Log($"<color=white>[PlayFab] Tên hiển thị chuẩn: {ArenaDisplayName}</color>");
                }
            }
        }, null);
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
        
        // Cập nhật Arena Data vào SaveData để đồng bộ hóa
        _cachedSaveData.elo = Elo;
        _cachedSaveData.wins = Wins;
        _cachedSaveData.losses = Losses;
        _cachedSaveData.totalGames = TotalGames;
        _cachedSaveData.rank = PlayerRank;
        _cachedSaveData.displayName = ArenaDisplayName;

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

                // RẤT QUAN TRỌNG: Cập nhật luôn Public Profile (lực chiến, tên, v.v.)
                // vì có thể người chơi vừa nâng cấp thẻ bài trong Canvas_Cards
                UpdatePublicProfile();
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
            if (hasCard)
            {
                dataWrapper.snapshots[i] = new CardSnapshot
                {
                    id = deckToSave[i].data.cardID,
                    star = deckToSave[i].starLevel,
                    top = deckToSave[i].GetTotalTop(),
                    right = deckToSave[i].GetTotalRight(),
                    bottom = deckToSave[i].GetTotalBottom(),
                    left = deckToSave[i].GetTotalLeft()
                };
            }
            else
            {
                dataWrapper.snapshots[i] = new CardSnapshot { id = -1 };
            }
        }

        string jsonDeck = JsonUtility.ToJson(dataWrapper);
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { PlayFabConstants.KEY_PLAYER_DECK, jsonDeck } },
            Permission = UserDataPermission.Public // Deck phải Public để đối thủ có thể đọc được trong Arena
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => {
                Debug.Log("<color=green>Sếp Yami ơi, Deck (Snapshot) đã lên mây an toàn!</color>");
                // Sau khi lưu Deck thành công, cập nhật luôn Public Profile
                UpdatePublicProfile(dataWrapper);
            },
            error => Debug.LogError("Toang rồi sếp: " + error.GenerateErrorReport())
        );
    }

    /// <summary>
    /// Cập nhật "Hộ chiếu" công khai của người chơi. 
    /// Gom tất cả: Tên, Level, Avatar, Frame và Deck hiện tại vào 1 nơi duy nhất.
    /// </summary>
    public void UpdatePublicProfile(DeckSaveData currentDeck = null)
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        // Nếu không truyền deck vào, chúng ta sẽ tự tạo snapshot từ DeckManager hiện tại
        if (currentDeck == null && DeckManager.Instance != null)
        {
            currentDeck = new DeckSaveData();
            var deckCards = DeckManager.Instance.currentDeck;
            for (int i = 0; i < PlayFabConstants.MAX_DECK_SIZE; i++)
            {
                if (deckCards[i] != null && deckCards[i].data != null)
                {
                    currentDeck.snapshots[i] = new CardSnapshot {
                        id = deckCards[i].data.cardID,
                        star = deckCards[i].starLevel,
                        top = deckCards[i].GetTotalTop(),
                        right = deckCards[i].GetTotalRight(),
                        bottom = deckCards[i].GetTotalBottom(),
                        left = deckCards[i].GetTotalLeft()
                    };
                }
                else currentDeck.snapshots[i] = new CardSnapshot { id = -1 };
            }
        }

        // Sử dụng hàm tính toán tập trung để lấy lực chiến mới nhất
        int calculatedPower = CalculateTotalPower(currentDeck);
        PlayerPower = calculatedPower;

        // Đảm bảo lấy tên chuẩn nhất (ArenaDisplayName > PlayerName)
        string finalName = PlayerInfoUI.GetSafeName(!string.IsNullOrEmpty(ArenaDisplayName) ? ArenaDisplayName : PlayerName);

        PublicProfileSaveData profile = new PublicProfileSaveData
        {
            displayName = finalName,
            level = PlayerLevel,
            exp = PlayerExp,
            avatarId = "default_avatar", // Có thể thay thế bằng biến thực tế nếu có
            frameId = "default_frame",   // Có thể thay thế bằng biến thực tế nếu có
            totalPower = PlayerPower,
            arenaRank = PlayerRank,      // Cập nhật hạng để người khác thấy
            deck = currentDeck
        };

        string jsonProfile = JsonUtility.ToJson(profile);
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { "PublicProfile", jsonProfile } },
            Permission = UserDataPermission.Public
        };

        PlayFabClientAPI.UpdateUserData(request, 
            res => {
                Debug.Log("<color=cyan>[PlayFab] Đã cập nhật Public Profile (Hộ chiếu) thành công!</color>");
                // Cập nhật lực chiến lên Statistics để leo bảng xếp hạng
                UpdatePlayerStatistics(PlayFabConstants.STAT_PLAYER_POWER, PlayerPower);
            },
            err => Debug.LogWarning("[PlayFab] Lỗi cập nhật Profile: " + err.GenerateErrorReport())
        );
    }

    /// <summary>
    /// Hàm tính toán lực chiến duy nhất của game. 
    /// Có thể tính từ DeckSaveData (Snapshot) hoặc từ DeckManager (Thẻ thực tế).
    /// </summary>
    public int CalculateTotalPower(DeckSaveData snapshotDeck = null)
    {
        int total = 0;

        // Ưu tiên tính từ Snapshot (dùng cho Public Profile)
        if (snapshotDeck != null && snapshotDeck.snapshots != null)
        {
            foreach (var snap in snapshotDeck.snapshots)
            {
                if (snap != null && snap.id != -1)
                    total += snap.top + snap.right + snap.bottom + snap.left;
            }
            return total;
        }

        // Nếu không có snapshot, tính trực tiếp từ DeckManager (dùng cho UI/Local)
        if (DeckManager.Instance != null && DeckManager.Instance.currentDeck != null)
        {
            foreach (var card in DeckManager.Instance.currentDeck)
            {
                if (card != null && card.data != null)
                    total += card.GetTotalTop() + card.GetTotalRight() + card.GetTotalBottom() + card.GetTotalLeft();
            }
            PlayerPower = total; // Lưu lại giá trị mới nhất
        }
        
        return total;
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
                
                // Nạp luôn dữ liệu Arena từ SaveData (phòng trường hợp fetch stats chưa xong)
                this.PlayerRank = _cachedSaveData.rank;
                this.Elo = _cachedSaveData.elo;
                this.Wins = _cachedSaveData.wins;
                this.Losses = _cachedSaveData.losses;
                this.TotalGames = _cachedSaveData.totalGames;
                this.ArenaDisplayName = _cachedSaveData.displayName;
            }

            // Kéo luôn Statistics về cho chuẩn
            FetchPlayerStatistics();
            FetchMyRank(); // Tự động dò hạng ngay khi vào game
            FetchAccountInfo(); // Tự động lấy tên chuẩn ngay khi vào game

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
                DeckSaveData loadedDeck = JsonUtility.FromJson<DeckSaveData>(deckJson);
                RestoreDeck(loadedDeck);
                UpdatePublicProfile(loadedDeck);
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
        if (loadedDeck == null) 
        {
            Debug.LogWarning("[PlayFab] Dữ liệu Deck rỗng hoặc không hợp lệ.");
            return;
        }

        var khoBaiCuaSep = CardListManager.Instance.GetOwnedCards();

        for (int i = 0; i < PlayFabConstants.MAX_DECK_SIZE; i++)
        {
            // Kiểm tra kỹ lưỡng: snapshots không null VÀ phần tử thứ i không null
            int idCanTim = -1;
            if (loadedDeck.snapshots != null && i < loadedDeck.snapshots.Length && loadedDeck.snapshots[i] != null)
            {
                idCanTim = loadedDeck.snapshots[i].id;
            }

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
                PlayerName = ""; 
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