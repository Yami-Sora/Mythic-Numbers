using UnityEngine;

/// <summary>
/// Chịu trách nhiệm khởi tạo bộ bài cho người chơi và AI (hoặc đối thủ Arena) trong chế độ Single Player.
/// Giảm tải logic khởi tạo cho GameManager.
/// </summary>
public static class SinglePlayerDeckBuilder
{
    public static void BuildDecks(out CardDataCache[] p1Deck, out CardDataCache[] p2Deck)
    {
        p1Deck = new CardDataCache[5];
        for (int i = 0; i < 5; i++) p1Deck[i] = LocalDeckContext.CurrentDeck[i];

        p2Deck = new CardDataCache[5];

        // --- [ARENA CHECK] ---
        if (LocalDeckContext.HasOpponentDeck)
        {
            Debug.Log("<color=orange>[Battle] Trận đấu Arena: Sử dụng bộ bài Snapshot của đối thủ!</color>");
            for (int i = 0; i < 5; i++) p2Deck[i] = LocalDeckContext.OpponentDeck[i];
            LocalDeckContext.HasOpponentDeck = false;
        }
        else
        {
            // --- [AI SCALING PVE] ---
            int stage = 1;
            float powerStep = 0.1f;

            if (PlayFabDataManager.Instance != null)
            {
                if (PlayFabDataManager.Instance.CurrentMode == PlayFabDataManager.GameMode.GoldDungeon) { stage = PlayFabDataManager.Instance.GoldDungeonStage; powerStep = 0.2f; }
                else if (PlayFabDataManager.Instance.CurrentMode == PlayFabDataManager.GameMode.LNDungeon) { stage = PlayFabDataManager.Instance.LNDungeonStage; powerStep = 0.2f; }
                else if (PlayFabDataManager.Instance.CurrentMode == PlayFabDataManager.GameMode.GemMine) { stage = PlayFabDataManager.Instance.GemMineStage; powerStep = 0.2f; }
                else { stage = PlayFabDataManager.Instance.CurrentStage; powerStep = 0.1f; }
            }

            float multiplier = 1.0f + (stage - 1) * powerStep;

            for (int i = 0; i < 5; i++)
            {
                var rCard = CardDatabase.Instance.GetRandomCard();
                if (rCard != null)
                {
                    p2Deck[i] = new CardDataCache
                    {
                        CardID = rCard.cardID,
                        Top = Mathf.RoundToInt(rCard.top * multiplier),
                        Right = Mathf.RoundToInt(rCard.right * multiplier),
                        Bottom = Mathf.RoundToInt(rCard.bottom * multiplier),
                        Left = Mathf.RoundToInt(rCard.left * multiplier),
                        StarLevel = 0
                    };
                }
            }
        }

        // --- [SHUFFLE DECKS] ---
        // Xáo bài để mỗi trận đều mới mẻ
        Shuffle(p1Deck);
        Shuffle(p2Deck);
    }

    private static void Shuffle(CardDataCache[] deck)
    {
        for (int i = deck.Length - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            CardDataCache temp = deck[i];
            deck[i] = deck[rnd];
            deck[rnd] = temp;
        }
    }
}
