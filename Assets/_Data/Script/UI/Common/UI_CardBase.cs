using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_CardBase : MonoBehaviour
{
    [Header("Visuals Base")]
    [SerializeField] protected Image imgCard;
    [SerializeField] protected Image imgFrame;
    [SerializeField] protected TextMeshProUGUI txtCardName;

    [Header("Stats Base")]
    [SerializeField] protected TextMeshProUGUI txtTop;
    [SerializeField] protected TextMeshProUGUI txtRight;
    [SerializeField] protected TextMeshProUGUI txtBottom;
    [SerializeField] protected TextMeshProUGUI txtLeft;

    protected CardDataSO _cardData;

    // Hàm Setup nhận trực tiếp CardDataSO
    public virtual void Setup(CardDataSO data)
    {
        if (data == null) return;
        _cardData = data;

        // Bật điện lên! Thẻ đã vào slot
        ToggleContent(true);

        if (imgCard != null) imgCard.sprite = data.cardImage;
        if (imgFrame != null && data.cardFrame != null) imgFrame.sprite = data.cardFrame;
        if (txtCardName != null) txtCardName.text = data.cardName;

        UpdateBaseStat(txtTop, data.top);
        UpdateBaseStat(txtRight, data.right);
        UpdateBaseStat(txtBottom, data.bottom);
        UpdateBaseStat(txtLeft, data.left);
    }

    // Hàm tiện ích: Bật/Tắt các thành phần phụ (Khung, Tên, 4 Chỉ số)
    // Cố tình bỏ qua imgCard để nó còn làm viền trống
    public virtual void ToggleContent(bool isVisible)
    {
        if (imgFrame != null) imgFrame.gameObject.SetActive(isVisible);
        if (txtCardName != null) txtCardName.transform.parent.gameObject.SetActive(isVisible);
        if (txtTop != null) txtTop.gameObject.SetActive(isVisible);
        if (txtRight != null) txtRight.gameObject.SetActive(isVisible);
        if (txtBottom != null) txtBottom.gameObject.SetActive(isVisible);
        if (txtLeft != null) txtLeft.gameObject.SetActive(isVisible);
    }

    protected void UpdateBaseStat(TextMeshProUGUI txtStat, int rawValue)
    {
        if (txtStat == null) return;
        var realm = RealmCalculator.GetRealmStat(rawValue);
        txtStat.text = realm.displayValue.ToString();
        txtStat.color = realm.displayColor;
    }
}