using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerInfoUI : MonoBehaviour
{
    [Header("Avatar & Title")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private GameObject titleContainer;
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Player Info")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI powerText;

    [Header("Buttons")]
    [SerializeField] private Button mailButton;
    [SerializeField] private Button friendsButton;

    [Header("Level & Exp")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image expFillImage;


    public static PlayerInfoUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Khởi tạo ban đầu với dữ liệu từ PlayFabDataManager nếu có
        if (PlayFabDataManager.Instance != null)
        {
            UpdatePlayerName(PlayFabDataManager.Instance.PlayerName);
            UpdateExpBar(PlayFabDataManager.Instance.PlayerLevel, 
                         PlayFabDataManager.Instance.PlayerExp, 
                         PlayFabDataManager.Instance.GetRequiredExp(PlayFabDataManager.Instance.PlayerLevel));
        }
        
        UpdatePower();
    }

    public void UpdatePlayerName(string name)
    {
        if (nameText != null) nameText.text = name;
    }

    public void UpdateExpBar(int level, long currentExp, long requiredExp)
    {
        if (levelText != null) levelText.text = level.ToString();
        if (expFillImage != null)
        {
            float fill = (float)currentExp / requiredExp;
            expFillImage.fillAmount = fill;
        }
    }

    public void UpdatePower()
    {
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
        
        if (powerText != null) powerText.text = totalPower.ToString("N0");
    }

    public void UpdateUI(string playerName, string playerTitle, int power)
    {
        if (nameText != null) nameText.text = playerName;
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
