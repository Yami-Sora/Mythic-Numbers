using UnityEngine;

public abstract class TabListenerBase : MonoBehaviour
{
    protected virtual void OnEnable()
    {
        Canvas_NavigationManager.OnTabChanged += OnTabChanged;
    }

    protected virtual void OnDisable()
    {
        Canvas_NavigationManager.OnTabChanged -= OnTabChanged;
    }

    protected abstract void OnTabChanged(Canvas_NavigationManager.TabType targetTab);
}