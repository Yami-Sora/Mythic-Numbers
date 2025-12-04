using UnityEngine;
using Fusion;

public abstract class BaseRule : IRuleSet
{
    public abstract string RuleName { get; }
    public abstract string RuleDescription { get; }


    public virtual void ResolveBattle(GameManagerNet gm, CardNet playedCard, int slotIndex)
    {
        int row = slotIndex / 3;
        int col = slotIndex % 3;

        CheckOneDirection(gm, playedCard, slotIndex - 3, "Bottom", row - 1, col);   // Top
        CheckOneDirection(gm, playedCard, slotIndex + 1, "Left", row, col + 1);     // Right
        CheckOneDirection(gm, playedCard, slotIndex + 3, "Top", row + 1, col);      // Bottom
        CheckOneDirection(gm, playedCard, slotIndex - 1, "Right", row, col - 1);    // Left
    }

    protected void CheckOneDirection(GameManagerNet gm, CardNet myCard, int nIdx, string enemySide, int r, int c)
    {
        // 1. Check biên
        if (nIdx < 0 || nIdx >= 9 || r < 0 || r > 2 || c < 0 || c > 2) return;

        // 2. Check bài
        NetworkId nId = gm.BoardState[nIdx];
        if (!nId.IsValid) return;

        NetworkObject obj = gm.Runner.FindObject(nId);
        if (obj == null) return;

        CardNet enemy = obj.GetComponent<CardNet>();

        // 3. Check phe (Không ăn bài đồng đội)
        if (enemy.OwnerID == myCard.OwnerID) return;

        // 4. Lấy chỉ số
        int myStat = GetStat(myCard, GetOppositeSide(enemySide));
        int enemyStat = GetStat(enemy, enemySide);

        // 5. Gọi hàm so sánh (Hàm này sẽ được override bởi các luật con)
        if (CompareStats(myStat, enemyStat))
        {
            enemy.FlipOwner();
            Debug.Log($"[{RuleName}] Lật bài! My:{myStat} vs Enemy:{enemyStat}");
        }
    }

    // Hàm abstract để các luật con tự định nghĩa cách so sánh
    protected abstract bool CompareStats(int myStat, int enemyStat);

    // Helper lấy chỉ số
    protected int GetStat(CardNet card, string side)
    {
        switch (side)
        {
            case "Top": return card.Top;
            case "Bottom": return card.Bottom;
            case "Left": return card.Left;
            case "Right": return card.Right;
            default: return 0;
        }
    }

    protected string GetOppositeSide(string side)
    {
        if (side == "Top") return "Bottom";
        if (side == "Bottom") return "Top";
        if (side == "Left") return "Right";
        if (side == "Right") return "Left";
        return "";
    }

}