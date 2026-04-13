using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static CardDataSO;

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
    private Color _selectedColor = new Color(1f, 0.84f, 0f); // Vàng kim (Gold)

    private List<OwnedCard> _ownedCards = new List<OwnedCard>();
    private CardRate? _currentFilter = null;

    [Header("--- BỘ BÀI TEST ---")]
    public CardDataSO[] allCardDatabase;

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
    // LOGIC LỌC (FILTER) & SẮP XẾP (SORT) MƯỢT MÀ
    // ==========================================
    public void DisplayCards(CardRate? filterRate = null)
    {
        _currentFilter = filterRate;

        // 1. Dọn dẹp Content (Áp dụng bí kíp Duyệt Ngược chống lỗi kẹt UI)
        for (int i = cardContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = cardContainer.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        // 2. Phép thuật LINQ: Lọc và Sắp xếp
        var cardsToShow = _ownedCards
            .Where(c => filterRate == null || c.data.rate == filterRate)
            .OrderByDescending(c => c.data.rate)
            .ThenBy(c => c.data.cardID)
            .ToList();

        // 3. Render ra UI
        foreach (var card in cardsToShow)
        {
            GameObject go = Instantiate(cardPrefab, cardContainer, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            // Giữ nguyên setting Scale của sếp (sếp đang set Vector3.one xong lại set lại thành 0.1f)
            rect.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            rect.anchoredPosition3D = Vector3.zero;

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