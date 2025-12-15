using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CardFocusUI : YamiMonoBehaviour
{
    [Header("References (Kéo từ đối tượng lá bài mẫu trong Panel)")]
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI txtTop, txtRight, txtBottom, txtLeft;
    [Header("Mod Stats")]
    [SerializeField] private TextMeshProUGUI modTxtTop;
    [SerializeField] private TextMeshProUGUI modTxtRight;
    [SerializeField] private TextMeshProUGUI modTxtBottom;
    [SerializeField] private TextMeshProUGUI modTxtLeft;

    [Header("Animation Settings")]
    [SerializeField] private float appearDuration = 0.3f;
    [SerializeField] private float closeDuration = 0.4f; // Thời gian bay về
    [SerializeField] private Transform cardContainer; // Đối tượng cha chứa hình ảnh lá bài để scale

    private Coroutine _currentRoutine;
    private CardNet _currentSourceCard;

    // Hàm này được GameUIManager gọi
    public void Show(CardNet sourceCard)
    {
        gameObject.SetActive(true);
        if (CanvasManager.Instance != null)
            CanvasManager.Instance.isCardFocusUIOpen = true;
        // 1. Copy dữ liệu từ lá bài gốc sang lá bài hiển thị
        CopyData(sourceCard);

        // 2. Chạy animation
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        _currentRoutine = StartCoroutine(AnimateOpenRoutine());
    }
    public void ClosePanel()
    {
        if (!gameObject.activeSelf) return;
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        _currentRoutine = StartCoroutine(AnimateCloseRoutine());
    }
    private void CopyData(CardNet source)
    {
        CardDataSO data = null;
        if (CardDatabase.Instance != null)
        {
            data = CardDatabase.Instance.GetCardData(source.CardID);
        }

        if (data != null)
        {
            cardImage.sprite = data.artwork;

            // Cập nhật text và màu sắc dựa trên so sánh Current vs Base
            UpdateStatUI(txtTop, modTxtTop, source.Top, data.top);
            UpdateStatUI(txtRight, modTxtRight, source.Right, data.right);
            UpdateStatUI(txtBottom, modTxtBottom, source.Bottom, data.bottom);
            UpdateStatUI(txtLeft, modTxtLeft, source.Left, data.left);
        }
        else
        {
            // Fallback nếu không tìm thấy data (chỉ hiện số trắng)
            UpdateStatUI(txtTop, modTxtTop, source.Top, source.Top);
            UpdateStatUI(txtRight, modTxtRight, source.Right, source.Right);
            UpdateStatUI(txtBottom, modTxtBottom, source.Bottom, source.Bottom);
            UpdateStatUI(txtLeft, modTxtLeft, source.Left, source.Left);
        }

        _currentSourceCard = source;
    }
    private void UpdateStatUI(TextMeshProUGUI mainText, TextMeshProUGUI modText, int currentVal, int baseVal)
    {
        if (mainText == null) return;

        mainText.text = currentVal.ToString();
        int diff = currentVal - baseVal;

        if (diff > 0)
        {
            // BUFF: Xanh
            mainText.color = Color.green;
            if (modText != null)
            {
                modText.gameObject.SetActive(true);
                modText.text = "+" + diff;
                modText.color = Color.green;
            }
        }
        else if (diff < 0)
        {
            // DEBUFF: Đỏ
            mainText.color = Color.red;
            if (modText != null)
            {
                modText.gameObject.SetActive(true);
                modText.text = diff.ToString();
                modText.color = Color.red;
            }
        }
        else
        {
            // BÌNH THƯỜNG: Trắng
            mainText.color = Color.white;
            if (modText != null) modText.gameObject.SetActive(false);
        }
    }
    // Hiệu ứng mở: Phóng to từ giữa màn hình
    private IEnumerator AnimateOpenRoutine()
    {
        // 1. Reset vị trí về giữa (Quan trọng! vì hàm Close đã làm nó lệch đi)
        cardContainer.localPosition = Vector3.zero;
        cardContainer.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < appearDuration)
        {
            float t = elapsed / appearDuration;
            // Lerp từ 0 -> 2
            cardContainer.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 2f, t);

            elapsed += Time.deltaTime;
            yield return null;
        }
        cardContainer.localScale = Vector3.one * 2f;
    }

    // Hiệu ứng đóng: Thu nhỏ và bay về vị trí slot
    private IEnumerator AnimateCloseRoutine()
    {
        Vector3 startPos = cardContainer.position;
        Vector3 startScale = cardContainer.localScale;

        // Nếu lá bài thật bị mất (do lỗi mạng/reset), thì bay về chỗ cũ hoặc giữ nguyên
        Vector3 targetPos = (_currentSourceCard != null) ? _currentSourceCard.transform.position : startPos;
        Vector3 targetScale = Vector3.one; // Thu về scale 1

        float elapsed = 0f;
        while (elapsed < closeDuration)
        {
            float t = elapsed / closeDuration;
            // Làm mượt chuyển động (SmoothStep)
            t = t * t * (3f - 2f * t);

            // Cập nhật lại targetPos liên tục phòng trường hợp Camera di chuyển hoặc Slot bị rung lắc
            if (_currentSourceCard != null) targetPos = _currentSourceCard.transform.position;

            cardContainer.position = Vector3.Lerp(startPos, targetPos, t);
            cardContainer.localScale = Vector3.Lerp(startScale, targetScale, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        gameObject.SetActive(false);
        if (CanvasManager.Instance != null)
            CanvasManager.Instance.isCardFocusUIOpen = false;
        _currentRoutine = null;
    }
}