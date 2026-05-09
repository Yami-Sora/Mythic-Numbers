using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;

public class UI_ArenaItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI txtRank;
    [SerializeField] private TextMeshProUGUI txtName;
    [SerializeField] private TextMeshProUGUI txtElo;
    [SerializeField] private TextMeshProUGUI txtLevel;
    [SerializeField] private TextMeshProUGUI txtTitle;
    [SerializeField] private TextMeshProUGUI txtPower;
    [SerializeField] private Image imgAvatar;
    [SerializeField] private Button btnChallenge;

    /// <summary>
    /// Setup hiển thị thông tin người chơi trong Arena.
    /// playerTitle: lấy từ PlayFabDataManager.Instance.PlayerTitle của người chơi hiện tại
    /// </summary>
    private PublicProfileSaveData cachedProfile = null;

    public void Setup(PlayerLeaderboardEntry entry, string playerTitle = "")
    {
        if (entry == null) return;
        
        string fallbackName = string.IsNullOrEmpty(entry.PlayFabId) ? "Vô Danh" : (entry.PlayFabId.Length > 8 ? entry.PlayFabId.Substring(0, 8) + "..." : entry.PlayFabId);
        
        string dName = string.IsNullOrEmpty(entry.DisplayName) || entry.DisplayName == "Anonymous" ? fallbackName : entry.DisplayName;

        if (txtRank) txtRank.text = "Hạng " + (entry.Position + 1);
        if (txtName) txtName.text = dName;
        if (txtElo) txtElo.text = entry.StatValue.ToString() + " điểm";
        
        // Mặc định cho Bot hoặc người chơi chưa có Profile (Sửa lại mặc định là Level 1)
        if (txtLevel) txtLevel.text = "1";
        if (txtTitle) txtTitle.text = string.IsNullOrEmpty(playerTitle) ? "Tân Thủ" : playerTitle;
        if (txtPower) txtPower.text = "0";

        cachedProfile = null;

        if (btnChallenge)
        {
            btnChallenge.onClick.RemoveAllListeners();
            btnChallenge.onClick.AddListener(() => OnChallengeClicked(entry, cachedProfile));
        }

        FetchProfileAndUpdateUI(entry.PlayFabId, profile => {
            if (btnChallenge)
            {
                btnChallenge.onClick.RemoveAllListeners();
                btnChallenge.onClick.AddListener(() => OnChallengeClicked(entry, profile));
            }
        });
    }

    /// <summary>
    /// Overload để hiển thị từ dữ liệu cache cục bộ.
    /// </summary>
    public void Setup(LeaderboardEntry entry, string playerTitle = "")
    {
        string fallbackName = string.IsNullOrEmpty(entry.PlayFabId) ? "Vô Danh" : (entry.PlayFabId.Length > 8 ? entry.PlayFabId.Substring(0, 8) + "..." : entry.PlayFabId);

        string dName = string.IsNullOrEmpty(entry.DisplayName) || entry.DisplayName == "Anonymous" ? fallbackName : entry.DisplayName;

        if (txtRank) txtRank.text = "Hạng " + (entry.Position + 1);
        if (txtName) txtName.text = dName;
        if (txtElo) txtElo.text = entry.Elo.ToString() + " điểm";
        if (txtTitle) txtTitle.text = string.IsNullOrEmpty(playerTitle) ? "" : playerTitle;
        
        // Mặc định
        if (txtLevel) txtLevel.text = "1";
        if (txtPower) txtPower.text = "0";
        
        if (btnChallenge) btnChallenge.gameObject.SetActive(false); // BXH chung không cho thách đấu trực tiếp

        cachedProfile = null;
        
        FetchProfileAndUpdateUI(entry.PlayFabId, null);
    }

    private void FetchProfileAndUpdateUI(string playFabId, System.Action<PublicProfileSaveData> onProfileLoaded)
    {
        if (string.IsNullOrEmpty(playFabId)) return;

        PlayFabDataManager.Instance.GetUserData(playFabId, (profile) =>
        {
            if (this == null) return;
            cachedProfile = profile;

            if (cachedProfile != null)
            {
                string fallbackName = PlayerInfoUI.GetSafeName(playFabId);
                string dName = string.IsNullOrEmpty(cachedProfile.displayName) || cachedProfile.displayName == "Anonymous" ? fallbackName : cachedProfile.displayName;

                if (txtLevel) txtLevel.text = cachedProfile.level.ToString();
                if (txtName) txtName.text = dName;
                if (txtPower) txtPower.text = cachedProfile.totalPower.ToString("N0");

                onProfileLoaded?.Invoke(cachedProfile);
            }
        });
    }

    private void OnChallengeClicked(PlayerLeaderboardEntry target, PublicProfileSaveData profile)
    {
        // [BLOCKER] Kiểm tra đội hình đủ 5 lá chưa
        if (DeckManager.Instance != null && !DeckManager.Instance.IsDeckFull())
        {
            if (VFXManager.Instance != null && btnChallenge != null)
            {
                VFXManager.Instance.SpawnFloatingText("Cần đủ 5 lá bài sếp ơi!", btnChallenge.transform, Color.red);
            }
            Debug.LogWarning("[Arena] Đội hình chưa đủ 5 lá bài, không thể khiêu chiến!");
            return;
        }

        if (ArenaManager.Instance != null && ArenaManager.Instance.UseChallenge())
        {
            Debug.Log($"<color=orange>[Arena]</color> Bắt đầu khiêu chiến: {target.DisplayName}");

            // Ghi nhớ ID đối thủ để "tính sổ" sau trận đấu
            LocalDeckContext.OpponentPlayFabId = target.PlayFabId;

            // NẠP BỘ BÀI ĐỐI THỦ (Rất quan trọng!)
            if (profile != null && profile.deck != null && profile.deck.snapshots != null)
            {
                Debug.Log("<color=green>[Arena] Đã lấy được Hộ chiếu đối thủ. Sử dụng bộ bài Snapshot!</color>");
                LocalDeckContext.SetOpponentDeckFromSnapshots(profile.deck.snapshots);
            }
            else
            {
                Debug.Log("<color=yellow>[Arena] Đối thủ không có Hộ chiếu (Bot), sử dụng bộ bài ngẫu nhiên.</color>");
                LocalDeckContext.SetRandomOpponentDeck();
            }

            // Thiết lập chế độ Arena để tránh nhầm với Story/Dungeon
            if (PlayFabDataManager.Instance != null)
            {
                PlayFabDataManager.Instance.CurrentMode = PlayFabDataManager.GameMode.Arena;
            }

            // Chuyển cảnh vào trận đấu (Chế độ Single Player vì đối thủ là Bot/Snapshot)
            var launcher = FindFirstObjectByType<BattleLauncher>();
            if (launcher != null)
            {
                launcher.OnPlayOfflineClicked();
            }
            else
            {
                Debug.LogError("[Arena] Không tìm thấy BattleLauncher trong Scene!");
            }
        }
        else
        {
            Debug.LogWarning("[Arena] Hết lượt khiêu chiến rồi sếp ơi!");
        }
    }
}
