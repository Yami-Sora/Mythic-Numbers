using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CardListManager : MonoBehaviour
{
    public static CardListManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject cardPrefab;

    [Header("Tab Visuals")]
    [SerializeField] private Image imgTabAll;
    [SerializeField] private Image imgTabSSR;
    [SerializeField] private Image imgTabSR;
    [SerializeField] private Image imgTabR;
    [SerializeField] private Image imgTabN;

    private Color _normalColor = Color.white;
    private Color _selectedColor = new Color(1f, 0.84f, 0f);

    private List<OwnedCard> _ownedCards = new List<OwnedCard>();
    public List<OwnedCard> GetOwnedCards() => _ownedCards;

    private CardDataSO.CardRate? _currentFilter = null;

    [Header("--- BỘ BÀI TEST ---")]
    public CardDataSO[] allCardDatabase;

    // [BÍ KÍP TỐI ƯU]: Danh sách chứa các thẻ bài đã được nặn ra (Object Pool)
    private List<UI_CardSlot> _cardPool = new List<UI_CardSlot>();

    private void Awake() => Instance = this;

    private void Start()
    {
        ShowAll();
    }

    // ==========================================
    // LOGIC LỌC & TỐI ƯU HIỆU NĂNG (OBJECT POOLING)
    // ==========================================
    public void DisplayCards()
    {
        // 1. LINQ Thông Minh: Mặc thêm 1 lớp giáp chống rác (c.data != null)
        var cardsToShow = _ownedCards
            .Where(c => c != null && c.data != null) // [BỌC THÉP]: Lọc ngay mấy cái thẻ ma rỗng ruột
            .Where(c => _currentFilter == null || c.data.rate == _currentFilter)
            .Where(c => !IsCardInDeck(c.data.cardID)) // Chuyển check Instance vào trong IsCardInDeck luôn cho gọn
            .OrderByDescending(c => c.data.rate)
            .ThenBy(c => c.data.cardID)
            .ToList();

        // 2. KHÔNG DÙNG DESTROY NỮA! Lấy thẻ từ Pool ra xài
        for (int i = 0; i < cardsToShow.Count; i++)
        {
            if (i >= _cardPool.Count)
            {
                GameObject go = Instantiate(cardPrefab, cardContainer, false);
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.localScale = new Vector3(1f, 1f, 1f);

                UI_CardSlot newSlot = go.GetComponent<UI_CardSlot>();
                _cardPool.Add(newSlot);
            }

            UI_CardSlot slot = _cardPool[i];
            slot.gameObject.SetActive(true);
            slot.Setup(cardsToShow[i]);
            slot.transform.SetAsLastSibling();
        }

        // 3. Giấu đi những thẻ thừa
        for (int i = cardsToShow.Count; i < _cardPool.Count; i++)
        {
            _cardPool[i].gameObject.SetActive(false);
        }
    }

    // [BỌC THÉP]: Phiên bản check thẻ 3 lớp siêu an toàn
    private bool IsCardInDeck(int cardID)
    {
        // Nhỡ DeckManager chưa kịp tỉnh dậy thì bỏ qua
        if (DeckManager.Instance == null || DeckManager.Instance.currentDeck == null) return false;

        for (int i = 0; i < DeckManager.Instance.maxDeckSize; i++)
        {
            var cardInDeck = DeckManager.Instance.currentDeck[i];

            // Check đủ 3 bước: Có thẻ ko? Thẻ có linh hồn (data) ko? ID có khớp ko?
            if (cardInDeck != null && cardInDeck.data != null && cardInDeck.data.cardID == cardID)
            {
                return true;
            }
        }
        return false;
    }
    // ==========================================
    // XỬ LÝ NÚT BẤM
    // ==========================================
    public void ShowAll() { _currentFilter = null; DisplayCards(); HighlightTab(imgTabAll); }
    public void ShowSSR() { _currentFilter = CardDataSO.CardRate.SSR; DisplayCards(); HighlightTab(imgTabSSR); }
    public void ShowSR() { _currentFilter = CardDataSO.CardRate.SR; DisplayCards(); HighlightTab(imgTabSR); }
    public void ShowR() { _currentFilter = CardDataSO.CardRate.R; DisplayCards(); HighlightTab(imgTabR); }
    public void ShowN() { _currentFilter = CardDataSO.CardRate.N; DisplayCards(); HighlightTab(imgTabN); }

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