using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardObj : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI txtTop, txtRight, txtBottom, txtLeft;
    [SerializeField] private GameObject highlightObj;

    // --- DATA ---
    public int Top { get; set; }
    public int Right { get; set; }
    public int Bottom { get; set; }
    public int Left { get; set; }
    public int OwnerID { get; set; }
    public int HandIndex { get; set; }
    public int CardID { get; set; }

    // --- SKILL DATA ---
    public bool IsInvincible { get; set; }
    public int InvincibleDuration { get; set; }

    public BaseSkillSO CurrentSkill { get; private set; }

    private readonly Color colorP1 = new Color(0.2f, 0.4f, 1f);
    private readonly Color colorP2 = new Color(1f, 0.3f, 0.3f);
    private readonly Color colorInvincible = new Color(1f, 0.84f, 0f);

    private void Start()
    {
        transform.localScale = Vector3.one;
        RefreshState();
    }

    public void RefreshState()
    {
        if (GameManager.Instance == null || InGameUIManager.Instance == null) 
        { 
            Debug.LogWarning("GameManager or GameUIManager is not ready yet.");
            return; 
        }
        LoadVisualsFromID();
        UpdateStatTexts();
        RefreshParentPosition();
        UpdateBackgroundColor();
        UpdateInvincibleVisuals();
    }

    public void SetInvincible(int duration)
    {
        IsInvincible = true;
        InvincibleDuration = duration;
        UpdateInvincibleVisuals();
    }

    // Hàm này được gọi từ GameManager để giảm thời gian hiệu lực
    public void TickInvincibility()
    {
        if (InvincibleDuration > 0)
        {
            InvincibleDuration--;
            if (InvincibleDuration <= 0)
            {
                IsInvincible = false;
                UpdateInvincibleVisuals();
            }
        }
    }

    private void UpdateInvincibleVisuals()
    {
        UpdateStatTexts();

        if (cardImage != null)
        {
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

            gameObject.name = $"Card_{data.cardName}_{GetInstanceID()}";
            CurrentSkill = data.skill;
        }
    }

    private void RefreshParentPosition()
    {
        // 1. Nếu bài đang trên tay -> Lấy vị trí tay từ GameUIManager
        if (HandIndex != -1)
        {
            int localId = GameManager.Instance.GetLocalPlayerID();
            Transform targetParent = (OwnerID == localId) ?
                InGameUIManager.Instance.RightHandPos : // "Tôi" luôn ở bên phải
                InGameUIManager.Instance.LeftHandPos;   // Đối thủ bên trái

            SetParentIfChanged(targetParent);
        }
        // 2. Nếu bài đã đánh xuống bàn -> Lấy vị trí Slot từ GameUIManager
        else
        {
            var boardState = GameManager.Instance.BoardState;

            for (int i = 0; i < 9; i++)
            {
                if (boardState[i] == this)
                {
                    if (InGameUIManager.Instance.Slots != null && InGameUIManager.Instance.Slots.Length > i)
                    {
                        SetParentIfChanged(InGameUIManager.Instance.Slots[i], Vector3.one * 0.9f);
                    }
                    break;
                }
            }
        }
    }

    private void SetParentIfChanged(Transform target, Vector3? scale = null)
    {
        Vector3 targetScale = scale ?? Vector3.one;
        if (target != null && (transform.parent != target || transform.localScale != targetScale))
        {
            transform.SetParent(target, false);
            
            // Nếu bài đang trên tay (HandIndex != -1), ép thứ tự SiblingIndex
            // để đảm bảo HorizontalLayoutGroup xếp đúng: 0 bên trái, 4 bên phải.
            if (HandIndex != -1)
            {
                transform.SetSiblingIndex(HandIndex);
            }

            // Reset Transform để UI không bị méo
            transform.localScale = targetScale;
            transform.localRotation = Quaternion.identity;
            transform.localPosition = Vector3.zero;
        }
    }

    private void UpdateStatTexts()
    {
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
            int localId = GameManager.Instance.GetLocalPlayerID();
            if (image != null) image.color = (OwnerID == localId) ? colorP1 : colorP2;
        }
    }

    public void FlipOwner()
    {
        if (IsInvincible) return;
        OwnerID = 1 - OwnerID;
        RefreshState();
    }

    public void SetHighlight(bool isActive)
    {
        if (highlightObj != null) highlightObj.SetActive(isActive);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnCardInputDown(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnCardInputUp(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnCardInputExit(this);
    }
}