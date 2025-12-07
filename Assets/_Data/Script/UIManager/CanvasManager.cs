using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    public static CanvasManager Instance { get; private set; }

    [Header("Launcher UI References")]
    [SerializeField] private Image LeftAiImage;
    [SerializeField] private Image LeftPlayer2;
    [SerializeField] private GameObject btnRule;
    [SerializeField] private Image RulePanel;

    [Header("Base Rule References")]
    [SerializeField] private TMP_Text BaseRuleName;
    [SerializeField] private TMP_Text BaseRuleDes;

    [Header("Sub Rule References")]
    [SerializeField] private TMP_Text SubRuleName;
    [SerializeField] private TMP_Text SubRuleDes;

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
        // Tìm NetworkRunner đang chạy (Nó được truyền từ Menu sang)
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();

        if (runner != null)
        {
            if (runner.GameMode == GameMode.Single)
            {
                SetLeftImageActive(true); // Bật ảnh AI
            }
            else
            {
                SetLeftImageActive(false); // Bật ảnh Player 2 (Online)
            }
        }
    }
    public void SetLeftImageActive(bool isActive)
    {
        if (LeftAiImage != null)
        {
            LeftAiImage.gameObject.SetActive(isActive);
        }
        if (LeftPlayer2 != null)
        {
            LeftPlayer2.gameObject.SetActive(!isActive);
        }
    }
    public void OnRuleButtonClicked()
    {
        if (RulePanel != null)
        {
            GameManagerNet.Instance.SetCurrenRule();
            RulePanel.gameObject.SetActive(true);
        }
    }
    public void OnCloseRulePanelClicked()
    {
        if (RulePanel != null)
        {
            RulePanel.gameObject.SetActive(false);
        }
    }
    public void UpdateRulePanelText(string baseRuleNameStr, string subRuleNameStr,
                                    string baseRuleDescStr, string subRuleDescStr)
    {
        if (BaseRuleName != null) BaseRuleName.text = baseRuleNameStr;
        if (BaseRuleDes != null) BaseRuleDes.text = baseRuleDescStr;

        if (SubRuleName != null) SubRuleName.text = subRuleNameStr;
        if (SubRuleDes != null) SubRuleDes.text = subRuleDescStr;
    }
}