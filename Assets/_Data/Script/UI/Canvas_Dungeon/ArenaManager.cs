using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// Quản lý toàn bộ logic của Arena Dungeon:
/// fetch leaderboard, spawn prefab, giao việc setup cho UI_ArenaItem.
/// Gắn vào GameObject Arena_Dungeon.
/// </summary>
public class ArenaManager : YamiMonoBehaviour
{
    public static ArenaManager Instance { get; private set; }

    [Header("Arena Setup")]
    [SerializeField] private Transform arenaItemContainer;
    [SerializeField] private GameObject arenaItemPrefab;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    /// <summary>Gọi từ Canvas_DungeonManager khi mở tab Arena</summary>
    public void OpenArena()
    {
        FetchArenaLeaderboard();
    }

    public void ClosePanel()
    {
        if (Canvas_DungeonManager.Instance != null)
            Canvas_DungeonManager.Instance.Close_All_Panels();
    }

    private void FetchArenaLeaderboard()
    {
        if (arenaItemContainer == null || arenaItemPrefab == null)
        {
            Debug.LogError("[Arena] Chưa gán arenaItemContainer hoặc arenaItemPrefab!");
            return;
        }

        // Xóa items cũ
        foreach (Transform child in arenaItemContainer)
            Destroy(child.gameObject);

        var request = new GetLeaderboardRequest
        {
            StatisticName = "elo",
            StartPosition = 0,
            MaxResultsCount = 20
        };

        PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardSuccess,
            error => Debug.LogError("[Arena] Lỗi lấy Leaderboard: " + error.GenerateErrorReport()));
    }

    private void OnLeaderboardSuccess(GetLeaderboardResult result)
    {
        // Lấy danh hiệu của người chơi hiện tại từ PlayerData
        string myTitle = PlayFabDataManager.Instance != null ? PlayFabDataManager.Instance.PlayerTitle : "";

        foreach (var entry in result.Leaderboard)
        {
            GameObject go = Instantiate(arenaItemPrefab, arenaItemContainer);
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.localScale = Vector3.one;

            UI_ArenaItem uiItem = go.GetComponent<UI_ArenaItem>();
            if (uiItem != null)
            {
                // Chỉ hiện danh hiệu của bản thân (chưa thể lấy danh hiệu người khác từ Leaderboard)
                string title = entry.PlayFabId == PlayFabSettings.staticPlayer.PlayFabId ? myTitle : "";
                uiItem.Setup(entry, title);
            }
        }
    }
}
