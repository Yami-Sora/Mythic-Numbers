using UnityEngine;
using UnityEngine.InputSystem;
// Class này xử lý logic chọn bài của người chơi Local
public class LocalInputHandler
{
    private GameManagerNet _gameManager;
    private CardNet _selectedLocalCard;

    // --- Variables for Long Press Logic ---
    private CardNet _currentPressCard;
    private float _pressStartTime;
    private bool _isPressing;
    private bool _isLongPressTriggered;
    private const float LONG_PRESS_DURATION = 0.4f; // Thời gian giữ để hiện Info (0.4 giây)

    public LocalInputHandler(GameManagerNet gm)
    {
        _gameManager = gm;
    }

    // --- POINTER EVENTS (Xử lý Click & Hold) ---
    public void OnPointerDown(CardNet card)
    {
        _isPressing = true;
        _isLongPressTriggered = false;
        _pressStartTime = Time.time;
        _currentPressCard = card;
    }

    public void OnPointerUp(CardNet card)
    {
        if (_isLongPressTriggered) {}
        else
        {
            // Nếu chưa kích hoạt xem Info (bấm nhanh) -> Xử lý là CLICK CHỌN BÀI
            if (_isPressing && _currentPressCard == card)
            {
                SelectCard(card);
            }
        }

        ResetPressState();
    }

    public void OnPointerExit(CardNet card)
    {
        ResetPressState();
    }

    // Hàm này được gọi mỗi frame từ GameManagerNet.Render()
    public void Update()
    {
        if (_isPressing && !_isLongPressTriggered)
        {
            // Kiểm tra thời gian giữ
            if (Time.time - _pressStartTime >= LONG_PRESS_DURATION)
            {
                _isLongPressTriggered = true;

                // HIỆN POPUP INFO
                if (_currentPressCard != null)
                {
                    CanvasManager.Instance?.ShowCardFocus(_currentPressCard);
                }
            }
        }
    }

    private void ResetPressState()
    {
        _isPressing = false;
        _isLongPressTriggered = false;
        _currentPressCard = null;
    }

    // --- GAMEPLAY LOGIC (Chọn bài để đánh) ---
    public void SelectCard(CardNet card)
    {
        // Logic kiểm tra chủ sở hữu
        if (card.OwnerID != _gameManager.GetLocalPlayerID())
        {
            GameUIManager.Instance?.ShowFloatingText("Không phải bài của bạn!", card.transform.position);
            return;
        }
        // Bài trên bàn không thể chọn lại để đánh (chỉ xem info)
        if (card.HandIndex == -1)
        {
            return;
        }

        IRuleSet currentRule = _gameManager.GetCurrentRule();
        if (currentRule != null)
        {
            if (!currentRule.CanPlayCard(_gameManager, card))
            {
                GameUIManager.Instance?.ShowFloatingText("Đánh bài theo đúng thứ tự!", card.transform.position);
                return;
            }
        }

        // Logic Highlight
        if (_selectedLocalCard != null) _selectedLocalCard.SetHighlight(false);

        // Nếu click lại chính bài đang chọn -> Bỏ chọn
        if (_selectedLocalCard == card)
        {
            _selectedLocalCard = null;
        }
        else
        {
            _selectedLocalCard = card;
            _selectedLocalCard.SetHighlight(true);
        }
    }

    public void OnSlotClicked(int slotIndex)
    {
        if (_selectedLocalCard == null) return;

        if (_gameManager.GetLocalPlayerID() != _gameManager.CurrentTurn) 
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            GameUIManager.Instance?.ShowFloatingText("Chưa đến lượt bạn!", mousePos);
            return; 
        };

        _selectedLocalCard.SetHighlight(false);

        // Gọi RPC bên GameManager để xử lý logic mạng
        _gameManager.RPC_PlayCard(_selectedLocalCard.Object.Id, slotIndex);

        // Gọi UI Manager để hiển thị panel phóng to lá bài vừa đánh
        CanvasManager.Instance?.ShowCardFocus(_selectedLocalCard);

        _selectedLocalCard = null;
    }

    public void Deselect()
    {
        if (_selectedLocalCard != null) _selectedLocalCard.SetHighlight(false);
        _selectedLocalCard = null;
    }
}