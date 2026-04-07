using Fusion;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRefereeNet : NetworkBehaviour, IPlayerLeft
{
    public static GameRefereeNet Instance { get; private set; }

    public struct PlayerMatchInfo
    {
        public string PlayFabId;
        public int Elo, Wins, Losses, TotalGames;
    }

    [Header("UI Win/Lose")]
    private GameObject resultPanel;
    private TMP_Text resultText;

    // Host only: map PlayerRef (0=P1, 1=P2) to PlayFabId + ELO stats
    private readonly Dictionary<int, PlayerMatchInfo> _playerInfoMap = new Dictionary<int, PlayerMatchInfo>();
    public GameObject ResultPanel => resultPanel;
    public TMP_Text ResultText => resultText;

    // Reconnect wait coroutine handle
    private Coroutine _reconnectCoroutine;

    // TTL used when waiting for reconnect (server side). Populated from SessionSettings if present.
    private int _playerTtl = 90;

    // Networked flag so clients reliably receive reconnect state even if they miss RPCs.
    [Networked]
    public bool IsOpponentReconnecting { get; set; }

    // Local cached state used to detect changes in Render()
    private bool _lastReconnectingState = false;

    public void SetUIRefs(GameObject panel, TMP_Text text)
    {
        resultPanel = panel;
        resultText = text;

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }
    public override void Spawned()
    {
        if (Instance != null && Instance != this)
        {
            Runner.Despawn(Object);
            return;
        }
        Instance = this;

        // Read TTL from SessionSettings attached to the NetworkRunner GameObject (if any)
        LauncherSessionSettings settings = FindFirstObjectByType<LauncherSessionSettings>();
        if (settings != null)
        {
            _playerTtl = Mathf.Clamp(settings.PlayerTtl, 60, 120);
            Debug.Log($"[Referee] Using PlayerTtl = {_playerTtl}s");
        }
        else
        {
            Debug.LogWarning("[Referee] SessionSettings not found, using default TTL.");
        }

        // If resultPanel is null (client case), try to get it from NetworkAppManager
        if (resultPanel == null)
        {
            NetworkAppManager appManager = FindFirstObjectByType<NetworkAppManager>();
            if (appManager != null)
            {
                appManager.SetupRefereeUI(this);
            }
            else
            {
                Debug.LogWarning("Referee: Không tìm thấy NetworkAppManager để lấy UI Reference! Hãy kiểm tra lại Scene.");
            }
        }
        else
        {
            resultPanel.SetActive(false);
        }

        // Đăng ký thông tin người chơi: Host tự đăng ký, Client gửi RPC tới Host
        RegisterLocalPlayerIfNeeded();
        TrySendRegisterRpc();
    }

    private void TrySendRegisterRpc()
    {
        if (Object.HasStateAuthority) return; // Host đã tự đăng ký
        var settings = FindFirstObjectByType<LauncherSessionSettings>();
        if (settings?.LocalPlayerData == null) return;

        RPC_RegisterPlayer(
            settings.LocalPlayerData.PlayFabId ?? "",
            settings.LocalPlayerData.Elo,
            settings.LocalPlayerData.Wins,
            settings.LocalPlayerData.Losses,
            settings.LocalPlayerData.TotalGames);
    }

    private void RegisterLocalPlayerIfNeeded()
    {
        if (!Object.HasStateAuthority) return; // Chỉ Host lưu
        var settings = FindFirstObjectByType<LauncherSessionSettings>();
        if (settings?.LocalPlayerData == null) return;

        int myId = Runner.IsServer ? 0 : 1;
        if (_playerInfoMap.ContainsKey(myId)) return;

        _playerInfoMap[myId] = new PlayerMatchInfo
        {
            PlayFabId = settings.LocalPlayerData.PlayFabId ?? "",
            Elo = settings.LocalPlayerData.Elo,
            Wins = settings.LocalPlayerData.Wins,
            Losses = settings.LocalPlayerData.Losses,
            TotalGames = settings.LocalPlayerData.TotalGames
        };
        Debug.Log($"[Referee] Host registered as P{myId} ELO={settings.LocalPlayerData.Elo}");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RegisterPlayer(string playFabId, int elo, int wins, int losses, int totalGames, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;
        // Client gửi RPC = player 1 (Host = 0)
        int playerId = info.Source == Runner.LocalPlayer ? 0 : 1;
        if (_playerInfoMap.ContainsKey(playerId)) return;

        _playerInfoMap[playerId] = new PlayerMatchInfo
        {
            PlayFabId = playFabId ?? "",
            Elo = elo,
            Wins = wins,
            Losses = losses,
            TotalGames = totalGames
        };
        Debug.Log($"[Referee] P{playerId} registered ELO={elo}");
    }

    // Render runs regularly on all instances — use it to react to networked flag changes.
    public override void Render()
    {
        base.Render();

        // React to networked reconnect flag change and update UI
        if (_lastReconnectingState != IsOpponentReconnecting)
        {
            _lastReconnectingState = IsOpponentReconnecting;
            if (InGameUIManager.Instance != null)
            {
                InGameUIManager.Instance.ShowReconnectMessage(IsOpponentReconnecting, IsOpponentReconnecting ? "Đợi đối thủ \nkết nối lại…" : "");
            }
        }
    }

    // --- LOGIC KIỂM TRA KẾT THÚC GAME ---
    // Hàm này được gọi từ GameManagerNet sau khi đánh xong 1 lá
    public void CheckEndGame()
    {
        // Chỉ Server (StateAuthority) mới có quyền kiểm tra và quyết định thắng thua
        if (!Object.HasStateAuthority) return;
        if (InGameUIManager.Instance != null && InGameUIManager.Instance.isCardFocusUIOpen) return;
        // Kiểm tra xem bàn cờ đã đầy chưa thông qua GameManager
        bool isFull = true;
        var board = GameManagerNet.Instance.BoardState; // Truy cập dữ liệu từ Manager

        for (int i = 0; i < 9; i++)
        {
            if (!board[i].IsValid) // Nếu còn ô trống
            {
                isFull = false;
                break;
            }
        }

        if (isFull)
        {
            CalculateResult();
        }
    }

    void CalculateResult()
    {
        int p1Score = 0; // Blue (ID 0)
        int p2Score = 0; // Red (ID 1)

        // Tìm tất cả bài để đếm
        // FindObjectsSortMode.None: Tìm thôi, không cần sắp xếp thứ tự -> Nhanh hơn nhiều
        CardNet[] allCards = FindObjectsByType<CardNet>(FindObjectsSortMode.None);

        foreach (CardNet card in allCards)
        {
            if (card.OwnerID == 0) p1Score++;
            else p2Score++;
        }

        Debug.Log($"KẾT QUẢ: P1 {p1Score} - P2 {p2Score}");

        // 0: P1 Thắng, 1: P2 Thắng, 2: Hòa
        int winnerID = 2;
        if (p1Score > p2Score) winnerID = 0;
        else if (p2Score > p1Score) winnerID = 1;

        // Lấy ELO để gửi kèm (nếu có) - mỗi client tự cập nhật ELO
        int p1Elo = 0, p2Elo = 0, p1Wins = 0, p2Wins = 0, p1Losses = 0, p2Losses = 0, p1Total = 0, p2Total = 0;
        if (_playerInfoMap.TryGetValue(0, out var p1))
        {
            p1Elo = p1.Elo; p1Wins = p1.Wins; p1Losses = p1.Losses; p1Total = p1.TotalGames;
        }
        if (_playerInfoMap.TryGetValue(1, out var p2))
        {
            p2Elo = p2.Elo; p2Wins = p2.Wins; p2Losses = p2.Losses; p2Total = p2.TotalGames;
        }

        RPC_ShowResult(winnerID, p1Score, p2Score, "", p1Elo, p2Elo, p1Wins, p2Wins, p1Losses, p2Losses, p1Total, p2Total);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowResult(int winnerID, int s1, int s2, string customMessage,
        int p1Elo, int p2Elo, int p1Wins, int p2Wins, int p1Losses, int p2Losses, int p1Total, int p2Total)
    {
        StartCoroutine(ShowResultSequence(winnerID, s1, s2, customMessage,
            p1Elo, p2Elo, p1Wins, p2Wins, p1Losses, p2Losses, p1Total, p2Total));
    }

    // ✅ [LOGIC QUAN TRỌNG]: Chờ isCardFocusUIOpen == false, rồi hiện kết quả và cập nhật ELO
    private IEnumerator ShowResultSequence(int winnerID, int s1, int s2, string customMessage,
        int p1Elo, int p2Elo, int p1Wins, int p2Wins, int p1Losses, int p2Losses, int p1Total, int p2Total)
    {
        Debug.Log("Game Over! Đang kiểm tra trạng thái CardFocus UI...");

        yield return new WaitForSeconds(1f);
        // Kiểm tra xem CanvasManager có tồn tại không
        if (InGameUIManager.Instance != null)
        {
            // Timeout an toàn: Nếu sau 3 giây mà UI vẫn chưa tắt (do lỗi gì đó), thì cứ hiện bảng kết quả luôn
            // để tránh game bị treo vĩnh viễn.
            float timeOut = 15f;

            // Vòng lặp chờ: Chừng nào isCardFocusUIOpen còn TRUE thì còn đợi
            while (InGameUIManager.Instance.isCardFocusUIOpen && timeOut > 0)
            {
                timeOut -= Time.deltaTime;
                yield return null; // Đợi 1 frame rồi check tiếp
            }
        }
        else
        {
            // Nếu không tìm thấy CanvasManager, chờ tạm 3 giây
            yield return new WaitForSeconds(3.0f);
        }

        // --- SAU KHI ĐÃ CHỜ XONG ---

        if (resultPanel == null || resultText == null)
        {
            Debug.LogError("LỖI: Result UI chưa được gán!");
            yield break;
        }

        resultPanel.SetActive(true); // BẬT BẢNG KẾT QUẢ

        // Xử lý hiển thị text
        if (!string.IsNullOrEmpty(customMessage))
        {
            // Trường hợp đặc biệt (ví dụ: đối thủ thoát)
            resultText.text = customMessage;
            if (customMessage.Contains("Win")) resultText.color = Color.green;
            else resultText.color = Color.red;
        }
        else
        {
            // Trường hợp kết thúc game bình thường
            int myID = GameManagerNet.Instance.GetLocalPlayerID();
            string message = "";

            if (winnerID == 2)
            {
                message = $"Draw\n ({s2} - {s1})";
                resultText.color = Color.yellow;
            }
            else if (winnerID == myID)
            {
                message = $"You Win\n ({s2} - {s1})";
                resultText.color = Color.green;
            }
            else
            {
                message = $"You Lose\n ({s2} - {s1})";
                resultText.color = Color.red;
            }

            resultText.text = message;

            // Cập nhật ELO lên backend (chỉ online, có PlayFab)
            if (Runner.GameMode != GameMode.Single && GameServices.Instance?.PlayerData != null && string.IsNullOrEmpty(customMessage))
            {
                int myId = GameManagerNet.Instance.GetLocalPlayerID();
                int myElo = myId == 0 ? p1Elo : p2Elo;
                int oppElo = myId == 0 ? p2Elo : p1Elo;
                int myWins = myId == 0 ? p1Wins : p2Wins;
                int myLosses = myId == 0 ? p1Losses : p2Losses;
                int myTotal = myId == 0 ? p1Total : p2Total;

                float result = winnerID == 2 ? 0.5f : (winnerID == myId ? 1f : 0f);
                int newElo = EloCalculator.CalculateNewElo(myElo, oppElo, result, myTotal);
                int newWins = myWins + (winnerID == myId ? 1 : 0);
                int newLosses = myLosses + (winnerID != 2 && winnerID != myId ? 1 : 0);
                int newTotal = myTotal + 1;

                GameServices.Instance.PlayerData.UpdateEloAfterMatchAsync(
                    newElo, newWins, newLosses, newTotal,
                    () => Debug.Log($"[Referee] ELO updated: {myElo} -> {newElo}"),
                    err => Debug.LogWarning($"[Referee] ELO update failed: {err}"));
            }
        }
    }

    // Called by ConnectionHandler when a player disconnects
    public void HandlePlayerLeft(PlayerRef player)
    {
        // Server (StateAuthority) decides how to handle temporary disconnects
        if (!Object.HasStateAuthority) return;

        // If the disconnected player is not the opponent (ignore local)
        if (player == Runner.LocalPlayer) return;

        // Ván đã kết thúc (đã hiện win/lose) – không hiển thị "đợi reconnect"
        if (resultPanel != null && resultPanel.activeInHierarchy)
        {
            return;
        }

        Debug.Log("Opponent disconnected – starting TTL wait for possible reconnect.");

        // Set networked flag – Render() sẽ cập nhật UI trên tất cả clients
        IsOpponentReconnecting = true;

        if (_reconnectCoroutine != null)
        {
            StopCoroutine(_reconnectCoroutine);
            _reconnectCoroutine = null;
        }
        _reconnectCoroutine = StartCoroutine(ReconnectTimeoutCoroutine(_playerTtl, player));
    }

    // Called by ConnectionHandler when a player joins (including reconnects)
    public void HandlePlayerJoined(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;

        Debug.Log($"Player joined/reconnected: {player}");

        // Clear the networked flag – Render() sẽ ẩn UI reconnect
        IsOpponentReconnecting = false;

        // Đồng bộ vị trí bài cho tất cả (client reconnect cần nhận state chính xác)
        if (GameManagerNet.Instance != null)
            GameManagerNet.Instance.RPC_ForceRefreshCards();

        // Cancel TTL wait
        if (_reconnectCoroutine != null)
        {
            StopCoroutine(_reconnectCoroutine);
            _reconnectCoroutine = null;
        }
    }

    private IEnumerator ReconnectTimeoutCoroutine(int ttlSeconds, PlayerRef disconnectedPlayer)
    {
        float timer = ttlSeconds;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        Debug.Log("Reconnect TTL expired. Treating opponent as permanently disconnected.");

        IsOpponentReconnecting = false; // Render() sẽ ẩn UI reconnect

        if (GameManagerNet.Instance != null)
        {
            int winnerID = GameManagerNet.Instance.GetLocalPlayerID();
            string msg = "Opponent Disconnected\nYou Win!";
            RPC_ShowResult(winnerID, 0, 0, msg, 0, 0, 0, 0, 0, 0, 0, 0);
        }
    }

    // Legacy interface method (kept empty; handled via HandlePlayerLeft/Joined)
    public void PlayerLeft(PlayerRef player)
    {
        // Intentionally left blank to avoid double-handling.
    }
}