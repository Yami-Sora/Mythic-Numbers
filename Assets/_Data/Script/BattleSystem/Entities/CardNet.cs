using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardNet : NetworkBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI txtTop, txtRight, txtBottom, txtLeft;
    [SerializeField] private GameObject highlightObj;

    [Networked] public int Top { get; set; }
    [Networked] public int Right { get; set; }
    [Networked] public int Bottom { get; set; }
    [Networked] public int Left { get; set; }
    [Networked] public int OwnerID { get; set; }
    [Networked] public int HandIndex { get; set; }
    [Networked] public int CardID { get; set; }

    // --- CÁC BIẾN NETWORK CHO SKILL ---
    [Networked] public NetworkBool IsInvincible { get; set; } // Trạng thái Vô Địch
    [Networked] public int InvincibleDuration { get; set; }   // Đếm số bán lượt hiệu lực

    public BaseSkillSO CurrentSkill { get; private set; }

    private ChangeDetector _changes;
    private bool _isInitialized = false;
    private readonly Color colorP1 = new Color(0.2f, 0.4f, 1f);
    private readonly Color colorP2 = new Color(1f, 0.3f, 0.3f);
    private readonly Color colorInvincible = new Color(1f, 0.84f, 0f);

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

        // Fail-safe: Gán tạm vào Canvas nếu chưa tìm thấy vị trí chính xác
        transform.localScale = Vector3.one;
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            transform.SetParent(canvas.transform, false);
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }

        RefreshState();
    }

    public override void Render()
    {
        if (!_isInitialized) RefreshState();

        foreach (var change in _changes.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(CardID): // Nếu ID thay đổi -> Load lại hình ảnh
                    LoadVisualsFromID();
                    break;
                case nameof(Top):
                case nameof(Right):
                case nameof(Bottom):
                case nameof(Left):
                    UpdateStatTexts();
                    break;
                case nameof(OwnerID):
                case nameof(HandIndex):
                    RefreshState();
                    break;
                case nameof(IsInvincible): // Render hiệu ứng khi trạng thái Vô Địch thay đổi
                    UpdateInvincibleVisuals();
                    break;
            }
        }
    }

    public void RefreshState()
    {
        if (GameManagerNet.Instance == null || InGameUIManager.Instance == null) 
        { 
            Debug.LogWarning("GameManagerNet or GameUIManager is not ready yet.");
            _isInitialized = false;
            return; 
        }
        LoadVisualsFromID();
        UpdateStatTexts();
        RefreshParentPosition();
        UpdateBackgroundColor();
        UpdateInvincibleVisuals();
        _isInitialized = true;
    }
    public void SetInvincible(int duration)
    {
        IsInvincible = true;
        InvincibleDuration = duration;
    }

    // Hàm này được gọi từ GameManagerNet để giảm thời gian hiệu lực
    public void TickInvincibility()
    {
        if (InvincibleDuration > 0)
        {
            InvincibleDuration--;
            if (InvincibleDuration <= 0)
            {
                IsInvincible = false;
            }
        }
    }

    private void UpdateInvincibleVisuals()
    {
        // Cập nhật lại màu chữ
        UpdateStatTexts();

        // Ám vàng cho ảnh lá bài
        if (cardImage != null)
        {
            // Khi Vô Địch thì nhuộm vàng, bình thường thì để màu gốc của ảnh
            cardImage.color = IsInvincible ? new Color(1f, 0.84f, 0.5f) : Color.white;
        }
    }
    private void LoadVisualsFromID()
    {
        if (CardDatabase.Instance == null) return;

        CardDataSO data = CardDatabase.Instance.GetCardData(CardID);
        if (data != null)
        {
            if (cardImage != null) cardImage.sprite = data.cardImage;

            gameObject.name = $"Card_{data.cardName}_{Object.Id}";
            CurrentSkill = data.skill;
        }
    }


    private void RefreshParentPosition()
    {
        // 1. Nếu bài đang trên tay -> Lấy vị trí tay từ GameUIManager
        if (HandIndex != -1)
        {
            int localId = GameManagerNet.Instance.GetLocalPlayerID();
            Transform targetParent = (OwnerID == localId) ?
                InGameUIManager.Instance.RightHandPos : // "Tôi" luôn ở bên phải
                InGameUIManager.Instance.LeftHandPos;   // Đối thủ bên trái

            SetParentIfChanged(targetParent);
        }
        // 2. Nếu bài đã đánh xuống bàn -> Lấy vị trí Slot từ GameUIManager
        else
        {
            // Truy cập BoardState từ GameManagerNet (nơi chứa dữ liệu mạng)
            var boardState = GameManagerNet.Instance.BoardState;

            for (int i = 0; i < 9; i++)
            {
                if (boardState[i] == Object.Id)
                {
                    // Truy cập mảng Slots từ GameUIManager (nơi chứa Transform)
                    if (InGameUIManager.Instance.Slots != null && InGameUIManager.Instance.Slots.Length > i)
                    {
                        SetParentIfChanged(InGameUIManager.Instance.Slots[i]);
                    }
                    break;
                }
            }
        }
    }

    private void SetParentIfChanged(Transform target)
    {
        if (target != null && transform.parent != target)
        {
            transform.SetParent(target, false);
            // Reset Transform để UI không bị méo
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            transform.localPosition = Vector3.zero;
        }
    }

    private void UpdateStatTexts()
    {
        // Màu chủ đạo: Nếu Vô Địch thì dùng Vàng Kim, không thì dùng màu Cảnh Giới
        Color GetColor(int stat)
        {
            if (IsInvincible) return new Color(1f, 0.84f, 0f); // Gold
            return RealmCalculator.GetRealmStat(stat).displayColor;
        }

        int GetVal(int stat) => RealmCalculator.GetRealmStat(stat).displayValue;

        if (txtTop)
        {
            txtTop.text = GetVal(Top).ToString();
            txtTop.color = GetColor(Top);
        }
        if (txtRight)
        {
            txtRight.text = GetVal(Right).ToString();
            txtRight.color = GetColor(Right);
        }
        if (txtBottom)
        {
            txtBottom.text = GetVal(Bottom).ToString();
            txtBottom.color = GetColor(Bottom);
        }
        if (txtLeft)
        {
            txtLeft.text = GetVal(Left).ToString();
            txtLeft.color = GetColor(Left);
        }
    }

    private void UpdateBackgroundColor()
    {
        // Logic đổi màu nền của ô Slot khi bài đặt vào
        if (HandIndex == -1 && transform.parent != null)
        {
            Image image = transform.parent.GetComponent<Image>();
            int localId = GameManagerNet.Instance.GetLocalPlayerID();
            if (image != null) image.color = (OwnerID == localId) ? colorP1 : colorP2;
        }
    }

    public void FlipOwner()
    {
        if (IsInvincible) return;
        OwnerID = 1 - OwnerID;
        // Fusion tự động sync, Render() sẽ gọi RefreshState() để cập nhật màu
    }

    public void SetHighlight(bool isActive)
    {
        if (highlightObj != null) highlightObj.SetActive(isActive);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Khi nhấn xuống -> Báo Manager để bắt đầu đếm giờ Long Press
        if (GameManagerNet.Instance != null)
            GameManagerNet.Instance.OnCardInputDown(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Khi nhấc tay lên -> Báo Manager để quyết định là Click hay kết thúc Long Press
        if (GameManagerNet.Instance != null)
            GameManagerNet.Instance.OnCardInputUp(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Khi kéo chuột ra khỏi bài -> Hủy Long Press nếu đang giữ
        if (GameManagerNet.Instance != null)
            GameManagerNet.Instance.OnCardInputExit(this);
    }
}