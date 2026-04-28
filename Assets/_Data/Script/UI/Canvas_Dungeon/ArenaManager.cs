using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Quản lý toàn bộ logic của Arena Dungeon:
/// Chọn ngẫu nhiên 4 đối thủ xếp trên người chơi từ top 20 phía trước.
/// Giao diện sử dụng 4 slot tĩnh thay vì ScrollView.
/// </summary>
public class ArenaManager : YamiMonoBehaviour
{
    public static ArenaManager Instance { get; private set; }

    [Header("Arena Setup (Static Slots)")]
    [SerializeField] private UI_ArenaItem[] arenaSlots;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    /// <summary>Gọi từ Canvas_DungeonManager khi mở tab Arena</summary>
    public void OpenArena()
    {
        FetchArenaOpponents();
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
            MaxResultsCount = 40 // Lấy rộng một chút để có nhiều lựa chọn (20 trước, 20 sau)
        };

        PlayFabClientAPI.GetLeaderboardAroundPlayer(request, OnGetOpponentsSuccess, 
            error => Debug.LogError("[Arena] Lỗi lấy đối thủ: " + error.GenerateErrorReport()));
    }

    private void OnGetOpponentsSuccess(GetLeaderboardAroundPlayerResult result)
    {
        string myId = PlayFabSettings.staticPlayer.PlayFabId;
        List<PlayerLeaderboardEntry> others = new List<PlayerLeaderboardEntry>();

        if (result.Leaderboard != null)
        {
            others = result.Leaderboard.Where(e => e.PlayFabId != myId).ToList();
        }

        List<PlayerLeaderboardEntry> potentialOpponents = new List<PlayerLeaderboardEntry>();
        
        // Lấy hạng của mình. Nếu chưa có hạng (mới chơi), giả định là hạng 100
        int myRank = 100;
        var me = result.Leaderboard?.FirstOrDefault(e => e.PlayFabId == myId);
        if (me != null) myRank = me.Position + 1;

        // Lấy tối đa 4 người xếp trên mình
        var thoseAhead = others.Where(e => e.Position < (myRank - 1))
            .OrderByDescending(e => e.Position)
            .Take(10) // Lấy 10 người gần mình nhất
            .OrderBy(x => Random.value)
            .Take(4)
            .ToList();

        potentialOpponents.AddRange(thoseAhead);

        // Nếu vẫn thiếu người (ví dụ mình đang ở top đầu), lấy thêm những người xếp sau
        if (potentialOpponents.Count < 4)
        {
            var thoseBehind = others.Where(e => !potentialOpponents.Contains(e))
                .OrderBy(e => e.Position)
                .Take(4 - potentialOpponents.Count)
                .ToList();
            potentialOpponents.AddRange(thoseBehind);
        }

        // Sắp xếp lại danh sách cuối cùng theo hạng
        var finalOpponents = potentialOpponents.OrderBy(e => e.Position).ToList();

        // Hiển thị lên 4 slot
        for (int i = 0; i < arenaSlots.Length; i++)
        {
            if (arenaSlots[i] == null) continue;

            arenaSlots[i].gameObject.SetActive(true);

            if (i < finalOpponents.Count)
            {
                // Gán người thật
                arenaSlots[i].Setup(finalOpponents[i], "Thiếu Chủ");
            }
            else
            {
                // Nếu vẫn thiếu (leaderboard quá ít người), tạo Bot
                CreateBot(arenaSlots[i], i, myRank);
            }
        }
    }

    private void CreateBot(UI_ArenaItem slot, int index, int myRank)
    {
        int botRank = Mathf.Max(1, myRank - (4 - index));
        string[] botNames = { "Vô Danh", "Kiếm Khách", "Ẩn Sĩ", "Cuồng Phong", "Bá Chủ" };
        string botName = botNames[Random.Range(0, botNames.Length)] + " (Bot)";
        
        PlayerLeaderboardEntry botEntry = new PlayerLeaderboardEntry
        {
            DisplayName = botName,
            Position = botRank - 1,
            StatValue = Mathf.Max(0, (100 - botRank) * 10),
        };

        slot.Setup(botEntry, "Ản Sĩ");
    }
}
