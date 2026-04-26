using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab.ClientModels;

public class UI_ArenaItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI txtRank;
    [SerializeField] private Image imgFrame;
    [SerializeField] private Image imgAvatar;
    [SerializeField] private TextMeshProUGUI txtTitle;
    [SerializeField] private TextMeshProUGUI txtName;
    [SerializeField] private TextMeshProUGUI txtElo;
    [SerializeField] private Button btnChallenge;

    public void Setup(PlayerLeaderboardEntry entry)
    {
        if (txtRank) txtRank.text = (entry.Position + 1).ToString();
        if (txtName) txtName.text = string.IsNullOrEmpty(entry.DisplayName) ? "Sếp Yami" : entry.DisplayName;
        if (txtElo) txtElo.text = entry.StatValue.ToString();
        
        // Title giả lập (Sau này sếp có thể lấy từ UserData của họ)
        if (txtTitle) txtTitle.text = "Tân Thủ"; 

        if (btnChallenge)
        {
            btnChallenge.onClick.RemoveAllListeners();
            btnChallenge.onClick.AddListener(() => OnChallengeClicked(entry));
        }
    }

    private void OnChallengeClicked(PlayerLeaderboardEntry target)
    {
        Debug.Log($"<color=orange>[Arena]</color> Đang khiêu chiến: {target.DisplayName} (Top {target.Position + 1})");
        // Logic khiêu chiến Arena ở đây sếp nhé
    }
}
