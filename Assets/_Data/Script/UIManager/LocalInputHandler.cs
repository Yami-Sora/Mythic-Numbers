using UnityEngine;
using Fusion;
// Class này xử lý logic chọn bài của người chơi Local
public class LocalInputHandler
{
    private GameManagerNet _gameManager;
    private CardNet _selectedLocalCard;

    public LocalInputHandler(GameManagerNet gm)
    {
        _gameManager = gm;
    }

    public void SelectCard(CardNet card)
    {
        // Logic kiểm tra chủ sở hữu
        if (card.OwnerID != _gameManager.GetLocalPlayerID())
        {
            GameUIManager.Instance?.ShowFloatingText("Không phải bài của bạn!", card.transform.position);
            return;
        }

        IRuleSet currentRule = _gameManager.GetCurrentRule();
        if (currentRule != null)
        {
            if (!currentRule.CanPlayCard(_gameManager, card))
            {
                // Hiển thị thông báo lỗi ngay tại vị trí lá bài
                GameUIManager.Instance?.ShowFloatingText("Đánh bài theo đúng thứ tự!", card.transform.position);
                return;
            }
        }
        // Logic Highlight
        if (_selectedLocalCard != null) _selectedLocalCard.SetHighlight(false);
        _selectedLocalCard = card;
        if (_selectedLocalCard != null) _selectedLocalCard.SetHighlight(true);
    }

    public void OnSlotClicked(int slotIndex)
    {
        if (_selectedLocalCard == null) return;

        // Kiểm tra lượt thông qua GameManager
        if (_gameManager.GetLocalPlayerID() != _gameManager.CurrentTurn) return;

        // Tắt highlight và gửi lệnh
        _selectedLocalCard.SetHighlight(false);

        // Gọi RPC bên GameManager để xử lý logic mạng
        _gameManager.RPC_PlayCard(_selectedLocalCard.Object.Id, slotIndex);

        _selectedLocalCard = null;
    }

    public void Deselect()
    {
        if (_selectedLocalCard != null) _selectedLocalCard.SetHighlight(false);
        _selectedLocalCard = null;
    }
}