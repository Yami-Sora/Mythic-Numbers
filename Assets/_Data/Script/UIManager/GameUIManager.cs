using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Transform[] slots;
    [SerializeField] private Transform leftHandPos;
    [SerializeField] private Transform rightHandPos;
    [SerializeField] private TMP_Text turnText;

    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private Transform effectsCanvas;

    public Transform[] Slots => slots;
    public Transform LeftHandPos => leftHandPos;
    public Transform RightHandPos => rightHandPos;

    private void Awake()
    {
        Instance = this;
    }

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
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                Image img = slots[i].GetComponent<Image>();
                if (img) img.color = Color.white;
            }
        }
    }

    public void ShowFloatingText(string message, Vector3 position)
    {
        if (floatingTextPrefab == null || effectsCanvas == null) return;

        GameObject go = Instantiate(floatingTextPrefab, position, Quaternion.identity);
        // worldPositionStays = true để giữ vị trí tại chỗ lá bài
        go.transform.SetParent(effectsCanvas, true);
        go.transform.localScale = Vector3.one;  

        FloatingText ft = go.GetComponent<FloatingText>();
        if (ft != null) ft.Setup(message);
    }
}