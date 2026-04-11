using UnityEngine;

public class Canvas_CardManager : MonoBehaviour
{
    public static Canvas_CardManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
    public void OnTransition()
    {
        CardListManager.Instance.gameObject.SetActive(true);
        CardDetailManager.Instance.gameObject.SetActive(false);
    }
}
