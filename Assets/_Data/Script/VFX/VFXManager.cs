using UnityEngine;
using TMPro;
using DG.Tweening;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

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

            // Set màu: Lấy màu đỏ.
            txt.color = color ?? new Color(1f, 0.2f, 0.2f, 1f);

            //Gom hết vào Sequence để quản lý vòng đời, chống lỗi "Destroy sớm"
            Sequence seq = DOTween.Sequence().SetLink(go);

            // 1. Phóng to lên
            seq.Join(go.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack));

            // 2. Bay lên trên 150px trong 1 giây 
            seq.Join(go.transform.DOMoveY(go.transform.position.y + 150f, 1f).SetEase(Ease.OutCubic));

            // 3. Mờ dần Alpha về 0 trong 2 giây
            seq.Join(txt.DOFade(0f, 2f).SetEase(Ease.InQuad));

            // 4. CHỐT SỔ: Chờ cả 3 thằng trên chạy xong hết 100% thì mới tự hủy
            seq.OnComplete(() => Destroy(go));
        }
    }
}