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

    public void DealCards()
    {
        for (int i = 0; i < 5; i++)
        {
            SpawnCard(0, i);
            SpawnCard(1, i);
        }
    }

    public void SpawnCard(int ownerID, int index)
    {
        if (CardDatabase.Instance == null)
        {
            Debug.LogError("CardDatabase not found! Make sure it exists in the scene.");
            return;
        }

        CardDataSO data = CardDatabase.Instance.GetRandomCard();
        if (data == null) return;

        var no = _runner.Spawn(_cardPrefab, Vector3.zero, Quaternion.identity);
        CardNet card = no.GetComponent<CardNet>();

        card.OwnerID = ownerID;
        card.HandIndex = index;
        card.CardID = data.id;
        card.Top = data.top;
        card.Right = data.right;
        card.Bottom = data.bottom;
        card.Left = data.left;

        if (data.skill != null)
            data.skill.OnCardSpawned(card);
    }
}
