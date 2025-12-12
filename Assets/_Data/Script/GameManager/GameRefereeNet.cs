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

        // ✅ [FIX QUAN TRỌNG CHO CLIENT]
        // Nếu resultPanel đang null (nghĩa là chưa được gán), tự đi tìm AppManager để xin
        // Điều này xảy ra trên máy Client vì Client không chạy qua Launcher để gọi hàm Setup
        if (resultPanel == null)
        {
            NetworkAppManager appManager = FindFirstObjectByType<NetworkAppManager>();
            if (appManager != null)
            {
                // Gọi hàm Setup bên AppManager để nhận tham chiếu UI
                appManager.SetupRefereeUI(this);
                // Debug.Log("Referee (Client): Đã tự tìm thấy UI từ AppManager.");
            }
            else
            {
                Debug.LogWarning("Referee: Không tìm thấy NetworkAppManager để lấy UI Reference! Hãy kiểm tra lại Scene.");
            }
        }
        else
        {
            // Nếu đã có (trên Host), tắt đi cho chắc chắn
            resultPanel.SetActive(false);
        }
    }

    // --- LOGIC KIỂM TRA KẾT THÚC GAME ---
    // Hàm này được gọi từ GameManagerNet sau khi đánh xong 1 lá
    public void CheckEndGame()
    {
        // Chỉ Server (StateAuthority) mới có quyền kiểm tra và quyết định thắng thua
        if (!Object.HasStateAuthority) return;
        if (CanvasManager.Instance.isCardFocusUIOpen) return;
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

    // ✅ [LOGIC QUAN TRỌNG]: Chờ isCardFocusUIOpen == false
    private IEnumerator ShowResultSequence(int winnerID, int s1, int s2, string customMessage)
    {
        Debug.Log("Game Over! Đang kiểm tra trạng thái CardFocus UI...");

        // Kiểm tra xem CanvasManager có tồn tại không
        if (CanvasManager.Instance != null)
        {
            // Timeout an toàn: Nếu sau 3 giây mà UI vẫn chưa tắt (do lỗi gì đó), thì cứ hiện bảng kết quả luôn
            // để tránh game bị treo vĩnh viễn.
            float timeOut = 3.0f;

            // Vòng lặp chờ: Chừng nào isCardFocusUIOpen còn TRUE thì còn đợi
            while (CanvasManager.Instance.isCardFocusUIOpen && timeOut > 0)
            {
                timeOut -= Time.deltaTime;
                yield return null; // Đợi 1 frame rồi check tiếp
            }
        }
        else
        {
            // Nếu không tìm thấy CanvasManager, chờ tạm 1 giây
            yield return new WaitForSeconds(1.0f);
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

    public void PlayerLeft(PlayerRef player)
    {
        // Nếu game đang diễn ra và người thoát không phải là mình
        if (player != Runner.LocalPlayer)
        {
            Debug.Log("Đối thủ đã thoát trận!");

            int winnerID = GameManagerNet.Instance.GetLocalPlayerID();

            // Hiển thị bảng kết quả: "Đối thủ đã ngắt kết nối!"
            string msg = "Opponent Disconnected!\nYou Win!";
            RPC_ShowResult(winnerID, 0, 0, msg);
        }
    }
}