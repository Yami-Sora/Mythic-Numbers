using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý việc chuyển đổi giữa các tab và panel trong Dungeon (Mine và Arena).
/// Vai trò: Điều phối (Orchestrator).
/// </summary>
public class Canvas_DungeonManager : TabListenerBase
{
    public static Canvas_DungeonManager Instance { get; private set; }

    [Header("Dungeon Tabs & Panels")]
    [SerializeField] private GameObject mineDungeonPanel;
    [SerializeField] private GameObject arenaDungeonPanel;
    [SerializeField] private GameObject dungeonTab;
    [SerializeField] private Button btnMineTab;
    [SerializeField] private Button btnArenaTab;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;

        if (btnMineTab != null) btnMineTab.onClick.AddListener(Open_Mine_Panel);
        if (btnArenaTab != null) btnArenaTab.onClick.AddListener(Open_Arena_Panel);
    }

    protected override void Start()
    {
        base.Start();
        
        // Tự tìm nếu chưa gán trong Inspector
        if (dungeonTab == null)
        {
            Transform t = transform.Find("Dungeon_Tab");
            if (t != null) dungeonTab = t.gameObject;
        }
    }

    protected override void OnTabChanged(Canvas_NavigationManager.TabType targetTab)
    {
        if (targetTab == Canvas_NavigationManager.TabType.Dungeon)
        {
            if (PlayFabDataManager.Instance != null)
            {
                if (PlayFabDataManager.Instance.CurrentMode == PlayFabDataManager.GameMode.Arena)
                {
                    Open_Arena_Panel();
                    PlayFabDataManager.Instance.CurrentMode = PlayFabDataManager.GameMode.Story; // Reset để lần sau click tab Dungeon sẽ ra menu chính
                    return;
                }
                else if (PlayFabDataManager.Instance.CurrentMode == PlayFabDataManager.GameMode.GemMine)
                {
                    Open_Mine_Panel();
                    PlayFabDataManager.Instance.CurrentMode = PlayFabDataManager.GameMode.Story; // Reset
                    return;
                }
            }

            Close_All_Panels(); // Mặc định mở menu chọn Dungeon
        }
    }

    // ===================== Tab Switching =====================

    /// <summary>Tắt tất cả panel khi mới vào tab Dungeon và hiện lại các nút chọn</summary>
    public void Close_All_Panels()
    {
        if (mineDungeonPanel) mineDungeonPanel.SetActive(false);
        if (arenaDungeonPanel) arenaDungeonPanel.SetActive(false);
        if (dungeonTab) dungeonTab.SetActive(true);
    }

    public void Open_Mine_Panel()
    {
        if (mineDungeonPanel) mineDungeonPanel.SetActive(true);
        if (arenaDungeonPanel) arenaDungeonPanel.SetActive(false);
        if (dungeonTab) dungeonTab.SetActive(false);

        if (MineDungeonManager.Instance != null)
            MineDungeonManager.Instance.OpenMine();
        else
            Debug.LogWarning("[DungeonManager] Không tìm thấy MineDungeonManager!");
    }

    public void Open_Arena_Panel()
    {
        if (mineDungeonPanel) mineDungeonPanel.SetActive(false);
        if (arenaDungeonPanel) arenaDungeonPanel.SetActive(true);
        if (dungeonTab) dungeonTab.SetActive(false);

        if (ArenaManager.Instance != null)
            ArenaManager.Instance.OpenArena();
        else
            Debug.LogWarning("[DungeonManager] Không tìm thấy ArenaManager!");
    }

    public void CloseCanvas()
    {
        if (Canvas_NavigationManager.Instance != null)
            Canvas_NavigationManager.Instance.SwitchTab(Canvas_NavigationManager.TabType.Combat);
    }
}
