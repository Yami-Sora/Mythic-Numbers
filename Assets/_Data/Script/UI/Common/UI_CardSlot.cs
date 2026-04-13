using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_CardSlot : UI_CardBase
{
    [Header("Interaction")]
    [SerializeField] private Button btnCard;

    private OwnedCard _currentCard;

    // Overload hàm Setup để nhận OwnedCard từ List
    public void Setup(OwnedCard cardInfo)
    {
        _currentCard = cardInfo;

        // Gọi thằng cha để vẽ hình và chữ
        base.Setup(cardInfo.data);

        // Nạp đạn cho nút bấm
        if (btnCard != null)
        {
            btnCard.onClick.RemoveAllListeners();
            btnCard.onClick.AddListener(OnCardClicked);
        }
    }

    private void OnCardClicked()
    {
        Debug.Log("<color=yellow>Sếp vừa chạm vào thần bài: " + _currentCard.data.cardName + "</color>");
        if (CardDetailManager.Instance != null)
        {
            CardDetailManager.Instance.OpenDetail(_currentCard);
        }

        // Chống lỗi NullReference nếu CardListManager đang bị tắt đột ngột
        if (CardListManager.Instance != null)
        {
            CardListManager.Instance.gameObject.SetActive(false);
        }
    }
}