using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_CardSlot : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Image imgCard;
    [SerializeField] private Image imgFrame; 
    [SerializeField] private TextMeshProUGUI txtCardName;

    [Header("Stats Base")]
    [SerializeField] private TextMeshProUGUI txtTop;
    [SerializeField] private TextMeshProUGUI txtRight;
    [SerializeField] private TextMeshProUGUI txtBottom;
    [SerializeField] private TextMeshProUGUI txtLeft;

    [Header("Interaction")]
    [SerializeField] private Button btnCard;

    // Lưu lại data để mốt click vào còn biết đường quăng sang bảng Khảm Ngọc
    private CardListManager.OwnedCard _currentCard;

    // Hàm này sẽ được gọi ở trong vòng lặp của CardListManager
    public void Setup(CardListManager.OwnedCard cardInfo)
    {
        _currentCard = cardInfo;
        CardDataSO data = cardInfo.data;

        // 1. Gán hình ảnh và tên
        if (imgCard != null) imgCard.sprite = data.cardImage;
        if (imgFrame != null && data.cardFrame != null) imgFrame.sprite = data.cardFrame;
        if (txtCardName != null) txtCardName.text = data.cardName;

        // 2. Gán chỉ số (Giữ nguyên logic quy đổi Cảnh Giới siêu cuốn của sếp)
        UpdateBaseStat(txtTop, data.top);
        UpdateBaseStat(txtRight, data.right);
        UpdateBaseStat(txtBottom, data.bottom);
        UpdateBaseStat(txtLeft, data.left);

        // 3. Nạp đạn cho nút bấm
        if (btnCard != null)
        {
            btnCard.onClick.RemoveAllListeners();
            btnCard.onClick.AddListener(OnCardClicked);
        }
    }

    // Hàm tái sử dụng để set Text + Màu cho 4 góc
    private void UpdateBaseStat(TextMeshProUGUI txtStat, int rawValue)
    {
        if (txtStat == null) return;

        // Tận dụng lại cái lò bát quái RealmCalculator của sếp
        var realm = RealmCalculator.GetRealmStat(rawValue);

        txtStat.text = realm.displayValue.ToString();
        txtStat.color = realm.displayColor; // Nhuộm màu cảnh giới
    }

    private void OnCardClicked()
    {
        Debug.Log("<color=yellow>Sếp vừa chạm vào thần bài: " + _currentCard.data.cardName + "</color>");
        CardDetailManager.Instance.gameObject.SetActive(true);
        CardListManager.Instance.gameObject.SetActive(false);
        CardDetailManager.Instance.OpenDetail(_currentCard);
    }
}