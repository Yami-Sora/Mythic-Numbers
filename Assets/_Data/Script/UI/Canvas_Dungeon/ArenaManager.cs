using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Quản lý toàn bộ logic của Arena Dungeon:
/// Chọn ngẫu nhiên 4 đối thủ xếp trên người chơi từ top 20 phía trước.
/// Giao diện sử dụng 4 slot tĩnh thay vì ScrollView.
/// </summary>
public class ArenaManager : YamiMonoBehaviour
{
    public static ArenaManager Instance { get; private set; }

    [System.Serializable]
    public class ArenaSaveData
    {
        public int challenges;
        public int refreshes;
        public string lastDate;
    }

    private const string ARENA_STATS_KEY = "ArenaStats";

    [Header("Arena Setup (Static Slots)")]
    [SerializeField] private UI_ArenaItem[] arenaSlots;

    [Header("Counters UI")]
    [SerializeField] private TextMeshProUGUI txtChallengeCount;
    [SerializeField] private TextMeshProUGUI txtRefreshCount;
    [SerializeField] private Button btnRefresh;
    [SerializeField] private Button btnAddChallenges;

    private int currentChallenges = 10;
    private int maxChallenges = 10;
    private int currentRefreshes = 5;
    private int maxRefreshes = 5;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    protected override void Start()
    {
        base.Start();
        LoadArenaData();

        if (btnRefresh != null)
        {
            btnRefresh.onClick.RemoveAllListeners();
            btnRefresh.onClick.AddListener(RefreshArena);
        }

        if (btnAddChallenges != null)
        {
            btnAddChallenges.onClick.RemoveAllListeners();
            btnAddChallenges.onClick.AddListener(() => {
                if (ArenaChallengePopup.Instance != null) ArenaChallengePopup.Instance.Open();
            });
        }
    }

    /// <summary>Gọi từ Canvas_DungeonManager khi mở tab Arena</summary>
    public void OpenArena()
    {
        FetchArenaOpponents();
    }

    public void RefreshArena()
    {
        if (currentRefreshes > 0)
        {
            currentRefreshes--;
            SaveArenaData();
            UpdateCounterUI();
            FetchArenaOpponents();
            Debug.Log("<color=cyan>[Arena]</color> Đã làm mới danh sách đối thủ.");
        }
        else
        {
            Debug.LogWarning("[Arena] Hết lượt làm mới!");
        }
    }

    public bool UseChallenge()
    {
        if (currentChallenges > 0)
        {
            currentChallenges--;
            SaveArenaData();
            UpdateCounterUI();
            return true;
        }
        return false;
    }

    public void AddChallenges(int amount)
    {
        currentChallenges += amount;
        SaveArenaData();
        UpdateCounterUI();
    }

    private void LoadArenaData()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            UpdateCounterUI();
            return;
        }



        // Lấy thời gian chuẩn từ Server để chống hack chỉnh ngày điện thoại
        PlayFabClientAPI.GetTime(new GetTimeRequest(), timeResult =>
        {
            DateTime serverTime = timeResult.Time;
            string today = serverTime.ToString("yyyy-MM-dd");

            PlayFabClientAPI.GetUserData(new GetUserDataRequest { Keys = new List<string> { ARENA_STATS_KEY } }, result =>
            {
                if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(false);

                if (result.Data != null && result.Data.ContainsKey(ARENA_STATS_KEY))
                {
                    string json = result.Data[ARENA_STATS_KEY].Value;
                    var data = JsonUtility.FromJson<ArenaSaveData>(json);
                    
                    if (data.lastDate == today)
                    {
                        currentChallenges = data.challenges;
                        currentRefreshes = data.refreshes;
                    }
                    else
                    {
                        currentChallenges = maxChallenges;
                        currentRefreshes = maxRefreshes;
                    }
                }
                else
                {
                    currentChallenges = maxChallenges;
                    currentRefreshes = maxRefreshes;
                }
                UpdateCounterUI();
            }, error => {
                Debug.LogWarning("[Arena] Lỗi tải dữ liệu, đang thử lại sau 3s...");
                StartCoroutine(DelayRetry(LoadArenaData));
            });
        }, error => {
            Debug.LogWarning("[Arena] Lỗi lấy thời gian server, đang thử lại sau 3s...");
            StartCoroutine(DelayRetry(LoadArenaData));
        });
    }

    private System.Collections.IEnumerator DelayRetry(Action action)
    {
        if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(true);
        yield return new WaitForSeconds(3f);
        action?.Invoke();
    }

    private void SaveArenaData()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;

        var data = new ArenaSaveData
        {
            challenges = currentChallenges,
            refreshes = currentRefreshes,
            lastDate = System.DateTime.UtcNow.ToString("yyyy-MM-dd")
        };

        string json = JsonUtility.ToJson(data);
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { ARENA_STATS_KEY, json } }
        };

        PlayFabClientAPI.UpdateUserData(request, 
            res => Debug.Log("<color=green>[Arena]</color> Đã lưu lượt Arena lên mây."),
            err => Debug.LogError("[Arena] Lỗi lưu lượt: " + err.GenerateErrorReport()));
    }

    private void UpdateCounterUI()
    {
        if (txtChallengeCount != null) txtChallengeCount.text = $"Lượt khiêu chiến: {currentChallenges}/{maxChallenges}";
        if (txtRefreshCount != null) txtRefreshCount.text = $"Lượt làm mới {currentRefreshes}/{maxRefreshes}";
    }

    public void ClosePanel()
    {
        if (Canvas_DungeonManager.Instance != null)
            Canvas_DungeonManager.Instance.Close_All_Panels();
    }

    private void FetchArenaOpponents()
    {
        if (arenaSlots == null || arenaSlots.Length == 0)
        {
            Debug.LogError("[Arena] Chưa gán arenaSlots tĩnh!");
            return;
        }



        // Ban đầu ẩn hết các slot để chờ load
        foreach (var slot in arenaSlots) if(slot != null) slot.gameObject.SetActive(false);

        // Lấy danh sách những người xung quanh player
        var request = new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = "elo",
            MaxResultsCount = 21 // Lấy 10 người trước, 10 người sau (tổng cộng 21 bao gồm cả mình)
        };

        PlayFabClientAPI.GetLeaderboardAroundPlayer(request, OnGetOpponentsSuccess, 
            error => {
                Debug.LogWarning("[Arena] Lỗi lấy đối thủ, đang thử lại sau 3s...");
                StartCoroutine(DelayRetry(FetchArenaOpponents));
            });
    }

    private void OnGetOpponentsSuccess(GetLeaderboardAroundPlayerResult result)
    {
        if (LoadingManager.Instance != null) LoadingManager.Instance.ShowLoading(false);

        // Lấy ID chính xác của mình
        string myId = PlayFabSettings.staticPlayer.PlayFabId;
        List<PlayerLeaderboardEntry> others = result.Leaderboard?.Where(e => e.PlayFabId != myId).ToList() ?? new List<PlayerLeaderboardEntry>();
        
        // Hạng của mình
        int myRank = 0;
        int myElo = PlayFabDataManager.Instance.Elo;
        var me = result.Leaderboard?.FirstOrDefault(e => e.PlayFabId == myId);
        
        if (me != null) 
        {
            myRank = me.Position + 1;
            myElo = me.StatValue;
        }

        // Luôn cập nhật UI, nếu không tìm thấy hạng thì hiện "Chưa xếp hạng" nhưng Tên và Lực chiến vẫn phải đúng
        int myPower = PlayFabDataManager.Instance.CalculateTotalPower();
        string myTitle = PlayFabDataManager.Instance.PlayerTitle;
        string myDisplayName = (me != null && !string.IsNullOrEmpty(me.DisplayName)) ? me.DisplayName : PlayFabDataManager.Instance.PlayerName;
        if (string.IsNullOrEmpty(myDisplayName)) myDisplayName = "Thiếu Chủ"; // Fallback cuối cùng
        
        // Ghi sổ vào DataManager để các UI khác dùng lại
        PlayFabDataManager.Instance.PlayerRank = myRank;
        PlayFabDataManager.Instance.ArenaDisplayName = myDisplayName;
        PlayFabDataManager.Instance.Elo = myElo;

        PlayerInfoUI.UpdateAllArenaInfo(myRank, myDisplayName, myElo, myTitle, myPower);

        // --- LOGIC CHỌN ĐỐI THỦ THÔNG MINH HƠN ---
        List<PlayerLeaderboardEntry> selectedOpponents = new List<PlayerLeaderboardEntry>();
        
        // 1. Lấy tất cả những người xếp TRÊN mình
        var ahead = others.Where(e => e.Position < (myRank - 1)).OrderByDescending(e => e.Position).ToList();
        selectedOpponents.AddRange(ahead.Take(4));

        // 2. Nếu vẫn thiếu (do mình đang ở top đầu), lấy thêm những người xếp DƯỚI mình
        if (selectedOpponents.Count < 4)
        {
            var behind = others.Where(e => e.Position > (myRank - 1)).OrderBy(e => e.Position).ToList();
            var needed = 4 - selectedOpponents.Count;
            selectedOpponents.AddRange(behind.Take(needed));
        }

        // Sắp xếp lại danh sách cuối cùng theo hạng từ cao xuống thấp
        var finalOpponents = selectedOpponents.OrderBy(e => e.Position).ToList();

        // Hiển thị lên 4 slot
        for (int i = 0; i < arenaSlots.Length; i++)
        {
            if (arenaSlots[i] == null) continue;
            arenaSlots[i].gameObject.SetActive(true);

            if (i < finalOpponents.Count)
            {
                arenaSlots[i].Setup(finalOpponents[i], "Thiếu Chủ");
            }
            else
            {
                // Nếu vẫn thiếu người thật, đẻ Bot ở các hạng tiếp theo (myRank + i + 1)
                CreateBot(arenaSlots[i], i, myRank);
            }
        }
    }

    private void CreateBot(UI_ArenaItem slot, int index, int myRank)
    {
        // Bot sẽ có hạng thấp hơn mình để lấp đầy danh sách (ví dụ mình hạng 2 thì bot hạng 3, 4, 5...)
        int botRank = (myRank > 0) ? myRank + index + 1 : index + 1;
        
        string[] botNames = { "Vô Danh", "Kiếm Khách", "Ẩn Sĩ", "Cuồng Phong", "Bá Chủ" };
        string botName = botNames[UnityEngine.Random.Range(0, botNames.Length)] + " (Bot)";
        
        PlayerLeaderboardEntry botEntry = new PlayerLeaderboardEntry
        {
            DisplayName = botName,
            Position = botRank - 1,
            StatValue = Mathf.Max(0, 100 - (botRank * 5)), // Điểm giảm dần theo hạng
        };

        slot.Setup(botEntry, "Ẩn Sĩ");
    }
}
