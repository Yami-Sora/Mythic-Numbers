using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CardFocusUI : YamiMonoBehaviour
{
    [Header("References (Kéo từ đối tượng lá bài mẫu trong Panel)")]
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI txtTop, txtRight, txtBottom, txtLeft;

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
        if (source.CurrentSkill != null)
        {
            // Nếu có skill, thử lấy ảnh từ database (để đảm bảo ảnh gốc chất lượng cao)
            var data = CardDatabase.Instance.GetCardData(source.CardID);
            if (data != null) cardImage.sprite = data.artwork;
        }

        // Copy chỉ số hiện tại
        txtTop.text = source.Top.ToString();
        txtRight.text = source.Right.ToString();
        txtBottom.text = source.Bottom.ToString();
        txtLeft.text = source.Left.ToString();

        _currentSourceCard = source;
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