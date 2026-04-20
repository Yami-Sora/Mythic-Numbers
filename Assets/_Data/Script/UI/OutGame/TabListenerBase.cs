using UnityEngine;

public abstract class TabListenerBase : YamiMonoBehaviour
{
    protected override void OnEnable()
    {
        Canvas_NavigationManager.OnTabChanged += OnTabChanged;
    }

    protected override void OnDisable()
    {
        Canvas_NavigationManager.OnTabChanged -= OnTabChanged;
    }

    protected abstract void OnTabChanged(Canvas_NavigationManager.TabType targetTab);
}