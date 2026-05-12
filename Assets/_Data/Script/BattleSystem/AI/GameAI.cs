using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameAI : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private float thinkingTime = 3f; // Thời gian giả vờ suy nghĩ

    private GameManager gameManager;
    //Biến lưu trữ luồng suy nghĩ hiện tại, fix bug tự động đánh 2 lá sau khi reset
    private Coroutine currentThinkingCoroutine;
    public void Init(GameManager manager)
    {
        this.gameManager = manager;
    }
    // Hàm cưỡng chế dừng suy nghĩ
    public void StopThinking()
    {
        if (currentThinkingCoroutine != null)
        {
            StopCoroutine(currentThinkingCoroutine);
            currentThinkingCoroutine = null;
            Debug.Log("[AI] Đã bị ép buộc ngừng suy nghĩ do Reset game.");
        }
    }

    // Hàm này được GameManager gọi khi đến lượt AI
    public void StartTurn(int aiPlayerID)
    {
        // Nếu đã có coroutine suy nghĩ đang chạy, dừng nó trước khi bắt đầu cái mới
        if (currentThinkingCoroutine != null)
        {
            StopCoroutine(currentThinkingCoroutine);
            currentThinkingCoroutine = null;
        }

        currentThinkingCoroutine = StartCoroutine(ThinkAndDecide(aiPlayerID));
    }

    private IEnumerator ThinkAndDecide(int aiPlayerID)
    {
        yield return new WaitForSeconds(thinkingTime);

        // Nếu GameManager bị null hoặc lượt đã đổi -> dừng
        if (gameManager == null || gameManager.CurrentTurn != aiPlayerID)
        {
            currentThinkingCoroutine = null;
            yield break;
        }

        // --- BƯỚC 1: LẤY DỮ LIỆU ---
        // Tìm bài trên tay AI
        List<CardObj> aiHand = new List<CardObj>();
        CardObj[] allCards = FindObjectsByType<CardObj>(FindObjectsSortMode.None);
        foreach (var card in allCards)
        {
            if (card.OwnerID == aiPlayerID && card.HandIndex != -1)
            {
                aiHand.Add(card);
            }
        }
        // 1b. ✅ QUAN TRỌNG: Lọc ra các lá bài HỢP LỆ (Legal Moves)
        // Nếu có luật Order, danh sách này sẽ chỉ còn 1 lá duy nhất.
        List<CardObj> legalCards = new List<CardObj>();
        IRuleSet currentRule = gameManager.GetCurrentRule();

        foreach (var card in aiHand)
        {
            // Nếu chưa có luật (fallback) hoặc luật cho phép đánh lá này -> Thêm vào list
            if (currentRule == null || currentRule.CanPlayCard(gameManager, card))
            {
                legalCards.Add(card);
            }
        }
        // Tìm ô trống
        List<int> emptySlots = new List<int>();
        for (int i = 0; i < 9; i++)
        {
            if (gameManager.BoardState[i] == null) emptySlots.Add(i);
        }

        // Nếu không còn bài hoặc không còn chỗ -> Dừng
        if (legalCards.Count == 0 || emptySlots.Count == 0)
        {
            currentThinkingCoroutine = null; // Kết thúc
            yield break;
        }

        // --- BƯỚC 2: THUẬT TOÁN GREEDY (TÌM NƯỚC ĐI TỐT NHẤT) ---
        // ID 1 (Reverse) hoặc ID 3 (Reverse + Order) là Reverse
        bool isReverseRule = (gameManager.CurrentRuleIndex == 1 || gameManager.CurrentRuleIndex == 3);

        CardObj bestCard = legalCards[0];
        int bestSlot = emptySlots[0];
        int maxFlips = -1;

        // Duyệt qua từng lá bài đang có
        foreach (var card in legalCards)
        {
            // Ướm thử vào từng ô trống
            foreach (var slotIndex in emptySlots)
            {
                int potentialFlips = SimulateFlipCount(card, slotIndex, aiPlayerID, isReverseRule);

                // Nếu nước đi này lật được nhiều hơn -> Chọn
                // (Dùng >= để ưu tiên các nước đi sau, tạo chút ngẫu nhiên nhỏ do thứ tự duyệt)
                if (potentialFlips >= maxFlips)
                {
                    maxFlips = potentialFlips;
                    bestCard = card;
                    bestSlot = slotIndex;
                }
            }
        }


        // Trước khi gửi RPC, kiểm tra lại trạng thái: lá vẫn ở tay, ô vẫn rỗng, và vẫn là lượt AI
        if (bestCard == null || bestCard.HandIndex == -1 || gameManager.BoardState[bestSlot] != null || gameManager.CurrentTurn != aiPlayerID)
        {
            currentThinkingCoroutine = null;
            yield break;
        }

        // --- BƯỚC 3: THỰC HIỆN NƯỚC ĐI ---
        gameManager.PlayCard(bestCard, bestSlot);
        // Đánh xong thì reset biến
        currentThinkingCoroutine = null;
    }

    // --- CÁC HÀM TÍNH TOÁN GIẢ LẬP (PURE LOGIC) ---

    // Đếm số lượng bài lật được (không thay đổi game state)
    private int SimulateFlipCount(CardObj cardToPlay, int slotIndex, int ownerID, bool isReverseRule)
    {
        int flips = 0;
        int row = slotIndex / 3;
        int col = slotIndex % 3;

        flips += CheckFlipSim(slotIndex - 3, cardToPlay.Top, "Bottom", row - 1, col, ownerID, isReverseRule);   // Top
        flips += CheckFlipSim(slotIndex + 1, cardToPlay.Right, "Left", row, col + 1, ownerID, isReverseRule);    // Right
        flips += CheckFlipSim(slotIndex + 3, cardToPlay.Bottom, "Top", row + 1, col, ownerID, isReverseRule);    // Bottom
        flips += CheckFlipSim(slotIndex - 1, cardToPlay.Left, "Right", row, col - 1, ownerID, isReverseRule);    // Left

        return flips;
    }

    private int CheckFlipSim(int nIdx, int myStat, string enemySide, int r, int c, int myOwner, bool isReverseRule)
    {
        // Check biên bàn cờ
        if (nIdx < 0 || nIdx >= 9 || r < 0 || r > 2 || c < 0 || c > 2) return 0;

        // Check ô trống
        CardObj enemy = gameManager.BoardState[nIdx];
        if (enemy == null) return 0;

        if (enemy.OwnerID == myOwner) return 0; // Bài phe mình -> không lật

        // So sánh chỉ số
        int enemyStat = 0;
        if (enemySide == "Top") enemyStat = enemy.Top;
        if (enemySide == "Bottom") enemyStat = enemy.Bottom;
        if (enemySide == "Left") enemyStat = enemy.Left;
        if (enemySide == "Right") enemyStat = enemy.Right;

        if (isReverseRule)
        {
            // Luật Reverse:
            if (myStat < enemyStat) return 1;
        }
        else
        {
            // Luật Normal:
            if (myStat > enemyStat) return 1;
        }

        return 0;
    }
}