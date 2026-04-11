using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDetailManager : YamiMonoBehaviour
{
    public static CardDetailManager Instance { get; private set; }

    [Header("Bên Phải - Kho Ngọc (Tách riêng)")]
    [SerializeField] private GemInventoryUI gemInventoryUI;

    [Header("Bên Trái - Thẻ Bài (Preview)")]

    [SerializeField] private UI_CardBase cardPreviewVisual;

    [Header("Bên Trái - Thông Tin Râu Ria")]
    [SerializeField] private TextMeshProUGUI txtCardName;
    [SerializeField] private TextMeshProUGUI txtCardName2;
    [SerializeField] private TextMeshProUGUI txtCardDesc;
    [SerializeField] private Transform socketContainer;
    [SerializeField] private GameObject socketPrefab;

    [Header("Stats References (Kèm Bonus)")]
    [SerializeField] private TextMeshProUGUI txtStatTop;
    [SerializeField] private TextMeshProUGUI txtStatRight;
    [SerializeField] private TextMeshProUGUI txtStatBottom;
    [SerializeField] private TextMeshProUGUI txtStatLeft;

    private CardListManager.OwnedCard _selectedCard;
    private UI_Socket _currentSelectedSocket;

    protected override void Awake()
    {
        base.Awake();
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenDetail(CardListManager.OwnedCard card)
    {
        _selectedCard = card;
        gameObject.SetActive(true);

        RefreshCardInfo();

        if (gemInventoryUI != null) gemInventoryUI.RefreshGemList();
    }

    private void RefreshCardInfo()
    {
        // 1. Vẽ thẻ bài và chữ (Giữ nguyên)
        if (cardPreviewVisual != null) cardPreviewVisual.Setup(_selectedCard.data);
        if (txtCardName != null) txtCardName.text = _selectedCard.data.cardName;
        if (txtCardName2 != null) txtCardName2.text = _selectedCard.data.cardName;
        if(txtCardDesc != null) txtCardDesc.text = _selectedCard.data.skill.description;

        // --- ĐOẠN PHÁP THUẬT RENDER LỖ NGỌC MỚI ---

        // 2. Gom hết 12 cái lỗ sếp đã xếp sẵn bằng tay vào 1 mảng
        UI_Socket[] allSockets = socketContainer.GetComponentsInChildren<UI_Socket>(true);

        // 3. Tắt sạch đi trước (Giấu đi)
        foreach (var socket in allSockets)
        {
            socket.gameObject.SetActive(false);
        }

        // 4. Thẻ bài có bao nhiêu lỗ thì bật sáng bấy nhiêu cái
        var cardSockets = _selectedCard.data.availableSockets;
        for (int i = 0; i < cardSockets.Count; i++)
        {
            if (i < allSockets.Length)
            {
                allSockets[i].gameObject.SetActive(true); // Hồi sinh nó
                allSockets[i].Setup(cardSockets[i]);      // Truyền công lực (hướng) vào
            }
            else
            {
                Debug.LogWarning("Thẻ này đục nhiều lỗ hơn số chỗ sếp xếp trên UI rồi kìa!");
            }
        }

        // -----------------------------------------

        RefreshStatsDisplay();
    }

    private void RefreshStatsDisplay()
    {
        if (_selectedCard == null) return;

        int bonusTop = 7;
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
        string bonusText = bonusVal > 0 ? $" <color=#00FF00>(+{bonusVal})</color>" : "";
        tmp.text = $"{label}: {baseVal}{bonusText}";
    }

    public void OnSocketClicked(UI_Socket socket)
    {
        if (_currentSelectedSocket != null) _currentSelectedSocket.SetHighlight(false);
        _currentSelectedSocket = socket;
        _currentSelectedSocket.SetHighlight(true);
    }

    public void CloseDetail() 
    {
        CardListManager.Instance.gameObject.SetActive(true);
        this.gameObject.SetActive(false); 
    }
}