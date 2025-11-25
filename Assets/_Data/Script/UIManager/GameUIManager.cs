using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Launcher UI References")]
    [SerializeField] private TMP_Text onlineButtonText;
    [SerializeField] private TMP_Text offlineButtonText;
    [SerializeField] private Image LeftAiImage;
    [SerializeField] private Image LeftPlayer2;


    [Header("Color Settings")]
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color defaultColor = Color.white;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Hàm công khai để đổi màu nút chọn Mode
    public void SetLauncherModeUI(bool isOnline)
    {
        if (isOnline)
        {
            SetTextColor(onlineButtonText, selectedColor);
            SetTextColor(offlineButtonText, defaultColor);
        }
        else
        {
            SetTextColor(onlineButtonText, defaultColor);
            SetTextColor(offlineButtonText, selectedColor);
        }
    }

    // Hàm tiện ích nội bộ để set màu an toàn (tránh lỗi null)
    private void SetTextColor(TMP_Text textComponent, Color color)
    {
        if (textComponent != null)
        {
            textComponent.color = color;
        }
    }

    public void SetLeftImageActive(bool isActive)
    {
        if (LeftAiImage != null)
        {
            LeftAiImage.gameObject.SetActive(isActive);
        }
        if (LeftPlayer2 != null)
        {
            LeftPlayer2.gameObject.SetActive(!isActive);
        }

    }
}