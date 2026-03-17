using UnityEngine;

/// <summary>
/// Công thức ELO chuẩn.
/// K = 32 (40 cho người mới dưới 30 trận).
/// </summary>
public static class EloCalculator
{
    private const int KFactorNew = 40;
    private const int KFactorDefault = 32;
    private const int NewPlayerGameThreshold = 30;

    /// <summary>
    /// Tính ELO mới sau trận đấu.
    /// </summary>
    /// <param name="myElo">ELO hiện tại của người chơi</param>
    /// <param name="opponentElo">ELO của đối thủ</param>
    /// <param name="result">1 = thắng, 0.5 = hòa, 0 = thua</param>
    /// <param name="totalGamesPlayed">Tổng số trận đã chơi (để dùng K cao hơn cho người mới)</param>
    /// <returns>ELO mới</returns>
    public static int CalculateNewElo(int myElo, int opponentElo, float result, int totalGamesPlayed = 0)
    {
        int k = totalGamesPlayed < NewPlayerGameThreshold ? KFactorNew : KFactorDefault;
        float expectedScore = 1f / (1f + Mathf.Pow(10f, (opponentElo - myElo) / 400f));
        float delta = k * (result - expectedScore);
        int newElo = Mathf.RoundToInt(myElo + delta);
        return Mathf.Max(0, newElo);
    }

    /// <summary>
    /// Tính ELO bucket cho matchmaking (ghép phòng theo khoảng ELO).
    /// Cùng bucket = ELO gần nhau (chia 100).
    /// </summary>
    public static int GetEloBucket(int elo, int bucketSize = 100)
    {
        return Mathf.Max(0, elo / bucketSize) * bucketSize;
    }
}
