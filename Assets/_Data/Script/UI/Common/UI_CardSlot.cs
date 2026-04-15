using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_CardSlot : UI_CardBase
{
    // Thêm Sprite rỗng để gán lại khi thẻ bị tháo ra (Sếp khai báo biến này ở trên đầu class nhé)
    [Header("Empty Slot Setting")]
    public Sprite emptySlotSprite;

    [Header("Interaction")]
    [SerializeField] private Button btnCard;

    [Header("Deck Builder Identity")]
    [Tooltip("Tick vào nếu đây là 1 trong 5 ô Đội Hình bên trái")]
    public bool isDeckSlot = false;
    [Tooltip("Đánh số 0, 1, 2, 3, 4 cho 5 ô bên trái. Để -1 nếu là thẻ trong kho")]
    public int mySlotIndex = -1;


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
        // FIX BUG 1: Nếu slot rỗng (chưa có bài) -> Chặn luôn không cho click làm gì cả.
        // Cực kỳ an toàn, không sinh bug, không ảnh hưởng chế độ Edit.
        if (_currentCard == null) return;

        if (DeckManager.Instance == null) return;

        // Tình Huống 1: ĐANG Ở CHẾ ĐỘ XẾP BÀI (Edit Mode = True)
        if (DeckManager.Instance.isEditingDeck)
        {
            if (isDeckSlot)
            {
                DeckManager.Instance.UnequipCard(mySlotIndex);
            }
            else
            {
                // FIX BUG 2: Truyền NGUYÊN BẢN thẻ OwnedCard vào, KHÔNG truyền .data nữa!
                DeckManager.Instance.EquipCard(_currentCard);
            }
        }
        // Tình Huống 2: ĐANG Ở CHẾ ĐỘ XEM (Edit Mode = False)
        else
        {
            if (CardDetailManager.Instance != null)
            {
                CardDetailManager.Instance.OpenDetail(_currentCard);
            }

            if (CardListManager.Instance != null)
            {
                CardListManager.Instance.gameObject.SetActive(false);
            }
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.gameObject.SetActive(false);
            }
        }
    }

    // Thêm hàm này để "Lột sạch" hình thẻ khi ô bị rỗng
    public void ClearSlot()
    {
        _currentCard = null;

        // 1. Dùng quyền năng của thằng cha tắt hết phụ kiện đi
        ToggleContent(false);

        // 2. Trả imgCard về dạng rỗng (Viền đứt nét của sếp)
        if (imgCard != null && emptySlotSprite != null)
        {
            imgCard.sprite = emptySlotSprite;
        }
    }
}