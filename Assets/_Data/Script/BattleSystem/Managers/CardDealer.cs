using Fusion;
using UnityEngine;

/// <summary>
/// Chịu trách nhiệm chia bài và spawn card – tách khỏi GameManagerNet (SRP).
/// </summary>
public class CardDealer
{
    private readonly NetworkRunner _runner;
    private readonly NetworkObject _cardPrefab;

    public CardDealer(NetworkRunner runner, NetworkObject cardPrefab)
    {
        _runner = runner;
        _cardPrefab = cardPrefab;
    }

    public void DealCards(CardDataCache[] p1Deck, CardDataCache[] p2Deck)
    {
        for (int i = 0; i < 5; i++)
        {
            SpawnCard(0, i, p1Deck[i]);
            SpawnCard(1, i, p2Deck[i]);
        }
    }

    public void SpawnCard(int ownerID, int index, CardDataCache cardData)
    {
        if (CardDatabase.Instance == null)
        {
            Debug.LogError("CardDatabase not found! Make sure it exists in the scene.");
            return;
        }

        if (cardData.CardID == -1) return; // Không có bài trong slot này

        CardDataSO data = CardDatabase.Instance.GetCardData(cardData.CardID);
        if (data == null) return;

        var no = _runner.Spawn(_cardPrefab, Vector3.zero, Quaternion.identity);
        CardNet card = no.GetComponent<CardNet>();

        card.OwnerID = ownerID;
        card.HandIndex = index;
        card.CardID = data.cardID;
        // Sử dụng chỉ số đã buff truyền từ Menu sang
        card.Top = cardData.Top;
        card.Right = cardData.Right;
        card.Bottom = cardData.Bottom;
        card.Left = cardData.Left;

        if (data.skill != null)
            data.skill.OnCardSpawned(card);
    }
}
