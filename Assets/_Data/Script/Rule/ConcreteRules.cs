// --- LUẬT NORMAL (LỚN ĂN BÉ) ---
public class NormalRule : BaseRule
{
    public override string RuleName => "Normal";

    protected override bool CompareStats(int myStat, int enemyStat)
    {
        return myStat > enemyStat;
    }
}

// --- LUẬT REVERSE (BÉ ĂN LỚN) ---
public class ReverseRule : BaseRule
{
    public override string RuleName => "Reverse";

    protected override bool CompareStats(int myStat, int enemyStat)
    {
        return myStat < enemyStat;
    }
}