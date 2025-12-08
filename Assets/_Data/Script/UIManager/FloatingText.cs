using System.Collections;
using TMPro;
using UnityEngine;

public class FloatingText : YamiMonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMesh;
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private float lifeTime = 1.5f;

    // Callback để trả object về pool (chứa method do GameUIManager cung cấp)
    private System.Action<GameObject> _returnToPoolCallback;

    protected override void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }
    public void Setup(string message, System.Action<GameObject> returnCallback = null)
    {
        _returnToPoolCallback = returnCallback;

        if (textMesh != null)
        {
            textMesh.text = message;
        }

        transform.localScale = Vector3.one;

        StartCoroutine(FloatUp());
    }

    private IEnumerator FloatUp()
    {
        float timer = 0f;

        while (timer < lifeTime)
        {
            // Chỉ di chuyển lên trên theo thời gian
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;

            timer += Time.deltaTime;
            yield return null;
        }

        // KẾT THÚC: Trả về pool thay vì Destroy
        if (_returnToPoolCallback != null)
        {
            _returnToPoolCallback(this.gameObject);
        }
        else
        {
            // Fallback: Nếu không có pool (dùng lẻ) thì mới destroy
            Destroy(gameObject);
        }
    }
}