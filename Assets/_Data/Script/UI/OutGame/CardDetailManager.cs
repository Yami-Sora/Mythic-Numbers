using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDetailManager : MonoBehaviour
{
    public static CardDetailManager Instance { get; private set; }

    [Header("Bên Phải - Kho Ngọc (Tách riêng)")]
    [SerializeField] private GemInventoryUI gemInventoryUI;

    [Header("Bên Trái - Thẻ Bài")]
    [SerializeField] private Image imgCardPreview;
    [SerializeField] private TextMeshProUGUI txtCardName;
    [SerializeField] private TextMeshProUGUI txtCardName2;
    [SerializeField] private TextMeshProUGUI txtCardDesc;
    [SerializeField] private Transform socketContainer;
    [SerializeField] private GameObject socketPrefab;

    [Header("Stats References")]
    [SerializeField] private TextMeshProUGUI txtStatTop;
    [SerializeField] private TextMeshProUGUI txtStatRight;
    [SerializeField] private TextMeshProUGUI txtStatBottom;
    [SerializeField] private TextMeshProUGUI txtStatLeft;

    private CardListManager.OwnedCard _selectedCard;
    private UI_Socket _currentSelectedSocket;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenDetail(CardListManager.OwnedCard card)
    {
        _selectedCard = card;
        gameObject.SetActive(true);

        // Cập nhật thông tin thẻ
        RefreshCardInfo();

        // Ra lệnh cho thằng đệ GemInventoryUI cập nhật danh sách ngọc
        if (gemInventoryUI != null) gemInventoryUI.RefreshGemList();
    }

    private void RefreshCardInfo()
    {
        imgCardPreview.sprite = _selectedCard.data.cardImage;
        txtCardName.text = _selectedCard.data.cardName;
        txtCardName2.text = _selectedCard.data.cardName;

        foreach (Transform child in socketContainer) Destroy(child.gameObject);
        foreach (var dir in _selectedCard.data.availableSockets)
        {
            GameObject go = Instantiate(socketPrefab, socketContainer);
            go.GetComponent<UI_Socket>().Setup(dir);
        }
        RefreshStatsDisplay();
    }
    // Hàm này dùng để cập nhật text chỉ số kèm phần cộng thêm màu xanh
    private void RefreshStatsDisplay()
    {
        if (_selectedCard == null) return;

        // Giả sử mốt sếp có một List<ItemDataSO> slottedGems trong OwnedCard để tính
        // Hiện tại tui demo biến fake để sếp thấy kết quả trên UI nhé
        int bonusTop = 7;    // Mốt sếp viết hàm tính tổng bonus từ ngọc khảm ở đây
        int bonusRight = 26;
        int bonusBottom = 12;
        int bonusLeft = 8;

        UpdateSingleStatText(txtStatTop, "Trên", _selectedCard.data.top, bonusTop);
        UpdateSingleStatText(txtStatRight, "Phải", _selectedCard.data.right, bonusRight);
        UpdateSingleStatText(txtStatBottom, "Dưới", _selectedCard.data.bottom, bonusBottom);
        UpdateSingleStatText(txtStatLeft, "Trái", _selectedCard.data.left, bonusLeft);
    }
    private void UpdateSingleStatText(TextMeshProUGUI tmp, string label, int baseVal, int bonusVal)
    {
        if (tmp == null) return;

        // Dùng Rich Text của TextMeshPro để nhuộm màu xanh lá cho phần cộng thêm
        // Cấu trúc: Tên: Gốc <color=green>(+Thêm)</color>
        string bonusText = bonusVal > 0 ? $" <color=#00FF00>(+{bonusVal})</color>" : "";
        tmp.text = $"{label}: {baseVal}{bonusText}";
    }
    public void OnSocketClicked(UI_Socket socket)
    {
        if (_currentSelectedSocket != null) _currentSelectedSocket.SetHighlight(false);
        _currentSelectedSocket = socket;
        _currentSelectedSocket.SetHighlight(true);
    }

    public void CloseDetail() => gameObject.SetActive(false);
}