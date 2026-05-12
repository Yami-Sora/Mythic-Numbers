using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Chịu trách nhiệm chia bài và spawn card – tách khỏi GameManager (SRP).
/// </summary>
public class CardDealer
{
    private readonly GameObject _cardPrefab;

    public CardDealer(GameObject cardPrefab)
    {
        _cardPrefab = cardPrefab;
    }

    public void DealCards(CardDataCache[] p1Deck, CardDataCache[] p2Deck, out List<CardObj> p1Hand, out List<CardObj> p2Hand)
    {
        p1Hand = new List<CardObj>();
        p2Hand = new List<CardObj>();

        // Dọn dẹp tay bài trước khi chia mới (Xóa sạch 'con cái' cũ)
        if (InGameUIManager.Instance != null)
        {
            if (InGameUIManager.Instance.LeftHandPos != null)
                foreach (Transform child in InGameUIManager.Instance.LeftHandPos) Object.Destroy(child.gameObject);
            if (InGameUIManager.Instance.RightHandPos != null)
                foreach (Transform child in InGameUIManager.Instance.RightHandPos) Object.Destroy(child.gameObject);
        }

        // Dùng counter riêng cho mỗi player thay vì index tuyệt đối.
        // Đảm bảo HandIndex luôn = 0,1,2... liên tục dù deck có slot trống.
        // OrderRule sẽ luôn force đúng vị trí đầu tiên có card thực sự (top-left).
        int p1Index = 0;
        int p2Index = 0;

        for (int i = 0; i < 5; i++)
        {
            if (p1Deck[i].CardID != -1) p1Hand.Add(SpawnCard(0, p1Index++, p1Deck[i]));
            if (p2Deck[i].CardID != -1) p2Hand.Add(SpawnCard(1, p2Index++, p2Deck[i]));
        }
    }

    public CardObj SpawnCard(int ownerID, int index, CardDataCache cardData)
    {
        if (CardDatabase.Instance == null)
        {
            Debug.LogError("CardDatabase not found! Make sure it exists in the scene.");
            return null;
        }

        if (cardData.CardID == -1) return null; // Không có bài trong slot này

        CardDataSO data = CardDatabase.Instance.GetCardData(cardData.CardID);
        if (data == null) return null;

        var go = Object.Instantiate(_cardPrefab, Vector3.zero, Quaternion.identity);
        CardObj card = go.GetComponent<CardObj>();

        card.OwnerID = ownerID;
        card.HandIndex = index;
        card.CardID = data.cardID;
        
        // Sử dụng chỉ số đã buff truyền từ LocalDeckContext sang
        card.Top = cardData.Top;
        card.Right = cardData.Right;
        card.Bottom = cardData.Bottom;
        card.Left = cardData.Left;

        card.RefreshState();

        if (data.skill != null)
            data.skill.OnCardSpawned(card);

        return card;
    }
}
