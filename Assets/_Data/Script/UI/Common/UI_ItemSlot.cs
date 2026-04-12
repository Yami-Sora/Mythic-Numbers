using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_ItemSlot : MonoBehaviour
{
    [SerializeField] private Image imgIcon;
    [SerializeField] private Image imgFrame; // Cái viền của ô
    [SerializeField] private TextMeshProUGUI txtAmount;
    [SerializeField] private Button btnSlot;

    // Màu xám cho ô trống
    private Color _emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    public void Setup(InventoryManager.InventoryItem item)
    {
        this.gameObject.SetActive(true);
        // 1. Hiện hình vật phẩm
        imgIcon.gameObject.SetActive(true);
        imgIcon.sprite = item.data.icon;

        // 2. Hiện số lượng
        txtAmount.gameObject.SetActive(true);
        txtAmount.text = item.amount.ToString();

        // 3. Đổi Frame và Màu sắc từ DataSO
        if (imgFrame != null)
        {
            imgFrame.gameObject.SetActive(true);
            // Nếu sếp có nhiều loại hình dáng viền khác nhau (tròn, vuông, cánh sen...)
            if (item.data.frame != null) imgFrame.sprite = item.data.frame;

            // Nhuộm màu viền theo độ hiếm sếp đã chỉnh trong SO
            imgFrame.color = item.data.itemColor;
        }

        btnSlot.interactable = true;

        if (btnSlot != null)
        {
            btnSlot.onClick.RemoveAllListeners();
            btnSlot.onClick.AddListener(() =>
            {
                // Gọi ông thần Popup dậy. Truyền false vì ngọc trong túi thì chưa được trang bị.
                if (GemInfoPopupManager.Instance != null)
                {
                    GemInfoPopupManager.Instance.OpenPopup(item, isEquipped: false);
                }
            });
        }
    }

    public void SetupEmpty()
    {
        this.gameObject.SetActive(true);

        imgFrame.gameObject.SetActive(true); 
        // Ẩn icon và số lượng
        imgIcon.gameObject.SetActive(false);
        txtAmount.gameObject.SetActive(false);

        // Chuyển viền thành màu xám mờ
        imgFrame.color = _emptyColor;

        // Không cho bấm
        btnSlot.interactable = false;
        btnSlot.onClick.RemoveAllListeners();
    }
}