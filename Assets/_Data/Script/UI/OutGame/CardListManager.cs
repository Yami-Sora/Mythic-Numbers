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
        HackInitialCards();
        ShowAll();
    }

    private void HackInitialCards()
    {
        if (allCardDatabase == null) return;
        foreach (var cardData in allCardDatabase)
        {
            _ownedCards.Add(new OwnedCard(cardData));
        }
    }

    // ==========================================
    // LOGIC LỌC & TỐI ƯU HIỆU NĂNG (OBJECT POOLING)
    // ==========================================
    public void DisplayCards()
    {
        // 1. LINQ Thông Minh: Lọc Tab VÀ Lọc luôn bài trong Deck ở ngay đây
        var cardsToShow = _ownedCards
            .Where(c => _currentFilter == null || c.data.rate == _currentFilter)
            .Where(c => DeckManager.Instance == null || !IsCardInDeck(c.data.cardID)) // Lọc bài đã trang bị
            .OrderByDescending(c => c.data.rate)
            .ThenBy(c => c.data.cardID)
            .ToList();

        // 2. KHÔNG DÙNG DESTROY NỮA! Lấy thẻ từ Pool ra xài
        for (int i = 0; i < cardsToShow.Count; i++)
        {
            // Thiếu thì mới Instantiate đẻ thêm
            if (i >= _cardPool.Count)
            {
                GameObject go = Instantiate(cardPrefab, cardContainer, false);

                // Set Scale đúng 1 lần lúc mới đẻ ra
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.localScale = new Vector3(0.1f, 0.1f, 0.1f);

                UI_CardSlot newSlot = go.GetComponent<UI_CardSlot>();
                _cardPool.Add(newSlot);
            }

            // Có sẵn rồi thì lôi ra Setup lại Data
            UI_CardSlot slot = _cardPool[i];
            slot.gameObject.SetActive(true); // Bật lên
            slot.Setup(cardsToShow[i]);

            // Quan Trọng: Ép nó xếp xuống dưới cùng để UI hiển thị đúng thứ tự Sort (SSR -> N)
            slot.transform.SetAsLastSibling();
        }

        // 3. Giấu đi những thẻ thừa (Không Destroy)
        // Ví dụ lúc trước xem Tab ALL có 100 thẻ, giờ qua Tab SSR chỉ có 5 thẻ -> Cất 95 thẻ đi
        for (int i = cardsToShow.Count; i < _cardPool.Count; i++)
        {
            _cardPool[i].gameObject.SetActive(false);
        }
    }

    private bool IsCardInDeck(int cardID)
    {
        for (int i = 0; i < DeckManager.Instance.maxDeckSize; i++)
        {
            if (DeckManager.Instance.currentDeck[i] != null &&
                DeckManager.Instance.currentDeck[i].data.cardID == cardID)
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