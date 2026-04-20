using UnityEngine;
using TMPro;
using DG.Tweening;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("Global UI Effects")]
    [Tooltip("Kéo cục Prefab Floating Text vào đây")]
    [SerializeField] private GameObject floatingTextPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SpawnFloatingText(string message, Transform spawnPos, Color? color = null)
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogWarning("[EffectManager] Thiếu Prefab Floating Text rồi sếp ơi!");
            return;
        }

        // Đẻ cục text ra ngay tại vị trí spawnPos
        GameObject go = Instantiate(floatingTextPrefab, spawnPos.position, Quaternion.identity, spawnPos.parent);

        // Ép tạm về 0 trước để làm hiệu ứng phóng to
        go.transform.localScale = Vector3.zero;

        TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
        if (txt != null)
        {
            txt.text = message;

            // Set màu: Nếu sếp không truyền màu vào, nó tự lấy màu đỏ.
            txt.color = color ?? new Color(1f, 0.2f, 0.2f, 1f);

            // COMBO DOTWEEN 3 TRONG 1:
            // 1. Phóng to từ 0 lên 0.1f (Bốp 1 phát ra luôn)
            go.transform.DOScale(new Vector3(0.1f, 0.1f, 0.1f), 0.2f).SetEase(Ease.OutBack);

            // 2. Bay lên trên 150px
            go.transform.DOMoveY(go.transform.position.y + 150f, 1f).SetEase(Ease.OutCubic);

            // 3. Mờ dần Alpha về 0 rồi tự hủy (Xóa rác)
            txt.DOFade(0f, 1f).SetEase(Ease.InQuad).OnComplete(() => Destroy(go));
        }
    }
}