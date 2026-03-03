using Fusion;
using TMPro;
using System.Collections;
using UnityEngine;

public class GameRefereeNet : NetworkBehaviour, IPlayerLeft
{
    public static GameRefereeNet Instance { get; private set; }

    [Header("UI Win/Lose")]
    private GameObject resultPanel;
    private TMP_Text resultText;
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
    }

    // Render runs regularly on all instances — use it to react to networked flag changes.
    public override void Render()
    {
        base.Render();

        // React to networked reconnect flag change and update UI
        if (_lastReconnectingState != IsOpponentReconnecting)
        {
            _lastReconnectingState = IsOpponentReconnecting;
            if (CanvasManager.Instance != null)
            {
                CanvasManager.Instance.ShowReconnectMessage(IsOpponentReconnecting, IsOpponentReconnecting ? "Đối thủ đang reconnect…" : "");
            }
        }
    }

    // --- LOGIC KIỂM TRA KẾT THÚC GAME ---
    // Hàm này được gọi từ GameManagerNet sau khi đánh xong 1 lá
    public void CheckEndGame()
    {
        // Chỉ Server (StateAuthority) mới có quyền kiểm tra và quyết định thắng thua
        if (!Object.HasStateAuthority) return;
        if (CanvasManager.Instance != null && CanvasManager.Instance.isCardFocusUIOpen) return;
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

        // Bắn pháo hiệu cho tất cả người chơi
        RPC_ShowResult(winnerID, p1Score, p2Score, "");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowResult(int winnerID, int s1, int s2, string customMessage)
    {
        // Thay vì hiện ngay, ta chạy Coroutine để chờ UI CardFocus tắt
        StartCoroutine(ShowResultSequence(winnerID, s1, s2, customMessage));
    }

    // RPC to show/hide reconnect UI on clients (best-effort)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowReconnectStatus(bool isReconnecting)
    {
        // Keep RPC for immediate effect; networked var is authoritative fallback
        if (CanvasManager.Instance != null)
        {
            if (isReconnecting)
            {
                CanvasManager.Instance.ShowReconnectMessage(true, "Đối thủ đang reconnect…");
            }
            else
            {
                CanvasManager.Instance.ShowReconnectMessage(false, "");
            }
        }
    }

    // ✅ [LOGIC QUAN TRỌNG]: Chờ isCardFocusUIOpen == false
    private IEnumerator ShowResultSequence(int winnerID, int s1, int s2, string customMessage)
    {
        Debug.Log("Game Over! Đang kiểm tra trạng thái CardFocus UI...");

        yield return new WaitForSeconds(1f);
        // Kiểm tra xem CanvasManager có tồn tại không
        if (CanvasManager.Instance != null)
        {
            // Timeout an toàn: Nếu sau 3 giây mà UI vẫn chưa tắt (do lỗi gì đó), thì cứ hiện bảng kết quả luôn
            // để tránh game bị treo vĩnh viễn.
            float timeOut = 15f;

            // Vòng lặp chờ: Chừng nào isCardFocusUIOpen còn TRUE thì còn đợi
            while (CanvasManager.Instance.isCardFocusUIOpen && timeOut > 0)
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

        // Set networked flag so all clients (and future-joining clients) know the state
        IsOpponentReconnecting = true;

        // Also notify immediately (best-effort via RPC)
        RPC_ShowReconnectStatus(true);

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

        // Clear the networked flag
        IsOpponentReconnecting = false;

        // Đồng bộ vị trí bài cho tất cả (client reconnect cần nhận state chính xác)
        if (GameManagerNet.Instance != null)
            GameManagerNet.Instance.RPC_ForceRefreshCards();

        // Direct call for Host – hide reconnect UI immediately
        if (CanvasManager.Instance != null)
            CanvasManager.Instance.ShowReconnectMessage(false, "");

        // Hide reconnect UI on clients
        RPC_ShowReconnectStatus(false);

        // Cancel TTL wait and hide UI
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

        // Ẩn reconnect UI trước khi hiện kết quả
        IsOpponentReconnecting = false;
        if (CanvasManager.Instance != null)
            CanvasManager.Instance.ShowReconnectMessage(false, "");
        RPC_ShowReconnectStatus(false);

        if (GameManagerNet.Instance != null)
        {
            int winnerID = GameManagerNet.Instance.GetLocalPlayerID();
            string msg = "Opponent Disconnected\nYou Win!";
            RPC_ShowResult(winnerID, 0, 0, msg);
        }
    }

    // Legacy interface method (kept empty; handled via HandlePlayerLeft/Joined)
    public void PlayerLeft(PlayerRef player)
    {
        // Intentionally left blank to avoid double-handling.
    }
}