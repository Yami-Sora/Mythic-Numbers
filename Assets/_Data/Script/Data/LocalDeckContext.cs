using UnityEngine;

public struct CardDataCache
{
    public int CardID;
    public int Top;
    public int Right;
    public int Bottom;
    public int Left;
    public int StarLevel; // Thêm cấp sao
}

public static class LocalDeckContext
{
    public const int MAX_DECK_SIZE = 5;
    public static CardDataCache[] CurrentDeck = new CardDataCache[MAX_DECK_SIZE];
    public static CardDataCache[] OpponentDeck = new CardDataCache[MAX_DECK_SIZE]; // Bộ bài đối thủ
    public static string OpponentPlayFabId = ""; // Lưu ID đối thủ để trừ điểm nếu sếp thắng
    
    public static bool HasDeck = false;
    public static bool HasOpponentDeck = false;

    public static void SetDeck(OwnedCard[] deck)
    {
        for (int i = 0; i < MAX_DECK_SIZE; i++)
        {
            if (deck[i] != null && deck[i].data != null)
            {
                CurrentDeck[i] = new CardDataCache
                {
                    CardID = deck[i].data.cardID,
                    Top = deck[i].GetTotalTop(),
                    Right = deck[i].GetTotalRight(),
                    Bottom = deck[i].GetTotalBottom(),
                    Left = deck[i].GetTotalLeft(),
                    StarLevel = deck[i].starLevel
                };
            }
            else
            {
                CurrentDeck[i] = new CardDataCache { CardID = -1 };
            }
        }
        HasDeck = true;
    }

    public static void SetOpponentDeckFromSnapshots(CardSnapshot[] snapshots)
    {
        if (snapshots == null) return;
        for (int i = 0; i < MAX_DECK_SIZE; i++)
        {
            if (i < snapshots.Length && snapshots[i] != null && snapshots[i].id != -1)
            {
                OpponentDeck[i] = new CardDataCache
                {
                    CardID = snapshots[i].id,
                    Top = snapshots[i].top,
                    Right = snapshots[i].right,
                    Bottom = snapshots[i].bottom,
                    Left = snapshots[i].left,
                    StarLevel = snapshots[i].star
                };
            }
            else
            {
                OpponentDeck[i] = new CardDataCache { CardID = -1 };
            }
        }
        HasOpponentDeck = true;
    }

    public static void SetRandomOpponentDeck()
    {
        if (CardDatabase.Instance == null) return;
        for (int i = 0; i < MAX_DECK_SIZE; i++)
        {
            var card = CardDatabase.Instance.GetRandomCard();
            if (card != null)
            {
                OpponentDeck[i] = new CardDataCache
                {
                    CardID = card.cardID,
                    Top = card.top,
                    Right = card.right,
                    Bottom = card.bottom,
                    Left = card.left,
                    StarLevel = 0
                };
            }
        }
        HasOpponentDeck = true;
    }
}
