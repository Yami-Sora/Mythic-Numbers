using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_ItemSlot : MonoBehaviour
{
    [SerializeField] private Image imgIcon;
    [SerializeField] private Image imgFrame; // Cái nền của ô
    [SerializeField] private TextMeshProUGUI txtAmount;
    [SerializeField] private Button btnSlot;

    // Màu xám cho ô trống
    private Color _emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    public void Setup(InventoryItem item)
    {
        this.gameObject.SetActive(true);
        // 1. Hiện hình vật phẩm
        imgIcon.gameObject.SetActive(true);
        imgIcon.sprite = item.data.icon;

        // 2. Hiện số lượng
        if (txtAmount != null)
        {
            if (item.data.type == ItemDataSO.ItemType.Gem)
            {
                // Nếu là Ngọc -> Tắt luôn chữ đi cho nó sang
                txtAmount.gameObject.SetActive(false);
            }
            else
            {
                // Nếu là Đạo cụ -> Bật chữ và hiện số lượng (Ví dụ x99)
                txtAmount.gameObject.SetActive(true);
                txtAmount.text = item.amount.ToString();
            }
        }

        // 3. Đổi Frame và Màu sắc từ DataSO
        if (imgFrame != null)
        {
            imgFrame.gameObject.SetActive(true);

            if (imgFrame != null) imgFrame.color = item.data.GetFrameColor();
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