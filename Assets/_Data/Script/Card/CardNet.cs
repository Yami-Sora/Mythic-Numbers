using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class CardNet : NetworkBehaviour
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

    private ChangeDetector _changes;
    private bool _isInitialized = false;
    private readonly Color colorP1 = new Color(0.2f, 0.4f, 1f);
    private readonly Color colorP2 = new Color(1f, 0.3f, 0.3f);

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
            }
        }
    }

    public void RefreshState()
    {
        if (GameManagerNet.Instance == null || GameUIManager.Instance == null) 
        { 
            Debug.LogWarning("GameManagerNet or GameUIManager is not ready yet.");
            _isInitialized = false;
            return; 
        }
        LoadVisualsFromID();
        UpdateStatTexts();
        RefreshParentPosition();
        UpdateBackgroundColor();
        _isInitialized = true;
    }
    private void LoadVisualsFromID()
    {
        if (CardDatabase.Instance == null) return;

        CardDataSO data = CardDatabase.Instance.GetCardData(CardID);
        if (data != null)
        {
            if (cardImage != null) cardImage.sprite = data.artwork;

            gameObject.name = $"Card_{data.cardName}_{Object.Id}";
        }
    }
    private void RefreshParentPosition()
    {
        // 1. Nếu bài đang trên tay -> Lấy vị trí tay từ GameUIManager
        if (HandIndex != -1)
        {
            Transform targetParent = (OwnerID == 0) ?
                GameUIManager.Instance.RightHandPos : // Player 1 (Host) thường bên phải
                GameUIManager.Instance.LeftHandPos;

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
                    if (GameUIManager.Instance.Slots != null && GameUIManager.Instance.Slots.Length > i)
                    {
                        SetParentIfChanged(GameUIManager.Instance.Slots[i]);
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
        if (txtTop) txtTop.text = Top.ToString();
        if (txtRight) txtRight.text = Right.ToString();
        if (txtBottom) txtBottom.text = Bottom.ToString();
        if (txtLeft) txtLeft.text = Left.ToString();
    }

    private void UpdateBackgroundColor()
    {
        // Logic đổi màu nền của ô Slot khi bài đặt vào
        if (HandIndex == -1 && transform.parent != null)
        {
            Image image = transform.parent.GetComponent<Image>();
            if (image != null) image.color = (OwnerID == 0) ? colorP1 : colorP2;
        }
    }

    public void OnCardClicked()
    {
        // Gọi về GameManagerNet, nó sẽ tự chuyển tiếp sang InputHandler
        if (HandIndex != -1 && GameManagerNet.Instance != null)
        {
            GameManagerNet.Instance.SelectCard(this);
        }
    }

    public void FlipOwner()
    {
        OwnerID = 1 - OwnerID;
        // Fusion tự động sync, Render() sẽ gọi RefreshState() để cập nhật màu
    }

    public void SetHighlight(bool isActive)
    {
        if (highlightObj != null) highlightObj.SetActive(isActive);
    }
}