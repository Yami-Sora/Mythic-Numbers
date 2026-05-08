using Fusion;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

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

            // Tìm nút Back trong panel (Sếp nhớ kéo một cái Button vào cái Panel này nhé)
            var btnBack = resultPanel.GetComponentInChildren<UnityEngine.UI.Button>(true);
            if (btnBack != null)
            {
                btnBack.onClick.RemoveAllListeners();
                btnBack.onClick.AddListener(BackToMenu);
            }
        }
    }

    public void BackToMenu()
    {
        Debug.Log("<color=cyan>[Referee] Sếp Yami thu quân về thành...</color>");
        
        // Tắt Runner trước khi load scene mới để dọn dẹp Network Objects
        if (Runner != null && Runner.IsRunning) 
        {
            Runner.Shutdown();
        }
        
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
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

        // Nếu resultPanel is null (client case), báo cho NetworkAppManager setup
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

        // Tìm tất cả bài để đếm (O(1) loop 9 thay vì FindObjectsByType)
        var board = GameManagerNet.Instance.BoardState;
        for (int i = 0; i < 9; i++)
        {
            if (board[i].IsValid)
            {
                var no = Runner.FindObject(board[i]);
                if (no != null)
                {
                    CardNet card = no.GetComponent<CardNet>();
                    if (card != null)
                    {
                        if (card.OwnerID == 0) p1Score++;
                        else p2Score++;
                    }
                }
            }
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

    // ✅ Đẩy việc hiển thị UI cho InGameUIManager tự lo liệu
    private IEnumerator ShowResultSequence(int winnerID, int s1, int s2, string customMessage,
        int p1Elo, int p2Elo, int p1Wins, int p2Wins, int p1Losses, int p2Losses, int p1Total, int p2Total)
    {
        Debug.Log("Game Over! Chuẩn bị truyền data cho InGameUIManager...");
        yield return new WaitForSeconds(1f);

        if (resultPanel == null || resultText == null)
        {
            Debug.LogError("LỖI: Result UI chưa được gán trong Referee!");
            yield break;
        }

        string message = "";
        Color textColor = Color.white;
        bool shouldUpdatePlayFab = false;
        int myID = GameManagerNet.Instance.GetLocalPlayerID();

        // Xử lý hiển thị text
        if (!string.IsNullOrEmpty(customMessage))
        {
            // Trường hợp đặc biệt (ví dụ: đối thủ thoát)
            message = customMessage;
            textColor = customMessage.Contains("Win") ? Color.green : Color.red;
        }
        else
        {
            // Trường hợp kết thúc game bình thường
            if (winnerID == 2)
            {
                message = $"Hòa\n ({s2} - {s1})";
                textColor = Color.yellow;
            }
            else if (winnerID == myID)
            {
                message = $"Chiến Thắng\n ({s2} - {s1})";
                textColor = Color.green;

                // [PVE LOGIC]: CỘNG THƯỞNG VÀ TĂNG ẢI (Chỉ dành cho chế độ Story/Dungeon)
                if (PlayFabDataManager.Instance != null && 
                    PlayFabDataManager.Instance.CurrentMode != PlayFabDataManager.GameMode.Arena)
                {
                    PlayFabDataManager.Instance.ClaimStageReward((gold, isBoss, gems) => {
                        string bonusMsg = "";
                        if (gold > 0) bonusMsg += $"\n<size=40><color=yellow>+{gold} Vàng</color></size>";
                        if (gems > 0) bonusMsg += $"\n<size=40><color=#FF00FF>+{gems} Linh Ngọc</color></size>";
                        
                        // Nếu panel đã hiện rồi thì append string vào
                        if (resultText != null) resultText.text += bonusMsg;
                    });
                }
            }
            else
            {
                message = $"Thất Bại\n ({s2} - {s1})";
                textColor = Color.red;
            }

            if (PlayFabDataManager.Instance != null && string.IsNullOrEmpty(customMessage))
            {
                shouldUpdatePlayFab = true;
            }
        }

        // Đẩy data sang UIManager kèm callback PlayFab
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.QueueResultPanel(resultPanel, resultText, message, textColor, () => 
            {
                // CALLBACK NÀY CHẠY SAU KHI UI KẾT QUẢ ĐÃ HIỂN THỊ
                if (shouldUpdatePlayFab)
                {
                    UpdatePlayFabStats(winnerID, myID, p1Elo, p2Elo, p1Total, p2Total);
                }
            });
        }
        else 
        {
            // Fallback nếu ko có UIManager
            resultText.text = message;
            resultText.color = textColor;
            resultPanel.SetActive(true);
            if (shouldUpdatePlayFab) UpdatePlayFabStats(winnerID, myID, p1Elo, p2Elo, p1Total, p2Total);
        }
    }

    private void UpdatePlayFabStats(int winnerID, int myID, int p1Elo, int p2Elo, int p1Total, int p2Total)
    {
        bool isWin = (winnerID == myID);
        bool isDraw = (winnerID == 2);

        // --- LOGIC ARENA MỚI ---
        if (PlayFabDataManager.Instance.CurrentMode == PlayFabDataManager.GameMode.Arena)
        {
            if (isWin)
            {
                int currentElo = PlayFabDataManager.Instance.Elo;
                int newElo = currentElo + 10;
                int newWins = PlayFabDataManager.Instance.Wins + 1;
                int newTotal = PlayFabDataManager.Instance.TotalGames + 1;

                GameServices.Instance.PlayerData.UpdateEloAfterMatchAsync(
                    newElo, newWins, PlayFabDataManager.Instance.Losses, newTotal,
                    () => {
                        Debug.Log($"[Arena] Đã cộng 10 điểm! Điểm mới: {newElo}");
                        PlayFabDataManager.Instance.Elo = newElo;
                        PlayFabDataManager.Instance.Wins = newWins;
                        PlayFabDataManager.Instance.TotalGames = newTotal;

                        if (!string.IsNullOrEmpty(LocalDeckContext.OpponentPlayFabId))
                        {
                            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest {
                                FunctionName = "SubtractEloFromOpponent",
                                FunctionParameter = new { opponentId = LocalDeckContext.OpponentPlayFabId }
                            }, result => {
                                Debug.Log("<color=red>[Arena]</color> Đã trừ 8 điểm của đối thủ thành công!");
                                LocalDeckContext.OpponentPlayFabId = ""; 
                            }, error => {
                                Debug.LogWarning("[Arena] Lỗi khi trừ điểm đối thủ: " + error.ErrorMessage);
                            });
                        }
                    },
                    err => Debug.LogWarning($"[Arena] Lỗi cập nhật điểm: {err}"));
            }
            else if (!isDraw) // Thua thì không đổi điểm, chỉ tăng số trận
            {
                    int newLosses = PlayFabDataManager.Instance.Losses + 1;
                    int newTotal = PlayFabDataManager.Instance.TotalGames + 1;

                    GameServices.Instance.PlayerData.UpdateEloAfterMatchAsync(
                    PlayFabDataManager.Instance.Elo, PlayFabDataManager.Instance.Wins, newLosses, newTotal,
                    () => {
                        Debug.Log("[Arena] Thua trận, điểm không đổi.");
                        PlayFabDataManager.Instance.Losses = newLosses;
                        PlayFabDataManager.Instance.TotalGames = newTotal;
                    },
                    null);
            }
        }
        // --- LOGIC PVP ONLINE ---
        else if (Runner.GameMode != GameMode.Single && GameServices.Instance?.PlayerData != null)
        {
            int myElo = myID == 0 ? p1Elo : p2Elo;
            int oppElo = myID == 0 ? p2Elo : p1Elo;
            float result = isDraw ? 0.5f : (isWin ? 1f : 0f);
            
            int newElo = EloCalculator.CalculateNewElo(myElo, oppElo, result, (myID == 0 ? p1Total : p2Total));
            // (giữ nguyên logic Elo cũ cho PvP Realtime nếu cần)
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