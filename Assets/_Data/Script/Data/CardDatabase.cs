using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CardDatabase : YamiMonoBehaviour
{
    public static CardDatabase Instance;

    [SerializeField] private CardDataSO[] allCards;

    private Dictionary<int, CardDataSO> _cardLookup;

    protected override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        base.Awake();

        // Sau khi đảm bảo allCards đã có dữ liệu, tạo Dictionary
        InitializeDictionary();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadCardsFromResources();
    }
    private void LoadCardsFromResources()
    {
        if (allCards != null && allCards.Length > 0) return;
        // 1. Load từ folder Resources/CardDataSO
        var loadedObjects = Resources.LoadAll<CardDataSO>("CardDataSO");

        // 2. Sắp xếp theo tên để đảm bảo đồng bộ ID giữa các máy
        allCards = loadedObjects.OrderBy(c => c.name).ToArray();

        // 3. Gán ID ngay lập tức để lưu vào Inspector
        for (int i = 0; i < allCards.Length; i++)
        {
            if (allCards[i] != null)
            {
                allCards[i].cardID = i;
            }
        }

        Debug.LogWarning("Loaded Cards From Resources: ", gameObject);
    }

    // Tạo Dictionary để tìm kiếm nhanh (O(1)) khi game chạy
    private void InitializeDictionary()
    {
        _cardLookup = new Dictionary<int, CardDataSO>();

        if (allCards == null || allCards.Length == 0)
        {
            Debug.LogError("[CardDatabase] Danh sách bài rỗng! Hãy bấm Reset component hoặc kiểm tra folder Resources.");
            return;
        }

        foreach (var card in allCards)
        {
            if (card != null)
            {
                // Đảm bảo ID khớp với index mảng
                if (!_cardLookup.ContainsKey(card.cardID))
                {
                    _cardLookup.Add(card.cardID, card);
                }
            }
        }

        Debug.Log($"[CardDatabase] Dictionary Init Complete. Count: {_cardLookup.Count}");
    }

    public CardDataSO GetCardData(int id)
    {
        if (_cardLookup != null && _cardLookup.ContainsKey(id))
            return _cardLookup[id];
        return null;
    }

    public CardDataSO GetRandomCard()
    {
        if (allCards == null || allCards.Length == 0) return null;
        return allCards[Random.Range(0, allCards.Length)];
    }
}