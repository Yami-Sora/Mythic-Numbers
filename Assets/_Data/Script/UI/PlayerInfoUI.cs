using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerInfoUI : MonoBehaviour
{
    [Header("Avatar & Title")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private GameObject titleContainer;
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Player Info")]
    [SerializeField] private TextMeshProUGUI rankText; // Thêm ô hiện Hạng
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private TextMeshProUGUI eloText; // Thêm ô hiện điểm Elo

    public static string GetSafeName(string name)
    {
        if (string.IsNullOrEmpty(name) || name == "Anonymous")
        {
            if (PlayFab.PlayFabSettings.staticPlayer != null && !string.IsNullOrEmpty(PlayFab.PlayFabSettings.staticPlayer.PlayFabId))
            {
                string id = PlayFab.PlayFabSettings.staticPlayer.PlayFabId;
                return id.Length > 8 ? id.Substring(0, 8) + "..." : id;
            }
            return "Vô Danh";
        }
        return name;
    }

    public void UpdateArenaInfo(int rank, string name, int elo, string title, int power)
    {
        if (rankText != null) rankText.text = rank > 0 ? $"Hạng {rank}" : "Chưa xếp hạng";
        if (nameText != null) nameText.text = GetSafeName(name);
        if (eloText != null) eloText.text = elo.ToString("N0") + " điểm";
        if (powerText != null) powerText.text = power.ToString("N0");
        
        if (titleContainer != null)
        {
            titleContainer.SetActive(!string.IsNullOrEmpty(title));
            if (titleText != null) titleText.text = title;
        }
    }

    [Header("Buttons")]
    [SerializeField] private Button mailButton;
    [SerializeField] private Button friendsButton;

    [Header("Level & Exp")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image expFillImage;

    // Singleton cũ để không làm gãy code hiện tại, nhưng giờ chỉ là 1 trong nhiều instance
    public static PlayerInfoUI Instance { get; private set; }
    
    // Danh sách tất cả các "anh em" PlayerInfoUI đang hoạt động
    private static readonly HashSet<PlayerInfoUI> _allInstances = new HashSet<PlayerInfoUI>();

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        _allInstances.Add(this);
        RefreshData();

        // Tự động cập nhật thông tin Arena từ kho dữ liệu khi UI được bật lên
        if (PlayFabDataManager.Instance != null)
        {
            UpdateArenaInfo(
                PlayFabDataManager.Instance.PlayerRank,
                PlayFabDataManager.Instance.ArenaDisplayName,
                PlayFabDataManager.Instance.Elo,
                PlayFabDataManager.Instance.PlayerTitle,
                PlayFabDataManager.Instance.CalculateTotalPower()
            );
        }
    }

    private void OnDisable()
    {
        _allInstances.Remove(this);
        if (Instance == this) Instance = null;
    }

    public void RefreshData()
    {
        if (PlayFabDataManager.Instance != null)
        {
            UpdatePlayerName(PlayFabDataManager.Instance.PlayerName);
            UpdateExpBar(PlayFabDataManager.Instance.PlayerLevel, 
                         PlayFabDataManager.Instance.PlayerExp, 
                         PlayFabDataManager.Instance.GetRequiredExp(PlayFabDataManager.Instance.PlayerLevel));
            UpdateElo(PlayFabDataManager.Instance.Elo); // Cập nhật Elo khi Refresh
        }
        UpdatePower();
    }

    // Static methods để update toàn bộ các UI cùng lúc
    public static void UpdateAllPlayerName(string name)
    {
        foreach (var ui in _allInstances) ui.UpdatePlayerName(name);
    }

    public static void UpdateAllExpBar(int level, long currentExp, long requiredExp)
    {
        foreach (var ui in _allInstances) ui.UpdateExpBar(level, currentExp, requiredExp);
    }

    public static void UpdateAllElo(int elo)
    {
        foreach (var ui in _allInstances) ui.UpdateElo(elo);
    }

    public static void UpdateAllArenaInfo(int rank, string name, int elo, string title, int power)
    {
        foreach (var ui in _allInstances) ui.UpdateArenaInfo(rank, name, elo, title, power);
    }


    public void UpdatePlayerName(string name)
    {
        if (nameText != null) nameText.text = GetSafeName(name);
    }

    public void UpdateExpBar(int level, long currentExp, long requiredExp)
    {
        if (levelText != null) levelText.text = level.ToString();
        if (expFillImage != null)
        {
            float fill = (float)currentExp / (float)Mathf.Max(1, requiredExp);
            expFillImage.fillAmount = fill;
        }
    }

    public void UpdateElo(int elo)
    {
        if (eloText != null) eloText.text = elo.ToString("N0");
    }

    public static void UpdateAllPower()
    {
        if (PlayFabDataManager.Instance == null) return;
        
        // Cập nhật giá trị mới nhất trong Data Manager
        int currentPower = PlayFabDataManager.Instance.CalculateTotalPower();
        
        // Lưu vào Data Manager để các chỗ khác dùng (như Public Profile)
        // Lưu ý: Chúng ta không set trực tiếp được vì PlayerPower là private set
        // Nhưng UpdatePower của UI sẽ lấy giá trị từ việc tính toán
        foreach (var ui in _allInstances) ui.UpdatePower(currentPower);
    }

    public void UpdatePower(int power = -1)
    {
        int displayPower = power;
        if (displayPower == -1)
        {
            displayPower = (PlayFabDataManager.Instance != null) ? 
                PlayFabDataManager.Instance.CalculateTotalPower() : 0;
        }
        
        if (powerText != null) powerText.text = displayPower.ToString("N0");
    }

    public void UpdateUI(string playerName, string playerTitle, int power)
    {
        if (nameText != null) nameText.text = GetSafeName(playerName);
        if (powerText != null) powerText.text = power.ToString("N0");

        if (titleContainer != null)
        {
            if (string.IsNullOrEmpty(playerTitle))
            {
                titleContainer.SetActive(false);
            }
            else
            {
                titleContainer.SetActive(true);
                if (titleText != null) titleText.text = playerTitle;
            }
        }
    }
}
