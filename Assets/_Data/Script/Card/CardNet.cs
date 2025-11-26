using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardNet : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI txtTop, txtRight, txtBottom, txtLeft;
    [SerializeField] private GameObject highlightObj;

    [Networked] public int Top { get; set; }
    [Networked] public int Right { get; set; }
    [Networked] public int Bottom { get; set; }
    [Networked] public int Left { get; set; }
    [Networked] public int OwnerID { get; set; } // 0 = Blue/Host, 1 = Red/Client/AI
    [Networked] public int HandIndex { get; set; }

    private readonly Color colorP1 = new Color(0.2f, 0.4f, 1f); // Blue
    private readonly Color colorP2 = new Color(1f, 0.3f, 0.3f); // Red

    public override void Spawned()
    {
        UpdateVisuals();
        RefreshParentOnUIReady();
    }

    public override void Render()
    {
        if (GameManagerNet.Instance == null || !GameManagerNet.Instance.IsUIReady) return;
        UpdateVisuals();
        RefreshParentOnUIReady();
    }

    private void RefreshParentOnUIReady()
    {
        if (HandIndex != -1) // Đang trên tay
        {
            var gm = GameManagerNet.Instance;
            int localPlayerId = gm.GetLocalPlayerID(); // Hàm này giờ trả về 0 hoặc 1
            Transform targetParent = null;

            // Logic hiển thị:
            // Nếu đây là bài của tôi (OwnerID == localPlayerId) -> Hiện bên Phải
            // Nếu đây là bài địch -> Hiện bên Trái
            if (OwnerID == localPlayerId)
            {
                targetParent = gm.RightHandPos;
            }
            else
            {
                targetParent = gm.LeftHandPos;
            }

            SetParentIfChanged(targetParent);
        }
        else // Đã đánh xuống bàn
        {
            // ... Code tìm Slot giữ nguyên như cũ ...
            for (int i = 0; i < 9; i++)
            {
                if (GameManagerNet.Instance.BoardState[i] == Object.Id)
                {
                    if (GameManagerNet.Instance.Slots != null && i < GameManagerNet.Instance.Slots.Length)
                    {
                        SetParentIfChanged(GameManagerNet.Instance.Slots[i]);
                        UpdateBackgroundColor();
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
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            // Fix Z axis
            Vector3 pos = transform.localPosition;
            pos.z = 0;
            transform.localPosition = pos;
        }
    }

    private void UpdateVisuals()
    {
        if (txtTop) txtTop.text = Top.ToString();
        if (txtRight) txtRight.text = Right.ToString();
        if (txtBottom) txtBottom.text = Bottom.ToString();
        if (txtLeft) txtLeft.text = Left.ToString();
        UpdateBackgroundColor();
    }

    private void UpdateBackgroundColor()
    {
        if (HandIndex == -1)
        {
            Image image = transform.parent.GetComponent<Image>();
            var gm = GameManagerNet.Instance;
            int localPlayerId = gm.GetLocalPlayerID();
            if (image != null) image.color = (OwnerID == localPlayerId) ? colorP1 : colorP2;
        }
    }

    // --- LOGIC LẬT BÀI SỬA LẠI ---
    public void FlipOwner()
    {
        // KHÔNG dùng ActivePlayers vì Offline mode chỉ có 1 player -> lỗi count != 2
        // KHÔNG dùng PlayerRef vì OwnerID đang dùng logic 0 và 1.

        // Đảo ngược 0 thành 1, 1 thành 0
        OwnerID = 1 - OwnerID;

        UpdateBackgroundColor();
    }

    // ... Các hàm click giữ nguyên ...
    public void OnCardClicked()
    {
        if (HandIndex != -1 && GameManagerNet.Instance != null)
        {
            if (OwnerID == GameManagerNet.Instance.GetLocalPlayerID())
            {
                GameManagerNet.Instance.SelectCard(this);
            }
        }
    }
    public void SetHighlight(bool isActive)
    {
        if (highlightObj != null) highlightObj.SetActive(isActive);
    }
}