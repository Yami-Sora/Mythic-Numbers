// --- LUẬT NORMAL (LỚN ĂN BÉ) ---
public class NormalRule : BaseRule
{
    public override string RuleName => "Normal";

    public override string RuleDescription => "Khi hai lá bài đặt cạnh nhau, lá bài có chỉ số cao hơn sẽ chiến thắng và lật ngược lá bài của đối thủ.";

    protected override bool CompareStats(int myStat, int enemyStat)
    {
        return myStat > enemyStat;
    }
}

// --- LUẬT REVERSE (BÉ ĂN LỚN) ---
public class ReverseRule : BaseRule
{
    public override string RuleName => "Reverse";

    public override string RuleDescription => "Mọi thứ bị đảo lộn! Khi hai lá bài đặt cạnh nhau, lá bài có chỉ số thấp hơn sẽ chiến thắng và lật ngược lá bài của đối thủ.";

    protected override bool CompareStats(int myStat, int enemyStat)
    {
        return myStat < enemyStat;
    }
}