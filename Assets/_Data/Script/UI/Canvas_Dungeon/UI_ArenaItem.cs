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
        
        if (txtRank) txtRank.text = "Hạng " + (entry.Position + 1);
        if (txtName) txtName.text = string.IsNullOrEmpty(entry.DisplayName) ? "Ẩn Danh" : entry.DisplayName;
        if (txtElo) txtElo.text = entry.StatValue.ToString() + " điểm";
        
        // Mặc định cho Bot hoặc người chơi chưa có Profile (Sửa lại mặc định là Level 1)
        if (txtLevel) txtLevel.text = "1";
        if (txtTitle) txtTitle.text = string.IsNullOrEmpty(playerTitle) ? "Tân Thủ" : playerTitle;

        cachedProfile = null;

        // TỰ ĐI LẤY HỘ CHIẾU CỦA ĐỐI THỦ (VÌ LEADERBOARD KHÔNG KÈM THEO)
        PlayFabClientAPI.GetUserData(new GetUserDataRequest {
            PlayFabId = entry.PlayFabId,
            Keys = new System.Collections.Generic.List<string> { "PublicProfile" }
        }, result => {
            if (this == null) return; // Tránh lỗi nếu UI bị tắt trước khi data về
            
            if (result.Data != null && result.Data.ContainsKey("PublicProfile"))
            {
                try 
                {
                    string json = result.Data["PublicProfile"].Value;
                    cachedProfile = JsonUtility.FromJson<PublicProfileSaveData>(json);

                    if (cachedProfile != null)
                    {
                        if (txtLevel) txtLevel.text = cachedProfile.level.ToString();
                        if (txtName) txtName.text = cachedProfile.displayName;
                        // Cập nhật lại Listener với profile mới nhất
                        if (btnChallenge)
                        {
                            btnChallenge.onClick.RemoveAllListeners();
                            btnChallenge.onClick.AddListener(() => OnChallengeClicked(entry, cachedProfile));
                        }
                    }
                }
                catch { }
            }
        }, null);

        if (btnChallenge)
        {
            btnChallenge.onClick.RemoveAllListeners();
            btnChallenge.onClick.AddListener(() => OnChallengeClicked(entry, cachedProfile));
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
            var launcher = FindFirstObjectByType<NetworkLauncher>();
            if (launcher != null)
            {
                launcher.OnPlayOfflineClicked();
            }
            else
            {
                Debug.LogError("[Arena] Không tìm thấy NetworkLauncher trong Scene!");
            }
        }
        else
        {
            Debug.LogWarning("[Arena] Hết lượt khiêu chiến rồi sếp ơi!");
        }
    }
}
