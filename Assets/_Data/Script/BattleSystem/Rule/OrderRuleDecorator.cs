using UnityEngine;

// Concrete Decorator: Thêm hành vi "Bắt buộc đánh theo thứ tự"
public class OrderRuleDecorator : RuleDecorator
{
    public OrderRuleDecorator(IRuleSet rule) : base(rule) { }

    // Ghi đè tên: Thêm chữ "Order" vào tên luật gốc
    public override string RuleName => "Order Rule";

    // Ghi đè mô tả: Thêm dòng giải thích về Order
    public override string RuleDescription => "Bạn buộc phải đánh quân bài theo thứ tự được sắp xếp.";

    public override bool CanPlayCard(GameManager gm, CardObj cardToPlay)
    {
        if (!CheckOrderCondition(cardToPlay))
        {
            return false; // Chặn ngay tại lớp vỏ này
        }

        //Nếu đúng thứ tự, gọi tiếp vào trong để luật gốc kiểm tra
        return base.CanPlayCard(gm, cardToPlay);
    }

    private bool CheckOrderCondition(CardObj cardToPlay)
    {
        // Lấy danh sách bài trên tay từ GameManager dựa trên OwnerID
        var hand = (cardToPlay.OwnerID == 0) ? 
            GameManager.Instance.P1Hand : 
            GameManager.Instance.P2Hand;

        if (hand == null || hand.Count == 0) return true;

        int minIndex = 100;
        foreach (var card in hand)
        {
            if (card.HandIndex != -1)
            {
                if (card.HandIndex < minIndex) minIndex = card.HandIndex;
            }
        }

        // Nếu lá định đánh có HandIndex nhỏ nhất trong danh sách bài trên tay -> Hợp lệ
        return cardToPlay.HandIndex == minIndex;
    }
}
