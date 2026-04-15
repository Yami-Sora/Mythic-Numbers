using UnityEngine;

public class Canvas_CardManager : TabListenerBase
{
    public static Canvas_CardManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
    protected override void OnTabChanged(Canvas_NavigationManager.TabType targetTab)
    {
        if (CardListManager.Instance != null) CardListManager.Instance.gameObject.SetActive(true);
        if (CardDetailManager.Instance != null) CardDetailManager.Instance.gameObject.SetActive(false);
        if (DeckManager.Instance != null) DeckManager.Instance.gameObject.SetActive(true);
    }
}
