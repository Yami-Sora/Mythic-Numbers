using UnityEngine;

public class LauncherSessionSettings : MonoBehaviour
{
    // TTL in seconds for waiting reconnects. Set by NetworkLauncher before StartGame.
    [Tooltip("Seconds to wait for a reconnect before treating player as permanently disconnected.")]
    public int PlayerTtl = 90;
}