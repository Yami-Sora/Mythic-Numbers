using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }
    [SerializeField] private CanvasGroup transitionOverlay; // Một cái Panel đen phủ toàn màn hình

    private void Awake()
    {
        Instance = this;
            transitionOverlay.alpha = 0f;
            transitionOverlay.blocksRaycasts = false;
        }
    }

    public void PlayTransition(Action onMidWay)
    {
        // 1. Fade IN (Che màn hình)
        transitionOverlay.blocksRaycasts = true;
        transitionOverlay.DOFade(1f, 0.25f).OnComplete(() => {

            // 2. Thực hiện đổi Canvas ở giữa lúc màn hình đang đen
            onMidWay?.Invoke();

            // 3. Fade OUT (Mở màn hình)
            transitionOverlay.DOFade(0f, 0.25f).OnComplete(() => {
                transitionOverlay.blocksRaycasts = false;
            });
        });
    }
}