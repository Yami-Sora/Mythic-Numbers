using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance { get; private set; }
    [SerializeField] private CanvasGroup loadingOverlay; // Một cái Panel đen phủ toàn màn hình

    private void Awake()
    {
        Instance = this;
        if (loadingOverlay != null)
        {
            loadingOverlay.alpha = 0f;
            loadingOverlay.blocksRaycasts = false;
        }
    }

    public void PlayTransition(Action onMidWay)
    {
        // 1. Fade IN (Che màn hình)
        loadingOverlay.blocksRaycasts = true;
        loadingOverlay.DOFade(1f, 0.25f).OnComplete(() => {

            // 2. Thực hiện đổi Canvas ở giữa lúc màn hình đang đen
            onMidWay?.Invoke();

            // 3. Fade OUT (Mở màn hình)
            loadingOverlay.DOFade(0f, 0.25f).OnComplete(() => {
                loadingOverlay.blocksRaycasts = false;
            });
        });
    }

    public void ShowLoading(bool isLoading)
    {
        if (loadingOverlay == null) return;
        
        loadingOverlay.blocksRaycasts = isLoading;
        // Hiện mờ mờ để sếp biết là đang load, không cần đen kịt
        loadingOverlay.DOFade(isLoading ? 0.5f : 0f, 0.2f);
    }
}