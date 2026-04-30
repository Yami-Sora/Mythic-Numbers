using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;

/// <summary>
/// Quản lý bảng xếp hạng (Leaderboard) trong Popup.
/// Sử dụng dữ liệu cache được cập nhật tự động mỗi 5p từ GameServices.
/// </summary>
public class LeaderboardPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private Button closeButton;

    [Header("Arena Setup")]
    [SerializeField] private GameObject arenaItemPrefab;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    private void Start()
    {
        // Đảm bảo ban đầu ẩn
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        ClearContent();
        LoadLeaderboard();

        // Ép lấy dữ liệu mới nhất từ server mỗi khi mở bảng xếp hạng
        if (GameServices.Instance != null && GameServices.Instance.Leaderboard != null)
        {
            GameServices.Instance.Leaderboard.ForceUpdateLeaderboard();
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (GameServices.Instance != null && GameServices.Instance.Leaderboard != null)
        {
            GameServices.Instance.Leaderboard.OnLeaderboardUpdated += RefreshLeaderboard;
        }
    }

    private void OnDisable()
    {
        if (GameServices.Instance != null && GameServices.Instance.Leaderboard != null)
        {
            GameServices.Instance.Leaderboard.OnLeaderboardUpdated -= RefreshLeaderboard;
        }
    }

    private void RefreshLeaderboard()
    {
        if (gameObject.activeInHierarchy)
        {
            ClearContent();
            LoadLeaderboard();
        }
    }

    private void ClearContent()
    {
        if (contentParent == null) return;
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);
    }

    private void LoadLeaderboard()
    {
        if (GameServices.Instance?.Leaderboard == null) return;

        // Lấy dữ liệu đã được cache sẵn trong LeaderboardService (cập nhật mỗi 5p)
        List<LeaderboardEntry> entries = GameServices.Instance.Leaderboard.GetCachedLeaderboard();
        
        if (entries == null || entries.Count == 0)
        {
            Debug.Log("[Leaderboard] Chưa có dữ liệu cache, đang chờ AutoFetch...");
            return;
        }

        // Lấy danh hiệu của người chơi hiện tại
        string myTitle = PlayFabDataManager.Instance != null ? PlayFabDataManager.Instance.PlayerTitle : "";

        foreach (var entry in entries)
        {
            if (arenaItemPrefab == null) break;

            GameObject go = Instantiate(arenaItemPrefab, contentParent);
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.localScale = Vector3.one;

            UI_ArenaItem uiItem = go.GetComponent<UI_ArenaItem>();
            if (uiItem != null)
            {
                // Sử dụng overload Setup cho LeaderboardEntry
                string title = (PlayFabSettings.staticPlayer != null && entry.PlayFabId == PlayFabSettings.staticPlayer.PlayFabId) ? myTitle : "";
                uiItem.Setup(entry, title);
            }
        }
    }
}
