using UnityEngine;

/// <summary>
/// Dữ liệu người chơi local dùng khi vào match (ELO, stats).
/// Được set bởi MatchmakingService trước StartGame.
/// </summary>
public class MatchPlayerData
{
    public string PlayFabId;
    public int Elo;
    public int Wins;
    public int Losses;
    public int TotalGames;
}

public class LauncherSessionSettings : MonoBehaviour
{
    // TTL in seconds for waiting reconnects. Set by NetworkLauncher before StartGame.
    [Tooltip("Seconds to wait for a reconnect before treating player as permanently disconnected.")]
    public int PlayerTtl = 90;

    /// <summary>
    /// Dữ liệu người chơi local khi vào match (set trước StartGame).
    /// </summary>
    public MatchPlayerData LocalPlayerData { get; set; }
}