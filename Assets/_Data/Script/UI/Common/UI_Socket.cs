using UnityEngine;
using UnityEngine.UI;

public class UI_Socket : MonoBehaviour
{
    [Header("UI Layers")]
    [SerializeField] private Image imgHighlight;
    [SerializeField] private Image imgFrame;
    [SerializeField] private Image imgIcon;
    [SerializeField] private Image imgLock;      // Biến chứa cái IconLock của sếp

    [Header("Interaction")]
    [SerializeField] private Button btnSocket;

    public InventoryManager.InventoryItem EquippedGem { get; private set; }
    public bool IsUnlocked { get; private set; } // Thẻ căn cước check mở khóa

    private void Awake()
    {
        // Chống lười: Lỡ sếp quên kéo Button vào thì nó tự đi tìm
        if (btnSocket == null) btnSocket = GetComponent<Button>();

        if (btnSocket != null)
        {
            btnSocket.onClick.RemoveAllListeners();
            btnSocket.onClick.AddListener(OnClickSocket);
        }
    }

    // Thiết lập trạng thái Khóa / Mở
    public void SetupState(bool isLocked)
    {
        IsUnlocked = !isLocked;
        EquippedGem = null;

        if (imgFrame) imgFrame.gameObject.SetActive(true);
        if (imgHighlight)
        {
            imgHighlight.gameObject.SetActive(true);
            imgHighlight.color = Color.white; // Màu nền mặc định
        }
        if (imgIcon) imgIcon.gameObject.SetActive(false);

        // Ổ khóa và Tương tác
        if (imgLock) imgLock.gameObject.SetActive(isLocked);
        if (btnSocket) btnSocket.interactable = !isLocked;
    }

    // Đổi màu vàng khi chờ khảm
    public void SetReadyToEquip(bool isReady)
    {
        if (imgHighlight != null)
        {
            imgHighlight.color = isReady ? Color.yellow : Color.white;
        }
    }

    public void EquipGem(InventoryManager.InventoryItem gem)
    {
        EquippedGem = gem;
        if (imgIcon != null && gem != null)
        {
            imgIcon.sprite = gem.data.icon;
            imgIcon.gameObject.SetActive(true);
        }
        SetReadyToEquip(false);
    }

    public void OnClickSocket()
    {
        if (CardDetailManager.Instance != null)
        {
            CardDetailManager.Instance.OnSocketClicked(this);
        }
    }
}