using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Launcher UI References")]
    [SerializeField] private Image LeftAiImage;
    [SerializeField] private Image LeftPlayer2;
    [SerializeField] private GameObject btnRule;
    [SerializeField] private Image RulePanel;
    [SerializeField] private TMP_Text RuleName;
    [SerializeField] private TMP_Text RuleDes;

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
            RulePanel.gameObject.SetActive(true);
            this.UpdateRulePanelText();
        }
    }
    public void OnCloseRulePanelClicked()
    {
        if (RulePanel != null)
        {
            RulePanel.gameObject.SetActive(false);
        }
    }
    public void UpdateRulePanelText()
    {
        if (RuleName != null)
        {
            IRuleSet ruleSet = GameManagerNet.Instance.GetCurrentRule();
            RuleName.text = ruleSet.RuleName;
            RuleDes.text = ruleSet.RuleDescription;
        }
    }
}