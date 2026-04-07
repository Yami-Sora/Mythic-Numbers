using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using Fusion;

public class InGameUIManager : MonoBehaviour
{
    public static InGameUIManager Instance { get; private set; }

    [Header("--- BOARD & HANDS ---")]
    [SerializeField] private Transform[] slots;
    [SerializeField] private Transform leftHandPos;
    [SerializeField] private Transform rightHandPos;
    public Transform[] Slots => slots;
    public Transform LeftHandPos => leftHandPos;
    public Transform RightHandPos => rightHandPos;

    [Header("--- PLAYER INFO & TURN ---")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private Image leftAiImage;
    [SerializeField] private Image leftPlayer2;

    [Header("--- RULE PANEL ---")]
    [SerializeField] private GameObject btnRule;
    [SerializeField] private GameObject rulePanel;
    [SerializeField] private TMP_Text baseRuleName;
    [SerializeField] private TMP_Text baseRuleDes;
    [SerializeField] private TMP_Text subRuleName;
    [SerializeField] private TMP_Text subRuleDes;

    [Header("--- POPUPS & OVERLAYS ---")]
    [SerializeField] private CardFocusUI cardFocusPanel;
    [SerializeField] private GameObject reconnectPanel;
    [SerializeField] private TMP_Text reconnectText;
    public bool isCardFocusUIOpen = false;

    [Header("--- EFFECTS (FLOATING TEXT) ---")]
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private Transform effectsCanvas;
    private Queue<GameObject> _textPool = new Queue<GameObject>();

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
        // 1. Check ẩn hiện Avatar đối thủ (AI vs Người)
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null)
        {
            bool isSinglePlayer = (runner.GameMode == GameMode.Single);
            if (leftAiImage != null) leftAiImage.gameObject.SetActive(isSinglePlayer);
            if (leftPlayer2 != null) leftPlayer2.gameObject.SetActive(!isSinglePlayer);
        }

        // 2. Tắt các Panel râu ria lúc mới vào game
        if (reconnectPanel != null) reconnectPanel.SetActive(false);
        if (rulePanel != null) rulePanel.SetActive(false);
    }

    // =========================================================
    // HÀM XỬ LÝ BOARD & THÔNG TIN LƯỢT
    // =========================================================

    public void SetupReferences(Transform[] slots, Transform left, Transform right, TMP_Text turnTxt, Transform canvas)
    {
        this.slots = slots;
        this.leftHandPos = left;
        this.rightHandPos = right;
        this.turnText = turnTxt;
        this.effectsCanvas = canvas;
    }

    public void UpdateTurnText(int localPlayerId, int currentTurn, string ruleName)
    {
        if (turnText == null) return;

        string ruleDisplay = string.IsNullOrEmpty(ruleName) ? "" : $"[{ruleName}]";
        if (localPlayerId == 0)
            turnText.text = (currentTurn == 0) ? $"Lượt của bạn (Blue) {ruleDisplay}" : $"Lượt đối thủ (Red) {ruleDisplay}";
        else
            turnText.text = (currentTurn == 0) ? $"Lượt đối thủ (Blue) {ruleDisplay}" : $"Lượt của bạn (Red) {ruleDisplay}";
    }

    public void ResetBoardUI()
    {
        if (slots == null) return;
        foreach (var slot in slots)
        {
            if (slot != null)
            {
                Image img = slot.GetComponent<Image>();
                if (img) img.color = Color.white;
            }
        }
    }

    // =========================================================
    // HÀM XỬ LÝ RULE & POPUP
    // =========================================================

    public void OnRuleButtonClicked()
    {
        if (rulePanel != null)
        {
            GameManagerNet.Instance.SetCurrenRule();
            rulePanel.SetActive(true);
        }
    }

    public void OnCloseRulePanelClicked()
    {
        if (rulePanel != null) rulePanel.SetActive(false);
    }

    public void UpdateRulePanelText(string baseName, string subName, string baseDesc, string subDesc)
    {
        if (baseRuleName != null) baseRuleName.text = baseName;
        if (baseRuleDes != null) baseRuleDes.text = baseDesc;
        if (subRuleName != null) subRuleName.text = subName;
        if (subRuleDes != null) subRuleDes.text = subDesc;
    }

    public void ShowCardFocus(CardNet card)
    {
        if (cardFocusPanel != null) cardFocusPanel.Show(card);
    }

    public void ShowReconnectMessage(bool show, string message)
    {
        if (reconnectPanel == null) return;
        reconnectPanel.SetActive(show);
        if (reconnectText != null) reconnectText.text = message;
    }

    // =========================================================
    // HÀM XỬ LÝ EFFECT TEXT BAY
    // =========================================================

    public void ShowFloatingText(string message, Vector3 position)
    {
        if (floatingTextPrefab == null || effectsCanvas == null) return;

        GameObject go;
        if (_textPool.Count > 0)
        {
            go = _textPool.Dequeue();
            go.SetActive(true);
        }
        else
        {
            go = Instantiate(floatingTextPrefab, Vector3.zero, Quaternion.identity);
            go.transform.SetParent(effectsCanvas, true);
        }

        go.transform.position = position;
        go.transform.localScale = Vector3.one;

        FloatingText ft = go.GetComponent<FloatingText>();
        if (ft != null) ft.Setup(message, ReturnTextToPool);
    }

    private void ReturnTextToPool(GameObject textObj)
    {
        textObj.SetActive(false);
        _textPool.Enqueue(textObj);
    }
}