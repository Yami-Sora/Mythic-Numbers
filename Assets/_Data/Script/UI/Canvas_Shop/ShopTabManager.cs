using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ShopTabManager : MonoBehaviour
{
    [System.Serializable]
    public class ShopTab
    {
        public string tabName;
        public Button tabButton;
        public GameObject decorateImage;
        public GameObject contentPanel;
    }

    [Header("Cấu hình các Tabs")]
    public ShopTab[] tabs;

    private int currentTabIndex = -1;

    void Start()
    {
        // 1. Gắn sự kiện click cho các nút
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            if (tabs[i].tabButton != null)
            {
                tabs[i].tabButton.onClick.AddListener(() => SwitchTab(index));
            }

            // Tắt hết Decorate và Content lúc mới khởi động
            if (tabs[i].decorateImage != null) tabs[i].decorateImage.SetActive(false);
            if (tabs[i].contentPanel != null) tabs[i].contentPanel.SetActive(false);
        }

        // 2. Tự động mở Tab đầu tiên (Recharge)
        if (tabs.Length > 0) SwitchTab(0);
    }

    public void SwitchTab(int index)
    {
        if (index == currentTabIndex) return; // Đang ở tab này rồi thì không làm gì cả

        // --- TẮT TAB CŨ ---
        if (currentTabIndex >= 0 && currentTabIndex < tabs.Length)
        {
            ShopTab oldTab = tabs[currentTabIndex];

            // Dừng hoạt ảnh cũ & tắt UI
            if (oldTab.decorateImage != null)
            {
                oldTab.decorateImage.transform.DOKill();
                oldTab.decorateImage.SetActive(false);
            }
            if (oldTab.contentPanel != null)
            {
                oldTab.contentPanel.transform.DOKill();
                oldTab.contentPanel.SetActive(false);
            }
            // Trả nút cũ về size gốc
            if (oldTab.tabButton != null) oldTab.tabButton.transform.localScale = Vector3.one;
        }

        // Cập nhật index mới
        currentTabIndex = index;
        ShopTab newTab = tabs[currentTabIndex];

        // --- BẬT VÀ MÚA DOTWEEN CHO TAB MỚI ---

        // 1. Nút bấm lún xuống nảy lên
        if (newTab.tabButton != null)
        {
            // Ép về size gốc trước khi múa để không bị lỗi cộng dồn scale
            newTab.tabButton.transform.localScale = Vector3.one;
            newTab.tabButton.transform.DOPunchScale(new Vector3(-0.1f, -0.1f, 0), 0.3f, 10, 1).SetUpdate(true);
        }

        // 2. Vệt sáng chạy dọc ra
        if (newTab.decorateImage != null)
        {
            newTab.decorateImage.SetActive(true);
            newTab.decorateImage.transform.localScale = new Vector3(1f, 0f, 1f);
            newTab.decorateImage.transform.DOScaleY(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        // 3. Nội dung bên phải phình to ra
        if (newTab.contentPanel != null)
        {
            newTab.contentPanel.SetActive(true);
            newTab.contentPanel.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            newTab.contentPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }
}