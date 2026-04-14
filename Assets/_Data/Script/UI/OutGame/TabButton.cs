using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TabButton : MonoBehaviour
{
    [SerializeField] private Canvas_NavigationManager.TabType tabType;
    private RectTransform _rectTransform;
    private Vector2 _originalPos;
    private bool _isSelected = false;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _originalPos = _rectTransform.anchoredPosition;
    }

    public void Select()
    {
        if (_isSelected) return;
        _isSelected = true;

        _rectTransform.DOKill(); // Dập tắt tween cũ nếu có
        _rectTransform.DOAnchorPosY(_originalPos.y + 3f, 0.2f);
    }

    public void Deselect()
    {
        _isSelected = false;

        _rectTransform.DOKill();
        _rectTransform.DOAnchorPosY(_originalPos.y, 0.2f);
    }

    public void OnClick()
    {
        Canvas_NavigationManager.Instance.OnTabButtonClicked((int)tabType);
    }
}