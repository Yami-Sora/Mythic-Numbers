using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform contentParent;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text rowPrefab;

    [Header("Settings")]
    [SerializeField] private Color eloColor = new Color(0.2f, 0.8f, 0.2f);

    private const int TopCount = 10;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true); // Bật luôn nếu đang inactive (kéo từ reference vẫn gọi được)
        if (panelRoot != null) panelRoot.SetActive(true);
        if (errorText != null) errorText.gameObject.SetActive(false);
        if (loadingText != null) { loadingText.gameObject.SetActive(true); loadingText.text = "Đang tải..."; }
        ClearContent();
        LoadLeaderboard();
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void ClearContent()
    {
        if (contentParent == null) return;
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);
    }

    private void LoadLeaderboard()
    {
        if (GameServices.Instance?.Leaderboard == null) { ShowError("Chưa kết nối. Kiểm tra PlayFab."); return; }
        GameServices.Instance.Leaderboard.GetTopPlayersAsync(TopCount, OnLoaded, ShowError);
    }

    private void OnLoaded(List<LeaderboardEntry> entries)
    {
        if (loadingText != null) loadingText.gameObject.SetActive(false);
        if (contentParent == null || rowPrefab == null)
        {
            if (loadingText != null) loadingText.text = entries.Count > 0 ? $"Top {entries.Count} đã tải." : "Chưa có dữ liệu.";
            return;
        }
        
        if (contentParent.GetComponent<Image>() != null)
            contentParent.GetComponent<Image>().enabled = false;
        
        ClearContent();
        foreach (var e in entries)
        {
            var row = Instantiate(rowPrefab, contentParent);
            row.text = $"#{e.Position}  {e.DisplayName}  <color=#2ECC71>ELO {e.Elo}</color>";
            row.gameObject.SetActive(true);
        }
    }

    private void ShowError(string msg)
    {
        if (loadingText != null) loadingText.gameObject.SetActive(false);
        if (errorText != null) { errorText.text = msg; errorText.gameObject.SetActive(true); }
    }
}
