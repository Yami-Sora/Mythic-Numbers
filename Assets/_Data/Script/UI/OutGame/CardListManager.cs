using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static CardDataSO;

public class CardListManager : MonoBehaviour
{
    public static CardListManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform cardContainer; // Kéo Viewport/Content vào đây
    [SerializeField] private GameObject cardPrefab;   // Kéo Prefab UI_CardSlot vào đây

    [Header("Tab Visuals")]
    [SerializeField] private Image imgTabAll;
    [SerializeField] private Image imgTabSSR;
    [SerializeField] private Image imgTabSR;
    [SerializeField] private Image imgTabR;
    [SerializeField] private Image imgTabN;

    private Color _normalColor = Color.white;
    private Color _selectedColor = new Color(1f, 0.84f, 0f); // Vàng kim (Gold)

    // Khác với túi đồ cộng dồn, thẻ bài mình lưu thành từng thực thể riêng biệt
    private List<OwnedCard> _ownedCards = new List<OwnedCard>();
    private CardRate? _currentFilter = null;

    [Header("--- BỘ BÀI TEST ---")]
    public CardDataSO[] allCardDatabase; //kéo hết thẻ đang có vào đây để test hiển thị

    private void Awake() => Instance = this;

    private void Start()
    {
        HackInitialCards(); // Nạp data test
        ShowAll();          // Mở tab Tất cả lúc mới vào
    }

    [System.Serializable]
    public class OwnedCard
    {
        public CardDataSO data;
        public int level = 1;
        // Mở rộng sau: public List<ItemDataSO> slottedGems; (Lưu các ngọc đang khảm)

        public OwnedCard(CardDataSO data)
        {
            this.data = data;
        }
    }

    // Nạp toàn bộ thẻ vào túi để test UI
    private void HackInitialCards()
    {
        if (allCardDatabase == null) return;
        foreach (var cardData in allCardDatabase)
        {
            _ownedCards.Add(new OwnedCard(cardData));
            // Có thể copy dòng trên ra nhiều lần để fake việc có nhiều thẻ trùng nhau
        }
    }

    // ==========================================
    // LOGIC LỌC (FILTER) & SẮP XẾP (SORT) MƯỢT MÀ
    // ==========================================
    public void DisplayCards(CardRate? filterRate = null)
    {
        _currentFilter = filterRate;

        // 1. Dọn dẹp Content
        foreach (Transform child in cardContainer) Destroy(child.gameObject);

        // 2. Phép thuật LINQ: Lọc và Sắp xếp
        var cardsToShow = _ownedCards
            .Where(c => filterRate == null || c.data.rate == filterRate) // Lọc theo Tab
            .OrderByDescending(c => c.data.rate)                         // Sort 1: Thẻ xịn (SSR) lên trước
            .ThenBy(c => c.data.cardID)                                  // Sort 2: Cùng rate thì xếp theo tên/ID
            .ToList();

        // 3. Render ra UI
        foreach (var card in cardsToShow)
        {
            GameObject go = Instantiate(cardPrefab, cardContainer, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;

            rect.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            rect.anchoredPosition3D = Vector3.zero;

            // ĐOẠN QUAN TRỌNG ĐÂY: Gọi Setup để đổ dữ liệu vào ô thẻ
            UI_CardSlot slotScript = go.GetComponent<UI_CardSlot>();

            if (slotScript != null)
            {
                slotScript.Setup(card);
            }
            else
            {
                Debug.LogError("Prefab thẻ bài chưa gắn script UI_CardSlot kìa!");
            }
        }
    }

    // ==========================================
    // XỬ LÝ NÚT BẤM (Gán vào OnClick của Button)
    // ==========================================
    public void ShowAll() { DisplayCards(null); HighlightTab(imgTabAll); }
    public void ShowSSR() { DisplayCards(CardRate.SSR); HighlightTab(imgTabSSR); }
    public void ShowSR() { DisplayCards(CardRate.SR); HighlightTab(imgTabSR); }
    public void ShowR() { DisplayCards(CardRate.R); HighlightTab(imgTabR); }
    public void ShowN() { DisplayCards(CardRate.N); HighlightTab(imgTabN); }

    private void HighlightTab(Image activeTabImg)
    {
        if (imgTabAll) imgTabAll.color = _normalColor;
        if (imgTabSSR) imgTabSSR.color = _normalColor;
        if (imgTabSR) imgTabSR.color = _normalColor;
        if (imgTabR) imgTabR.color = _normalColor;
        if (imgTabN) imgTabN.color = _normalColor;

        if (activeTabImg) activeTabImg.color = _selectedColor;
    }
}