using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class CardNet : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI txtTop, txtRight, txtBottom, txtLeft;
    [SerializeField] private GameObject highlightObj;

    [Networked] public int Top { get; set; }
    [Networked] public int Right { get; set; }
    [Networked] public int Bottom { get; set; }
    [Networked] public int Left { get; set; }

    // ✅ FUSION 2: Sử dụng ChangeDetector thay vì attribute OnChanged
    [Networked] public int OwnerID { get; set; }
    [Networked] public int HandIndex { get; set; }

    private ChangeDetector _changes;
    private readonly Color colorP1 = new Color(0.2f, 0.4f, 1f);
    private readonly Color colorP2 = new Color(1f, 0.3f, 0.3f);

    public override void Spawned()
    {
        // Khởi tạo bộ theo dõi thay đổi
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

        // Reset Scale và tìm Canvas ngay lập tức để tránh lỗi hiển thị ban đầu (Fail-Safe)
        transform.localScale = Vector3.one;
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            transform.SetParent(canvas.transform, false);
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }

        // Gọi RefreshState ngay lập tức để cập nhật visual ban đầu
        RefreshState();
    }

    // ✅ Vòng lặp Render: Chạy mỗi frame để check thay đổi mạng
    // Tối ưu hơn FixedUpdateNetwork vì chỉ chạy logic khi có dữ liệu thay đổi
    public override void Render()
    {
        // DetectChanges trả về danh sách các property đã thay đổi từ lần render trước
        foreach (var change in _changes.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(Top):
                case nameof(OwnerID):
                case nameof(HandIndex):
                    RefreshState(); // Tự động cập nhật khi Server đổi dữ liệu
                    break;
            }
        }
    }

    // Hàm cập nhật tổng thể (Public để GameManager gọi khi cần)
    public void RefreshState()
    {
        if (GameManagerNet.Instance == null || !GameManagerNet.Instance.IsUIReady) return;

        UpdateVisuals();
        RefreshParentPosition();
        UpdateBackgroundColor();
    }

    private void RefreshParentPosition()
    {
        if (HandIndex != -1) // Trên tay
        {
            Transform targetParent = (OwnerID == 0) ?
                GameManagerNet.Instance.RightHandPos :
                GameManagerNet.Instance.LeftHandPos;
            SetParentIfChanged(targetParent);
        }
        else // Xuống bàn
        {
            for (int i = 0; i < 9; i++)
            {
                if (GameManagerNet.Instance.BoardState[i] == Object.Id)
                {
                    if (GameManagerNet.Instance.Slots != null && GameManagerNet.Instance.Slots.Length > i)
                    {
                        SetParentIfChanged(GameManagerNet.Instance.Slots[i]);
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
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;

            transform.localPosition = Vector3.zero;
        }
    }

    private void UpdateVisuals()
    {
        if (txtTop) txtTop.text = Top.ToString();
        if (txtRight) txtRight.text = Right.ToString();
        if (txtBottom) txtBottom.text = Bottom.ToString();
        if (txtLeft) txtLeft.text = Left.ToString();
    }

    private void UpdateBackgroundColor()
    {
        if (HandIndex == -1)
        {
            Image image = transform.parent.GetComponent<Image>();
            if (image != null) image.color = (OwnerID == 0) ? colorP1 : colorP2;
        }
    }

    public void OnCardClicked()
    {
        if (HandIndex != -1 && GameManagerNet.Instance != null)
        {
            GameManagerNet.Instance.SelectCard(this);
        }
    }

    public void FlipOwner()
    {
        // Server đổi giá trị -> ChangeDetector trên Client sẽ bắt được và gọi RefreshState
        OwnerID = 1 - OwnerID;
    }

    public void SetHighlight(bool isActive)
    {
        if (highlightObj != null) highlightObj.SetActive(isActive);
    }
}