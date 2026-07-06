using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CollapsibleMenuController : MonoBehaviour
{
    [Header("--- UI References ---")]
    [SerializeField] private Button btnToggle;
    [SerializeField] private RectTransform panelMenuContent;
    [SerializeField] private CanvasGroup canvasGroupContent;

    [Header("--- Animation Settings ---")]
    [SerializeField] private AnimationMode animMode = AnimationMode.Slide;
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    [Header("--- Slide Configuration ---")]
    [SerializeField] private Vector2 collapsedPosition;
    [SerializeField] private Vector2 expandedPosition;

    [Header("--- Rotate Toggle Button ---")]
    [SerializeField] private bool rotateToggle = true;
    [SerializeField] private float collapsedRotation = 0f;
    [SerializeField] private float expandedRotation = 180f;

    private bool _isExpanded = false;
    private Tween _tweenMenu;
    private Tween _tweenFade;
    private Tween _tweenRotate;

    public enum AnimationMode { Slide, Scale, Fade }

    private void Start()
    {
        if (btnToggle != null)
        {
            btnToggle.onClick.AddListener(ToggleMenu);
        }

        // Khởi tạo trạng thái ban đầu (mặc định là đóng)
        SetMenuState(false, false);
    }

    public void ToggleMenu()
    {
        SetMenuState(!_isExpanded, true);
    }

    public void SetMenuState(bool expand, bool animate)
    {
        _isExpanded = expand;

        // Dừng các Tween cũ nếu đang chạy
        _tweenMenu?.Kill();
        _tweenFade?.Kill();
        _tweenRotate?.Kill();

        if (animate)
        {
            // 1. Animation cho Panel nội dung
            switch (animMode)
            {
                case AnimationMode.Slide:
                    if (panelMenuContent != null)
                    {
                        Vector2 targetPos = _isExpanded ? expandedPosition : collapsedPosition;
                        _tweenMenu = panelMenuContent.DOAnchorPos(targetPos, duration).SetEase(easeType);
                    }
                    break;

                case AnimationMode.Scale:
                    if (panelMenuContent != null)
                    {
                        Vector3 targetScale = _isExpanded ? Vector3.one : Vector3.zero;
                        _tweenMenu = panelMenuContent.DOScale(targetScale, duration).SetEase(easeType);
                    }
                    break;

                case AnimationMode.Fade:
                    if (canvasGroupContent != null)
                    {
                        float targetAlpha = _isExpanded ? 1f : 0f;
                        canvasGroupContent.blocksRaycasts = _isExpanded;
                        _tweenFade = canvasGroupContent.DOFade(targetAlpha, duration).SetEase(easeType);
                    }
                    break;
            }

            // 2. Animation xoay nút Toggle
            if (rotateToggle && btnToggle != null)
            {
                float targetAngle = _isExpanded ? expandedRotation : collapsedRotation;
                _tweenRotate = btnToggle.transform.DORotate(new Vector3(0, 0, targetAngle), duration).SetEase(Ease.InOutQuad);
            }
        }
        else
        {
            // Thiết lập ngay lập tức không qua animation
            switch (animMode)
            {
                case AnimationMode.Slide:
                    if (panelMenuContent != null)
                    {
                        panelMenuContent.anchoredPosition = _isExpanded ? expandedPosition : collapsedPosition;
                    }
                    break;

                case AnimationMode.Scale:
                    if (panelMenuContent != null)
                    {
                        panelMenuContent.localScale = _isExpanded ? Vector3.one : Vector3.zero;
                    }
                    break;

                case AnimationMode.Fade:
                    if (canvasGroupContent != null)
                    {
                        canvasGroupContent.alpha = _isExpanded ? 1f : 0f;
                        canvasGroupContent.blocksRaycasts = _isExpanded;
                    }
                    break;
            }

            if (rotateToggle && btnToggle != null)
            {
                btnToggle.transform.rotation = Quaternion.Euler(0, 0, _isExpanded ? expandedRotation : collapsedRotation);
            }
        }
    }
}