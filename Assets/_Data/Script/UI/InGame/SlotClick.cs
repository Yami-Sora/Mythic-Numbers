using UnityEngine;
public class SlotClick : MonoBehaviour
{
    public int slotIndex; // Điền 0 đến 8 vào Inspector từng ô
    public void OnClick()
    {
        // Tìm GameManager trong scene để gọi
        GameManager.Instance.OnSlotClicked(slotIndex);
    }
}
