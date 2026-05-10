using TMPro;
using System.Collections;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class GameReferee : MonoBehaviour
{
    public static GameReferee Instance { get; private set; }

    [Header("UI Win/Lose")]
    private GameObject resultPanel;
    private TMP_Text resultText;

    public GameObject ResultPanel => resultPanel;
    public TMP_Text ResultText => resultText;

    private bool _isExiting = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Nếu resultPanel is null (client case), báo cho BattleFlowManager setup
        if (resultPanel == null)
        {
            BattleFlowManager appManager = FindFirstObjectByType<BattleFlowManager>();
            if (appManager != null)
            {
                appManager.SetupRefereeUI(this);
            }
            else
            {
                Debug.LogWarning("Referee: Không tìm thấy BattleFlowManager để lấy UI Reference! Hãy kiểm tra lại Scene.");
            }
        }
        else
        {
            resultPanel.SetActive(false);
        }
    }

    public void SetUIRefs(GameObject panel, TMP_Text text)
    {
        resultPanel = panel;
        resultText = text;

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);

            // Tìm nút Back trong panel
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
        if (_isExiting) return;
        _isExiting = true;

        Debug.Log("<color=cyan>[Referee] Sếp Yami thu quân về thành...</color>");
        
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }

    // --- LOGIC KIỂM TRA KẾT THÚC GAME ---
    public void CheckEndGame()
    {
        if (InGameUIManager.Instance != null && InGameUIManager.Instance.isCardFocusUIOpen) return;
        
        // Kiểm tra xem bàn cờ đã đầy chưa thông qua GameManager
        bool isFull = true;
        var board = GameManager.Instance.BoardState; 

        for (int i = 0; i < 9; i++)
        {
            if (board[i] == null) // Nếu còn ô trống
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

    private void CalculateResult()
    {
        int p1Score = 0; // Blue (ID 0)
        int p2Score = 0; // Red (ID 1)

        // Đếm điểm từ các lá đã đánh trên bàn
        var board = GameManager.Instance.BoardState;
        for (int i = 0; i < 9; i++)
        {
            if (board[i] != null)
            {
                if (board[i].OwnerID == 0) p1Score++;
                else p2Score++;
            }
        }

        // Đếm thêm lá bài còn trên tay chưa đánh (HandIndex != -1)
        // Lá trên tay luôn thuộc về chủ ban đầu của nó
        CardObj[] allCards = UnityEngine.Object.FindObjectsByType<CardObj>(FindObjectsSortMode.None);
        foreach (var card in allCards)
        {
            if (card != null && card.HandIndex != -1)
            {
                if (card.OwnerID == 0) p1Score++;
                else p2Score++;
            }
        }

        Debug.Log($"KẾT QUẢ (bàn + tay): P1 {p1Score} - P2 {p2Score}");

        // 0: P1 Thắng, 1: P2 Thắng, 2: Hòa
        int winnerID = 2;
        if (p1Score > p2Score) winnerID = 0;
        else if (p2Score > p1Score) winnerID = 1;

        StartCoroutine(ShowResultSequence(winnerID, p1Score, p2Score, ""));
    }

    // ✅ Đẩy việc hiển thị UI cho InGameUIManager tự lo liệu
    private IEnumerator ShowResultSequence(int winnerID, int s1, int s2, string customMessage)
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
        int myID = GameManager.Instance.GetLocalPlayerID(); // Luôn là 0 trong Offline

        // Xử lý hiển thị text
        if (!string.IsNullOrEmpty(customMessage))
        {
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

                // [PVE LOGIC]: CỘNG THƯỞNG VÀ TĂNG ẢI
                if (PlayFabDataManager.Instance != null && 
                    PlayFabDataManager.Instance.CurrentMode != PlayFabDataManager.GameMode.Arena)
                {
                    PlayFabDataManager.Instance.ClaimStageReward((gold, isBoss, gems) => {
                        string bonusMsg = "";
                        if (gold > 0) bonusMsg += $"\n<size=40><color=yellow>+{gold} Vàng</color></size>";
                        if (gems > 0) bonusMsg += $"\n<size=40><color=#FF00FF>+{gems} Linh Ngọc</color></size>";
                        
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
                if (shouldUpdatePlayFab)
                {
                    UpdatePlayFabStats(winnerID, myID);
                }
            });
        }
        else 
        {
            // Fallback nếu ko có UIManager
            resultText.text = message;
            resultText.color = textColor;
            resultPanel.SetActive(true);
            if (shouldUpdatePlayFab) UpdatePlayFabStats(winnerID, myID);
        }
    }

    private void UpdatePlayFabStats(int winnerID, int myID)
    {
        bool isWin = (winnerID == myID);
        bool isDraw = (winnerID == 2);

        // --- LOGIC ARENA ---
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
    }
}