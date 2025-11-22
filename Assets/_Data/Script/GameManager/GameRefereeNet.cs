using Fusion;
using TMPro;
using UnityEngine;

public class GameRefereeNet : NetworkBehaviour
{
    public static GameRefereeNet Instance;

    [Header("UI Win/Lose")]
    public GameObject resultPanel; // Kéo Panel kết quả vào đây
    public TMP_Text resultText;    // Kéo Text kết quả vào đây

    private void Awake()
    {
        Instance = this;
    }
    public override void Spawned()
    {
        // --- TỰ ĐỘNG TÌM UI ---
        // Tìm object có tên là "ResultPanel" đang nằm trong Scene
        GameObject panelObj = GameObject.Find("ResultPanel");

        if (panelObj != null)
        {
            resultPanel = panelObj;
            // Tìm component TextMeshPro nằm con của Panel
            resultText = resultPanel.GetComponentInChildren<TMP_Text>();

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

        string msg = "";
        if (winnerID == 2)
        {
            msg = $"HÒA!  \n ({s2} - {s1})";
            if (resultText) resultText.color = Color.yellow;
        }
        else if (winnerID == myID)
        {
            msg = $"CHIẾN THẮNG!  \n ({s2} - {s1})";
            if (resultText) resultText.color = Color.green;
        }
        else
        {
            msg = $"THẤT BẠI  \n ({s2} - {s1})";
            if (resultText) resultText.color = Color.red;
        }

        if (resultText) resultText.text = msg;
    }
}