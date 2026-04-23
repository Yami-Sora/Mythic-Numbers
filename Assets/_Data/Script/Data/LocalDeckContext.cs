using UnityEngine;

public struct CardDataCache
{
    public int CardID;
    public int Top;
    public int Right;
    public int Bottom;
    public int Left;
}

public static class LocalDeckContext
{
    public const int MAX_DECK_SIZE = 5;
    public static CardDataCache[] CurrentDeck = new CardDataCache[MAX_DECK_SIZE];
    public static bool HasDeck = false;

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
                    Left = deck[i].GetTotalLeft()
                };
            }
            else
            {
                CurrentDeck[i] = new CardDataCache { CardID = -1 }; // Empty slot
            }
        }
        HasDeck = true;
    }
}
