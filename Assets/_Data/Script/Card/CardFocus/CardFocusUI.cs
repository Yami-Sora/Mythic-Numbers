using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CardFocusUI : YamiMonoBehaviour
{
    [Header("References")]
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

    private Coroutine _currentAnimRoutine;
    private Coroutine _monitoringRoutine; // Coroutine theo dõi chỉ số
    private CardNet _currentSourceCard;

    private int _baseTop, _baseRight, _baseBottom, _baseLeft;
    private int _displayedTop, _displayedRight, _displayedBottom, _displayedLeft;
    // Hàm này được GameUIManager gọi
    public void Show(CardNet sourceCard)
    {
        gameObject.SetActive(true);
        if (CanvasManager.Instance != null)
            CanvasManager.Instance.isCardFocusUIOpen = true;

        _currentSourceCard = sourceCard;

        // 1. Lấy Base Stats
        FetchBaseStats(sourceCard);

        // 2. Reset cache và Update ngay lập tức để không bị trắng thông tin frame đầu
        ResetDisplayCache();
        ForceUpdateUI();

        // 3. Bắt đầu Coroutine theo dõi thay đổi (Thay cho Update)
        if (_monitoringRoutine != null) StopCoroutine(_monitoringRoutine);
        _monitoringRoutine = StartCoroutine(MonitorStatsRoutine());

        // 4. Chạy animation
        if (_currentAnimRoutine != null) StopCoroutine(_currentAnimRoutine);
        _currentAnimRoutine = StartCoroutine(AnimateOpenRoutine());
    }

    public void ClosePanel()
    {
        if (!gameObject.activeSelf) return;

        // Dừng theo dõi khi đóng panel
        if (_monitoringRoutine != null) StopCoroutine(_monitoringRoutine);

        if (_currentAnimRoutine != null) StopCoroutine(_currentAnimRoutine);
        _currentAnimRoutine = StartCoroutine(AnimateCloseRoutine());
    }

    private IEnumerator MonitorStatsRoutine()
    {
        // Vòng lặp vô tận, chạy cho đến khi Coroutine bị Stop (lúc đóng Panel)
        while (true)
        {
            // Kiểm tra Card còn tồn tại không
            if (_currentSourceCard == null || _currentSourceCard.Object == null || !_currentSourceCard.Object.IsValid)
            {
                ClosePanel();
                yield break;
            }

            // Kiểm tra thay đổi
            if (HasStatsChanged())
            {
                ForceUpdateUI();
            }

            // Đợi 0.1 giây rồi mới check lại -> Tiết kiệm hiệu năng hơn Update (chạy mỗi frame)
            // 0.1s là đủ nhanh để mắt người không nhận ra độ trễ
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void FetchBaseStats(CardNet source)
    {
        CardDataSO data = null;
        if (CardDatabase.Instance != null)
        {
            data = CardDatabase.Instance.GetCardData(source.CardID);
        }

        if (data != null)
        {
            cardImage.sprite = data.artwork;
            _baseTop = data.top;
            _baseRight = data.right;
            _baseBottom = data.bottom;
            _baseLeft = data.left;
        }
        else
        {
            _baseTop = source.Top;
            _baseRight = source.Right;
            _baseBottom = source.Bottom;
            _baseLeft = source.Left;
        }
    }

    private void ResetDisplayCache()
    {
        _displayedTop = -999;
        _displayedRight = -999;
        _displayedBottom = -999;
        _displayedLeft = -999;
    }

    private bool HasStatsChanged()
    {
        return _currentSourceCard.Top != _displayedTop ||
               _currentSourceCard.Right != _displayedRight ||
               _currentSourceCard.Bottom != _displayedBottom ||
               _currentSourceCard.Left != _displayedLeft;
    }

    private void ForceUpdateUI()
    {
        if (_currentSourceCard == null) return;

        _displayedTop = _currentSourceCard.Top;
        _displayedRight = _currentSourceCard.Right;
        _displayedBottom = _currentSourceCard.Bottom;
        _displayedLeft = _currentSourceCard.Left;

        UpdateStatUI(txtTop, modTxtTop, _displayedTop, _baseTop);
        UpdateStatUI(txtRight, modTxtRight, _displayedRight, _baseRight);
        UpdateStatUI(txtBottom, modTxtBottom, _displayedBottom, _baseBottom);
        UpdateStatUI(txtLeft, modTxtLeft, _displayedLeft, _baseLeft);
    }

    private void UpdateStatUI(TextMeshProUGUI mainText, TextMeshProUGUI modText, int currentVal, int baseVal)
    {
        if (mainText == null) return;

        mainText.text = currentVal.ToString();
        int diff = currentVal - baseVal;

        if (diff > 0)
        {
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
            mainText.color = Color.white;
            if (modText != null) modText.gameObject.SetActive(false);
        }
    }

    private IEnumerator AnimateOpenRoutine()
    {
        cardContainer.localPosition = Vector3.zero;
        cardContainer.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < appearDuration)
        {
            float t = elapsed / appearDuration;
            cardContainer.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 2f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cardContainer.localScale = Vector3.one * 2f;
    }

    private IEnumerator AnimateCloseRoutine()
    {
        Vector3 startPos = cardContainer.position;
        Vector3 startScale = cardContainer.localScale;

        Vector3 targetPos = (_currentSourceCard != null) ? _currentSourceCard.transform.position : startPos;
        Vector3 targetScale = Vector3.one;

        float elapsed = 0f;
        while (elapsed < closeDuration)
        {
            float t = elapsed / closeDuration;
            t = t * t * (3f - 2f * t);

            if (_currentSourceCard != null) targetPos = _currentSourceCard.transform.position;

            cardContainer.position = Vector3.Lerp(startPos, targetPos, t);
            cardContainer.localScale = Vector3.Lerp(startScale, targetScale, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        gameObject.SetActive(false);
        if (CanvasManager.Instance != null)
            CanvasManager.Instance.isCardFocusUIOpen = false;
        _currentAnimRoutine = null;
    }
}