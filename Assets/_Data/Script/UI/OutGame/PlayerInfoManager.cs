using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;

public class PlayerInfoManager : MonoBehaviour
{
    public static PlayerInfoManager Instance { get; private set; }

    [Header("UI References - Left Info")]
    [SerializeField] private Image imgAvatar;
    [SerializeField] private TextMeshProUGUI txtName;
    [SerializeField] private TextMeshProUGUI txtLevel;
    [SerializeField] private TextMeshProUGUI txtExp;
    [SerializeField] private Image imgExpFill;
    [SerializeField] private TextMeshProUGUI txtPower;
    [SerializeField] private TextMeshProUGUI txtGuild;
    [SerializeField] private TextMeshProUGUI txtUID;

    [Header("UI References - Settings")]
    [SerializeField] private Slider sliderMusic;
    [SerializeField] private Slider sliderSound;
    [SerializeField] private Button btnLogout;
    [SerializeField] private Button btnGiftCode;
    [SerializeField] private Button btnLanguage;
    [SerializeField] private Button btnClose;
    [SerializeField] private Button btnCopyUID;
    [SerializeField] private Button btnEditName;

    private void Awake()
    {
        Instance = this;

        // Gán sự kiện cho các nút
        if (btnClose) btnClose.onClick.AddListener(ClosePopup);
        if (btnLogout) btnLogout.onClick.AddListener(OnLogoutClick);
        if (btnCopyUID) btnCopyUID.onClick.AddListener(CopyUIDToClipboard);
        if (btnGiftCode) btnGiftCode.onClick.AddListener(OnGiftCodeClick);
        if (btnLanguage) btnLanguage.onClick.AddListener(OnLanguageClick);
        if (btnEditName) btnEditName.onClick.AddListener(OnEditNameClick);

        // Gán sự kiện cho Slider
        if (sliderMusic) sliderMusic.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sliderSound) sliderSound.onValueChanged.AddListener(OnSoundVolumeChanged);
    }

    public void OpenPopup()
    {
        gameObject.SetActive(true);
        UpdatePlayerUI();
    }

    public void ClosePopup()
    {
        gameObject.SetActive(false);
    }

    private string GetSafeName(string name)
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

    private void UpdatePlayerUI()
    {
        if (PlayFabDataManager.Instance == null) return;

        // 1. Lấy UID từ PlayFab
        if (txtUID != null && PlayFab.PlayFabSettings.staticPlayer != null)
            txtUID.text = PlayFab.PlayFabSettings.staticPlayer.PlayFabId;

        // 2. Tên hiển thị
        if (txtName) txtName.text = GetSafeName(PlayFabDataManager.Instance.PlayerName); 

        // 3. Logic Exp & Level
        int currentLevel = PlayFabDataManager.Instance.PlayerLevel; 
        long currentExp = PlayFabDataManager.Instance.PlayerExp;
        long maxExp = PlayFabDataManager.Instance.GetRequiredExp(currentLevel);

        if (txtLevel) txtLevel.text = "Lv." + currentLevel;
        if (txtExp) txtExp.text = string.Format("{0}/{1}", currentExp, maxExp);
        
        if (imgExpFill != null)
        {
            float progress = (float)currentExp / maxExp;
            imgExpFill.fillAmount = Mathf.Clamp01(progress);
        }

        // 4. Lực chiến (Tính toán từ Deck)
        UpdatePowerText();

        if (txtGuild) txtGuild.text = "Chưa có"; 

        // Cập nhật Slider từ PlayerPrefs
        if (sliderMusic) sliderMusic.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        if (sliderSound) sliderSound.value = PlayerPrefs.GetFloat("SoundVolume", 1f);
    }

    private void UpdatePowerText()
    {
        if (txtPower == null) return;

        int totalPower = 0;
        if (DeckManager.Instance != null && DeckManager.Instance.currentDeck != null)
        {
            foreach (var card in DeckManager.Instance.currentDeck)
            {
                if (card != null && card.data != null)
                {
                    totalPower += card.GetTotalTop();
                    totalPower += card.GetTotalRight();
                    totalPower += card.GetTotalBottom();
                    totalPower += card.GetTotalLeft();
                }
            }
        }
        txtPower.text = totalPower.ToString("N0");
    }

    private void CopyUIDToClipboard()
    {
        if (txtUID != null)
        {
            GUIUtility.systemCopyBuffer = txtUID.text;
            Debug.Log("<color=green>[PlayerInfo]</color> Đã copy UID vào Clipboard!");
            // Sếp có thể hiện một cái thông báo nhỏ ở đây
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        // Gọi AudioManager của sếp ở đây: AudioManager.Instance.SetMusicVolume(value);
    }

    private void OnSoundVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SoundVolume", value);
        // Gọi AudioManager của sếp ở đây: AudioManager.Instance.SetSoundVolume(value);
    }

    private void OnLogoutClick()
    {
        Debug.Log("<color=red>[PlayerInfo]</color> Đang đăng xuất...");
        
        // Xóa thông tin đăng nhập
        PlayFabClientAPI.ForgetAllCredentials();
        
        // Reset dữ liệu cục bộ nếu cần
        if (PlayFabDataManager.Instance != null)
        {
            // Có thể thêm hàm ResetData trong PlayFabDataManager nếu cần xóa sạch cache
        }

        // Quay về màn hình Menu/Login (Tùy cấu trúc dự án của sếp)
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }

    private void OnGiftCodeClick()
    {
        Debug.Log("<color=yellow>[PlayerInfo]</color> Mở bảng nhập GiftCode");
    }

    private void OnLanguageClick()
    {
        Debug.Log("<color=cyan>[PlayerInfo]</color> Mở cài đặt ngôn ngữ");
    }

    private void OnEditNameClick()
    {
        Debug.Log("<color=magenta>[PlayerInfo]</color> Mở popup đổi tên");
    }
}
