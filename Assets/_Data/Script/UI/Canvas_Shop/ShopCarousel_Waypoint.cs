using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;

public class ShopCarousel_Waypoint : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Waypoints (Các điểm neo)")]
    public RectTransform pointHiddenTop;
    public RectTransform pointTop;
    public RectTransform pointCenter;
    public RectTransform pointBottom;
    public RectTransform pointHiddenBottom;

    [Header("Menu Items (Các nút bấm)")]
    public List<RectTransform> menuItems;

    [Header("Settings")]
    public float tweenDuration = 0.4f;
    public Vector3 scaleCenter = new Vector3(1.2f, 1.2f, 1f);
    public Vector3 scaleSide = new Vector3(0.8f, 0.8f, 1f);
    public Vector3 scaleHidden = Vector3.zero;

    private int centerItemIndex = 1; // Mặc định item số 1 (Rút Cao Cấp) nằm giữa

    void Start()
    {
        UpdateUI(true); // Sắp xếp ngay lập tức khi mở game
    }

    // --- LOGIC DI CHUYỂN ---
    private void UpdateUI(bool isInstant = false)
    {
        int totalItems = menuItems.Count;

        for (int i = 0; i < totalItems; i++)
        {
            RectTransform item = menuItems[i];
            item.DOKill(); // Dừng mọi chuyển động cũ để tránh kẹt

            // Tính khoảng cách tương đối so với thằng đang ở giữa
            int relativePos = i - centerItemIndex;

            // Xử lý vòng lặp vô tận (Wrap around)
            if (relativePos > totalItems / 2) relativePos -= totalItems;
            else if (relativePos < -totalItems / 2f) relativePos += totalItems;

            RectTransform targetPoint;
            Vector3 targetScale;
            bool isHidden = false;

            // Gán điểm đến dựa trên vị trí tương đối
            if (relativePos == 0) // Đang ở giữa
            {
                targetPoint = pointCenter;
                targetScale = scaleCenter;
                item.SetAsLastSibling(); // Đưa lên trên cùng để không bị đè
            }
            else if (relativePos == -1) // Ở trên
            {
                targetPoint = pointTop;
                targetScale = scaleSide;
            }
            else if (relativePos == 1) // Ở dưới
            {
                targetPoint = pointBottom;
                targetScale = scaleSide;
            }
            else if (relativePos < -1) // Vượt quá lên trên -> Góc chết trên
            {
                targetPoint = pointHiddenTop;
                targetScale = scaleHidden;
                isHidden = true;
            }
            else // Vượt quá xuống dưới -> Góc chết dưới
            {
                targetPoint = pointHiddenBottom;
                targetScale = scaleHidden;
                isHidden = true;
            }

            // --- THỰC THI DI CHUYỂN ---
            if (isInstant || isHidden)
            {
                // Fix lỗi 1: Dùng TỌA ĐỘ THẾ GIỚI (.position) thay vì tọa độ địa phương
                item.position = targetPoint.position;
                item.localScale = targetScale;
            }
            else
            {
                // Fix lỗi 1: Dùng DOMove thay vì DOAnchorPos
                item.DOMove(targetPoint.position, tweenDuration).SetEase(Ease.OutBack);
                item.DOScale(targetScale, tweenDuration).SetEase(Ease.OutBack);
            }
        }
    }

    // --- XỬ LÝ VUỐT ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Khởi động động cơ vuốt...
    }

    // --- XỬ LÝ VUỐT ---
    public void OnDrag(PointerEventData eventData)
    {
        // Có thể để trống
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Vuốt lên
        if (eventData.delta.y > 50f)
        {
            centerItemIndex = (centerItemIndex + 1) % menuItems.Count;
            UpdateUI();
        }
        // Vuốt xuống
        else if (eventData.delta.y < -50f)
        {
            centerItemIndex = (centerItemIndex - 1 + menuItems.Count) % menuItems.Count;
            UpdateUI();
        }
    }
}   