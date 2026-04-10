using UnityEngine;
using UnityEngine.UI;

public class UI_Socket : MonoBehaviour
{
    [SerializeField] private Image imgHighlight; // Cái viền sáng khi được chọn
    [SerializeField] private Image imgGemIcon;   // Hình viên ngọc sau khi khảm
    [SerializeField] private Image imgDirectionIcon; // Icon mũi tên chỉ hướng (Top/Down...)

    public GemDirection Direction { get; private set; }

    public void Setup(GemDirection dir)
    {
        Direction = dir;
        imgHighlight.gameObject.SetActive(false);
        imgGemIcon.gameObject.SetActive(false); // Mặc định lỗ trống

        // Mốt sếp đổi sprite của imgDirectionIcon theo hướng dir để người chơi dễ nhìn nhé
    }

    public void SetHighlight(bool isActive) => imgHighlight.gameObject.SetActive(isActive);

    public void OnClickSocket()
    {
        CardDetailManager.Instance.OnSocketClicked(this);
    }
}