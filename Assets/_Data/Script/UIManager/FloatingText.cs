using UnityEngine;
using TMPro;

public class FloatingText : YamiMonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 200f; // Tốc độ bay lên (pixel/s)
    [SerializeField] private float fadeSpeed = 2f;   // Tốc độ mờ dần
    [SerializeField] private float lifeTime = 1.5f;  // Thời gian tồn tại tối đa

    private TextMeshProUGUI tmpText;
    private float timer;

    protected override void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
    }

    public void Setup(string message)
    {
        tmpText.text = message;
        timer = lifeTime;
    }

    private void Update()
    {
        // 1. Bay lên
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

        // 2. Giảm thời gian sống
        timer -= Time.deltaTime;

        // 3. Hiệu ứng mờ dần (Fade Alpha)
        if (timer <= lifeTime / fadeSpeed) // Chỉ mờ ở nửa sau quãng đời
        {
            float alpha = Mathf.Clamp01(timer / (lifeTime / 2));
        }

        // 4. Tự hủy
        if (timer <= 0)
        {
            Destroy(gameObject);
        }
    }
}