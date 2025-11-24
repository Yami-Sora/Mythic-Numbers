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

        if (resultPanel != null)
        {
            // Đảm bảo lúc đầu nó tắt đi
            resultPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Không tìm thấy 'ResultPanel'! Hãy kiểm tra lại tên trong Hierarchy.");
        }
    }

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
        // Kiểm tra null trước khi dùng để tránh lỗi
        if (resultPanel == null || resultText == null) return;

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