using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class CurrencyUIManager : MonoBehaviour
{
    public static CurrencyUIManager Instance { get; private set; }

    [Header("UI Tiền Tệ (Top Bar)")]
    public TextMeshProUGUI txtGold;
    public TextMeshProUGUI txtGem;

    [Header("UI Thể Lực (Top Bar)")]
    public TextMeshProUGUI txtStamina;
    public Button btnAddStamina;

    private int currentStamina;
    private int maxStamina = 200;
    private float regenTimer = 0f;
    private bool isRegenerating = false;
    private int _lastDisplayedSecond = -1;
    private int _currentGemBalance = 0;

    private void Awake() => Instance = this;

    private void Start()
    {
        // Bấm nút + thì gọi thằng Quản lý Popup ra làm việc
        if (btnAddStamina != null)
        {
            btnAddStamina.onClick.AddListener(() => {
                if (StaminaPopupManager.Instance != null)
                {
                    StaminaPopupManager.Instance.OpenPopup();
                    PushDataToPopup();
                }
            });
        }
    }

    public void UpdateBalances(int gold, int gem)
    {
        _currentGemBalance = gem;
        if (txtGold) txtGold.text = gold.ToString("N0");
        if (txtGem) txtGem.text = gem.ToString("N0");

        // Cập nhật số dư cho Popup nếu nó đang được mở
        PushDataToPopup();
    }

    public void UpdateStamina(int staminaAmount, int secondsFromPlayFab)
    {
        currentStamina = staminaAmount;
        regenTimer = secondsFromPlayFab;
        isRegenerating = currentStamina < maxStamina;
        _lastDisplayedSecond = -1;
        RefreshStaminaUI();
    }

    private void Update()
    {
        if (isRegenerating)
        {
            regenTimer -= Time.deltaTime;
            if (regenTimer <= 0)
            {
                currentStamina++;
                regenTimer = 300f;
                isRegenerating = currentStamina < maxStamina;
                _lastDisplayedSecond = -1;
                RefreshStaminaUI();
            }
            else
            {
                int currentSecond = Mathf.CeilToInt(regenTimer);
                if (currentSecond != _lastDisplayedSecond)
                {
                    _lastDisplayedSecond = currentSecond;
                    PushDataToPopup(); // Bắn 1 giây sang popup
                }
            }
        }
    }

    private void RefreshStaminaUI()
    {
        if (txtStamina != null) txtStamina.text = $"{currentStamina}/{maxStamina}";

        if (currentStamina >= maxStamina) isRegenerating = false;

        PushDataToPopup();
    }

    // Hàm trung chuyển: Đẩy dữ liệu nóng hổi từ TopBar sang cho thằng quản lý Popup
    private void PushDataToPopup()
    {
        if (StaminaPopupManager.Instance != null)
        {
            bool isMax = currentStamina >= maxStamina;
            TimeSpan time = TimeSpan.FromSeconds(regenTimer);
            string timeStr = time.ToString(@"mm\:ss");

            StaminaPopupManager.Instance.UpdatePopupRealtimeData(timeStr, isMax, _currentGemBalance);
        }
    }
}