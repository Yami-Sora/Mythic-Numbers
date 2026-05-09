using UnityEngine;
using UnityEngine.UI;

public class TabButton : MonoBehaviour
{
    [SerializeField] private Canvas_NavigationManager.TabType tabType;
    private bool _isSelected = false;

    public void Select()
    {
        if (_isSelected) return;
        _isSelected = true;
    }

    public void Deselect()
    {
        _isSelected = false;
    }

    public void OnClick()
    {
        Canvas_NavigationManager.Instance.OnTabButtonClicked((int)tabType);
    }
}