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
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI powerText;

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

    public static void UpdateAllPower()
    {
        foreach (var ui in _allInstances) ui.UpdatePower();
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
            float fill = (float)currentExp / (float)Mathf.Max(1, requiredExp);
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
