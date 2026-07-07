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
    [SerializeField] private AnimationMode animMode = AnimationMode.SlideAndScale;
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease easeType = Ease.Linear;

    [Header("--- Slide Configuration ---")]
    [SerializeField] private bool autoCollapseToToggle = true;
    [SerializeField] private Vector2 collapsedPosition;
    [SerializeField] private Vector2 expandedPosition;

    [Header("--- Rotate Toggle Button ---")]
    [SerializeField] private bool rotateToggle = true;
    [SerializeField] private float collapsedRotation = 0f;
    [SerializeField] private float expandedRotation = 180f;

    private bool _isExpanded = true;
    private Tween _tweenMenu;
    private Tween _tweenScale;
    private Tween _tweenFade;
    private Tween _tweenRotate;

    public enum AnimationMode { Slide, Scale, Fade, SlideAndScale }

    private void Start()
    {
        if (btnToggle != null)
        {
            btnToggle.onClick.AddListener(ToggleMenu);
            
            // Tự động lấy anchoredPosition của btnToggle làm điểm đóng nếu bật autoCollapseToToggle
            if (autoCollapseToToggle)
            {
                collapsedPosition = btnToggle.GetComponent<RectTransform>().anchoredPosition;
            }
        }

        // Khởi tạo trạng thái ban đầu (mặc định là mở)
        SetMenuState(true, false);
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
        _tweenScale?.Kill();
        _tweenFade?.Kill();
        _tweenRotate?.Kill();

        // Kích hoạt panel lên trước khi chạy hoạt ảnh mở
        if (_isExpanded && panelMenuContent != null)
        {
            panelMenuContent.gameObject.SetActive(true);
        }

        if (animate)
        {
            // 1. Animation cho Panel nội dung
            switch (animMode)
            {
                case AnimationMode.Slide:
                    if (panelMenuContent != null)
                    {
                        Vector2 targetPos = _isExpanded ? expandedPosition : collapsedPosition;
                        _tweenMenu = panelMenuContent.DOAnchorPos(targetPos, duration)
                            .SetEase(easeType)
                            .OnComplete(() => {
                                if (!_isExpanded) panelMenuContent.gameObject.SetActive(false);
                            });
                    }
                    break;

                case AnimationMode.Scale:
                    if (panelMenuContent != null)
                    {
                        Vector3 targetScale = _isExpanded ? Vector3.one : Vector3.zero;
                        _tweenMenu = panelMenuContent.DOScale(targetScale, duration)
                            .SetEase(easeType)
                            .OnComplete(() => {
                                if (!_isExpanded) panelMenuContent.gameObject.SetActive(false);
                            });
                    }
                    break;

                case AnimationMode.Fade:
                    if (canvasGroupContent != null)
                    {
                        float targetAlpha = _isExpanded ? 1f : 0f;
                        canvasGroupContent.blocksRaycasts = _isExpanded;
                        _tweenFade = canvasGroupContent.DOFade(targetAlpha, duration)
                            .SetEase(easeType)
                            .OnComplete(() => {
                                if (!_isExpanded && panelMenuContent != null) panelMenuContent.gameObject.SetActive(false);
                            });
                    }
                    break;

                case AnimationMode.SlideAndScale:
                    if (panelMenuContent != null)
                    {
                        Vector2 targetPos = _isExpanded ? expandedPosition : collapsedPosition;
                        Vector3 targetScale = _isExpanded ? Vector3.one : Vector3.zero;

                        if (_isExpanded)
                        {
                            // Đặt trạng thái bắt đầu từ btnToggle trước khi tween ra ngoài
                            panelMenuContent.anchoredPosition = collapsedPosition;
                            panelMenuContent.localScale = Vector3.zero;
                        }

                        _tweenMenu = panelMenuContent.DOAnchorPos(targetPos, duration).SetEase(easeType);
                        _tweenScale = panelMenuContent.DOScale(targetScale, duration)
                            .SetEase(easeType)
                            .OnComplete(() => {
                                if (!_isExpanded) panelMenuContent.gameObject.SetActive(false);
                            });
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
                        if (!_isExpanded) panelMenuContent.gameObject.SetActive(false);
                    }
                    break;

                case AnimationMode.Scale:
                    if (panelMenuContent != null)
                    {
                        panelMenuContent.localScale = _isExpanded ? Vector3.one : Vector3.zero;
                        if (!_isExpanded) panelMenuContent.gameObject.SetActive(false);
                    }
                    break;

                case AnimationMode.Fade:
                    if (canvasGroupContent != null)
                    {
                        canvasGroupContent.alpha = _isExpanded ? 1f : 0f;
                        canvasGroupContent.blocksRaycasts = _isExpanded;
                        if (!_isExpanded && panelMenuContent != null) panelMenuContent.gameObject.SetActive(false);
                    }
                    break;

                case AnimationMode.SlideAndScale:
                    if (panelMenuContent != null)
                    {
                        panelMenuContent.anchoredPosition = _isExpanded ? expandedPosition : collapsedPosition;
                        panelMenuContent.localScale = _isExpanded ? Vector3.one : Vector3.zero;
                        if (!_isExpanded) panelMenuContent.gameObject.SetActive(false);
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