using Fusion;
using TMPro;
using UnityEngine;

public class GameRefereeNet : NetworkBehaviour
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

        // Đảm bảo lúc đầu nó tắt đi, CHỈ CHẠY MỘT LẦN KHI GÁN
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
        RPC_ShowResult(winnerID, p1Score, p2Score);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowResult(int winnerID, int s1, int s2)
    {
        Debug.Log($"RPC_ShowResult Đã nhận lệnh! Winner: {winnerID}");
        // Kiểm tra null trước khi dùng để tránh lỗi
        // Kiểm tra null và báo lỗi nếu thiếu UI
        if (resultPanel == null)
        {
            Debug.LogError("LỖI: resultPanel đang bị NULL! Chưa được gán từ NetworkAppManager.");
            return;
        }
        if (resultText == null)
        {
            Debug.LogError("LỖI: resultText đang bị NULL!");
            return;
        }

        resultPanel.SetActive(true); // Bật Panel lên

        // Lấy ID người chơi hiện tại từ GameManager để hiển thị text cho đúng
        // (Lưu ý: Hàm GetLocalPlayerID cần phải đúng logic như bài trước đã bàn)
        int myID = GameManagerNet.Instance.GetLocalPlayerID();

        string message = "";
        if (winnerID == 2)
        {
            message = $"Draw\n ({s2} - {s1})";
            if (resultText) resultText.color = Color.yellow;
        }
        else if (winnerID == myID)
        {
            message = $"You Win\n ({s2} - {s1})";
            if (resultText) resultText.color = Color.green;
        }
        else
        {
            message = $"You Lose\n ({s2} - {s1})";
            if (resultText) resultText.color = Color.red;
        }

        if (resultText) resultText.text = message;
    }
}