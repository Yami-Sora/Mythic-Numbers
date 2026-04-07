using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CardFocusUI : YamiMonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image cardImage;
    [SerializeField] private Transform cardContainer; // Đối tượng cha chứa hình ảnh lá bài để scale

    [Header("Stats References")]
    [SerializeField] private TextMeshProUGUI txtTop, txtRight, txtBottom, txtLeft;
    [SerializeField] private TextMeshProUGUI modTxtTop, modTxtRight, modTxtBottom, modTxtLeft;

    [Header("New Info Panel References")]
    [SerializeField] private CanvasGroup infoPanelGroup; // Kéo Panel chứa Tên và Mô tả vào đây
    [SerializeField] private Transform infoPanelContainer; // Transform của Panel chứa Tên/Mô tả (để làm animation trượt)
    [SerializeField] private TextMeshProUGUI txtCardName;
    [SerializeField] private TextMeshProUGUI txtSkillDescription;

    [Header("Animation Settings")]
    [SerializeField] private float appearDuration = 0.3f;
    [SerializeField] private float closeDuration = 0.4f; // Thời gian bay về
    [SerializeField] private float slideOffset = 350f;

    private Coroutine _currentAnimRoutine;
    private Coroutine _monitoringRoutine; // Coroutine theo dõi chỉ số
    private CardNet _currentSourceCard;

    private int _baseTop, _baseRight, _baseBottom, _baseLeft;
    private int _displayedTop, _displayedRight, _displayedBottom, _displayedLeft;

    public void Show(CardNet sourceCard)
    {
        gameObject.SetActive(true);
        if (InGameUIManager.Instance != null)
            InGameUIManager.Instance.isCardFocusUIOpen = true;

        _currentSourceCard = sourceCard;

        // 1. Lấy Base Stats
        FetchBaseStats(sourceCard);

        // 2. Reset cache và Update ngay lập tức để không bị trắng thông tin frame đầu
        ResetDisplayCache();
        ForceUpdateUI();

        // 3. Bắt đầu Coroutine theo dõi thay đổi
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

            if (txtCardName != null) txtCardName.text = data.cardName;

            if (txtSkillDescription != null)
            {
                if (data.skill != null)
                    txtSkillDescription.text = data.skill.description;
                else
                    txtSkillDescription.text = "Không có kỹ năng đặc biệt.";
            }
        }
        else
        {
            _baseTop = source.Top;
            _baseRight = source.Right;
            _baseBottom = source.Bottom;
            _baseLeft = source.Left;
            if (txtCardName) txtCardName.text = "Unknown Card";
            if (txtSkillDescription) txtSkillDescription.text = "";
        }
    }

    private IEnumerator AnimateOpenRoutine()
    {
        cardContainer.localPosition = Vector3.zero;
        cardContainer.localScale = Vector3.zero;

        // Info Panel bắt đầu ẩn và nằm lệch sang phải một chút
        if (infoPanelGroup != null) infoPanelGroup.alpha = 0f;
        if (infoPanelContainer != null)
            infoPanelContainer.localPosition = new Vector3(slideOffset + 100f, 0, 0); // Lệch phải hơn đích đến 1 chút để trượt vào

        // Card trượt sang TRÁI (-X)
        Vector3 targetCardPos = new Vector3(-slideOffset * 0.8f, 0, 0); // 0.8 để nó không quá xa
        Vector3 targetCardScale = Vector3.one * 2f; // Phóng to

        // Info trượt về vị trí BÊN PHẢI (+X)
        Vector3 targetInfoPos = new Vector3(slideOffset * 0.8f, 0, 0);

        float elapsed = 0f;
        while (elapsed < appearDuration)
        {
            float t = elapsed / appearDuration;
            // Easing Out Back cho nảy nhẹ
            float tSmooth = Mathf.Sin(t * Mathf.PI * 0.5f);

            // Animate Card
            cardContainer.localScale = Vector3.Lerp(Vector3.zero, targetCardScale, tSmooth);
            cardContainer.localPosition = Vector3.Lerp(Vector3.zero, targetCardPos, tSmooth);

            // Animate Info Panel (Fade in + Slide in)
            if (infoPanelGroup != null)
                infoPanelGroup.alpha = Mathf.Lerp(0f, 1f, t * 1.5f); // Fade nhanh hơn chút

            if (infoPanelContainer != null)
                infoPanelContainer.localPosition = Vector3.Lerp(new Vector3(slideOffset + 100f, 0, 0), targetInfoPos, tSmooth);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Final set
        cardContainer.localScale = targetCardScale;
        cardContainer.localPosition = targetCardPos;
        if (infoPanelGroup) infoPanelGroup.alpha = 1f;
        if (infoPanelContainer) infoPanelContainer.localPosition = targetInfoPos;
    }

    private IEnumerator AnimateCloseRoutine()
    {
        Vector3 startCardPos = cardContainer.localPosition;
        Vector3 startCardScale = cardContainer.localScale;

        // Đích đến: Vị trí Slot trên bàn cờ
        Vector3 targetSlotPos = (_currentSourceCard != null) ? _currentSourceCard.transform.position : Vector3.zero;

        float elapsed = 0f;
        while (elapsed < closeDuration)
        {
            float t = elapsed / closeDuration;
            // Easing In Back (thu lại đà)
            t = t * t * (3f - 2f * t);

            // Card bay từ vị trí lệch trái -> Về vị trí gốc trên bàn cờ
            cardContainer.position = Vector3.Lerp(transform.TransformPoint(startCardPos), targetSlotPos, t);
            cardContainer.localScale = Vector3.Lerp(startCardScale, Vector3.one, t);

            // Info Panel Fade Out nhanh
            if (infoPanelGroup != null)
                infoPanelGroup.alpha = Mathf.Lerp(1f, 0f, t * 2f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- PHẦN POLLING & UPDATE STATS ---
        gameObject.SetActive(false);
        if (InGameUIManager.Instance != null)
            InGameUIManager.Instance.isCardFocusUIOpen = false;
        _currentAnimRoutine = null;
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

}