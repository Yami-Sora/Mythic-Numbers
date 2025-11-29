using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Launcher UI References")]
    [SerializeField] private Image LeftAiImage;
    [SerializeField] private Image LeftPlayer2;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void Start()
    {
        // Tìm NetworkRunner đang chạy (Nó được truyền từ Menu sang)
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();

        if (runner != null)
        {
            if (runner.GameMode == GameMode.Single)
            {
                SetLeftImageActive(true); // Bật ảnh AI
            }
            else
            {
                SetLeftImageActive(false); // Bật ảnh Player 2 (Online)
            }
        }
    }
    public void SetLeftImageActive(bool isActive)
    {
        if (LeftAiImage != null)
        {
            LeftAiImage.gameObject.SetActive(isActive);
        }
        if (LeftPlayer2 != null)
        {
            LeftPlayer2.gameObject.SetActive(!isActive);
        }
    }

}