using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab.ClientModels;

public class UI_ArenaItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI txtRank;
    [SerializeField] private TextMeshProUGUI txtName;
    [SerializeField] private TextMeshProUGUI txtElo;
    [SerializeField] private TextMeshProUGUI txtLevel;
    [SerializeField] private TextMeshProUGUI txtTitle;
    [SerializeField] private Image imgAvatar;
    [SerializeField] private Button btnChallenge;

    /// <summary>
    /// Setup hiển thị thông tin người chơi trong Arena.
    /// playerTitle: lấy từ PlayFabDataManager.Instance.PlayerTitle của người chơi hiện tại
    /// </summary>
    public void Setup(PlayerLeaderboardEntry entry, string playerTitle = "")
    {
        if (txtRank) txtRank.text = "Hạng " + (entry.Position + 1);
        if (txtName) txtName.text = string.IsNullOrEmpty(entry.DisplayName) ? "Ẩn Danh" : entry.DisplayName;
        if (txtElo) txtElo.text = entry.StatValue.ToString() + " điểm";
        if (txtTitle) txtTitle.text = string.IsNullOrEmpty(playerTitle) ? "" : playerTitle;

        // Level sẽ được bổ sung khi có UserData của các người chơi khác
        if (txtLevel) txtLevel.text = "";

        if (btnChallenge)
        {
            btnChallenge.onClick.RemoveAllListeners();
            btnChallenge.onClick.AddListener(() => OnChallengeClicked(entry));
        }
    }

    /// <summary>
    /// Overload để hiển thị từ dữ liệu cache cục bộ.
    /// </summary>
    public void Setup(LeaderboardEntry entry, string playerTitle = "")
    {
        if (txtRank) txtRank.text = "Hạng " + (entry.Position + 1);
        if (txtName) txtName.text = entry.DisplayName;
        if (txtElo) txtElo.text = entry.Elo.ToString() + " điểm";
        if (txtTitle) txtTitle.text = string.IsNullOrEmpty(playerTitle) ? "" : playerTitle;
        if (txtLevel) txtLevel.text = "";
        
        if (btnChallenge) btnChallenge.gameObject.SetActive(false); // BXH chung không cho thách đấu trực tiếp
    }

    private void OnChallengeClicked(PlayerLeaderboardEntry target)
    {
        Debug.Log($"<color=orange>[Arena]</color> Khiêu chiến: {target.DisplayName} (Top {target.Position + 1})");
        // TODO: Mở màn hình xác nhận khiêu chiến
    }
}
