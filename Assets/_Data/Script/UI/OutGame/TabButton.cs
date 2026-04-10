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

        // Nhón chân lên 3 đơn vị 
        _rectTransform.DOAnchorPosY(_originalPos.y + 3f, 0.2f);
    }

    public void Deselect()
    {
        _isSelected = false;
        // Hạ chân xuống vị trí cũ
        _rectTransform.DOAnchorPosY(_originalPos.y, 0.2f);
    }

    public void OnClick()
    {
        Canvas_NavigationManager.Instance.OnTabButtonClicked((int)tabType);
    }
}