using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class CardNet : NetworkBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TextMeshProUGUI txtTop, txtRight, txtBottom, txtLeft;
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
                GameManagerNet.Instance.RightHandPos : // P1 (Host) ở bên phải (logic tạm)
                GameManagerNet.Instance.LeftHandPos;   // P2 ở bên trái

            // Set Parent Cục bộ (Local) (nếu chưa set) để UI tự động sắp xếp gọn gàng, bỏ qua Lerp mạng cho code đơn giản hơn.
            if (transform.parent != targetParent) transform.SetParent(targetParent, false);
        }
        else
        {
            // Bài đã đánh -> Cần tìm nó đang ở ô nào trên bàn
            // Duyệt mảng BoardState chứa các NetworkId trong GameManager để tìm xem lá bài hiện tại đang ở ô số mấy
            for (int i = 0; i < 9; i++)
            {
                if (GameManagerNet.Instance.BoardState[i] == Object.Id)//ID mạng của lá bài hiện tại có khớp với ID đang được lưu trữ trong ô bàn cờ số i không?
                {
                    Transform targetSlot = GameManagerNet.Instance.Slots[i];
                    if (transform.parent != targetSlot)
                    {
                        transform.SetParent(targetSlot, false);
                        transform.localPosition = Vector3.zero; // Căn giữa ô
                        transform.localScale = Vector3.one; // Đảm bảo không bị biến dạng
                        UpdateBackgroundColor();
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
    }
    void UpdateBackgroundColor()
    {
        if (HandIndex == -1)
        {
            Image image = transform.parent.GetComponent<Image>();
            image.color = (OwnerID == 0) ? colorP1 : colorP2;
        }
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
        UpdateBackgroundColor();
    }
    public void SetHighlight(bool isActive)
    {
        if (highlightObj != null)
        {
            highlightObj.SetActive(isActive);
        }
    }
}