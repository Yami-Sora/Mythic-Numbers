using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class CardNet : NetworkBehaviour
{
    [Header("UI Refs")]
    public TextMeshProUGUI txtTop, txtRight, txtBottom, txtLeft;
    public Image bgImage; // Kéo cái ảnh nền lá bài vào đây
    public GameObject highlightObj; // Một cái viền vàng (ẩn đi mặc định)

    // Dữ liệu đồng bộ qua mạng
    [Networked] public int Top { get; set; }
    [Networked] public int Right { get; set; }
    [Networked] public int Bottom { get; set; }
    [Networked] public int Left { get; set; }
    [Networked] public int OwnerID { get; set; } // 0 = Player1 (Host), 1 = Player2 (Client)
    [Networked] public int HandIndex { get; set; } // Vị trí trong tay (-1 nếu đã xuống bàn)

    // Màu sắc (Xanh vs Đỏ)
    private Color colorP1 = new Color(0.2f, 0.4f, 1f); // Xanh
    private Color colorP2 = new Color(1f, 0.3f, 0.3f); // Đỏ

    public override void Spawned()
    {
        UpdateVisuals();
    }

    // Hàm này gọi mỗi frame để đảm bảo UI đúng với dữ liệu mạng
    // Tự động di chuyển vị trí UI dựa trên trạng thái
    public override void FixedUpdateNetwork()
    {
        UpdateVisuals();

        // Nếu bài chưa đánh (HandIndex != -1) -> Về tay
        if (HandIndex != -1)
        {
            Transform targetParent = (OwnerID == 0) ?
                GameManagerNet.Instance.rightHandPos : // P1 (Host) ở bên phải (logic tạm)
                GameManagerNet.Instance.leftHandPos;   // P2 ở bên trái

            // Vì là NetworkObject, ta không setParent trực tiếp mà chỉ lerp position
            // Nhưng để đơn giản cho người mới, ta sẽ set parent ở local visual (nếu chưa set)
            if (transform.parent != targetParent) transform.SetParent(targetParent, false);
        }
        else
        {
            // Bài đã đánh -> Cần tìm nó đang ở ô nào trên bàn
            // Duyệt mảng BoardState trong GameManager để tìm xem mình đang ở ô số mấy
            for (int i = 0; i < 9; i++)
            {
                if (GameManagerNet.Instance.BoardState[i] == Object.Id)
                {
                    Transform targetSlot = GameManagerNet.Instance.slots[i];
                    if (transform.parent != targetSlot)
                    {
                        transform.SetParent(targetSlot, false);
                        transform.localPosition = Vector3.zero; // Căn giữa ô
                        transform.localScale = Vector3.one; // Đảm bảo không bị biến dạng
                    }
                    break;
                }
            }
        }
    }

    void UpdateVisuals()
    {
        txtTop.text = Top.ToString();
        txtRight.text = Right.ToString();
        txtBottom.text = Bottom.ToString();
        txtLeft.text = Left.ToString();

        bgImage.color = (OwnerID == 0) ? colorP1 : colorP2;
    }

    // Sự kiện Click (Gán vào Button component)
    public void OnCardClicked()
    {
        // 1. In ra thông số để kiểm tra xem tại sao if lại sai
        Debug.Log($"Click! HasInputAuth: {Object.HasInputAuthority}, HandIndex: {HandIndex}, OwnerID: {OwnerID}");

        // 2. Điều kiện kiểm tra đơn giản hơn cho Shared Mode
        // Chỉ cần kiểm tra: Bài đang ở trên tay (HandIndex != -1) 
        // VÀ (Bài của mình VÀ mình là P1) HOẶC (Bài đối thủ VÀ mình là P2)

        if (HandIndex != -1)
        {
            // Logic kiểm tra quyền sở hữu đơn giản:
            // Lấy ID người chơi hiện tại từ GameManager (chúng ta sẽ thêm hàm này sau)
            int myPlayerID = GameManagerNet.Instance.GetLocalPlayerID();

            if (OwnerID == myPlayerID)
            {
                GameManagerNet.Instance.SelectCard(this);
                Debug.Log("-> Đã chọn bài thành công");
            }
            else
            {
                Debug.LogWarning("-> Không phải bài của bạn!");
            }
        }
    }

    // Hàm đổi chủ sở hữu (Khi bị lật)
    public void FlipOwner()
    {
        OwnerID = 1 - OwnerID; // Đảo 0 thành 1, 1 thành 0
    }
    public void SetHighlight(bool isActive)
    {
        if (highlightObj != null)
        {
            highlightObj.SetActive(isActive);
        }
    }
}