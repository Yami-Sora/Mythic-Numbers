using UnityEngine;
using UnityEditor;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class PlayFabBotGenerator : EditorWindow
{
    private int botCount = 10;
    private int startingElo = 100;
    private string botNamePrefix = "Bot_Server1_";
    private bool isRunning = false;
    private int processedCount = 0;

    [MenuItem("Mythic Tools/PlayFab Bot Generator")]
    public static void ShowWindow()
    {
        GetWindow<PlayFabBotGenerator>("Bot Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Cấu Hình Máy Đẻ Bot", EditorStyles.boldLabel);
        botCount = EditorGUILayout.IntField("Số lượng Bot:", botCount);
        startingElo = EditorGUILayout.IntField("Điểm Elo khởi đầu:", startingElo);
        botNamePrefix = EditorGUILayout.TextField("Tiền tố tên Bot:", botNamePrefix);

        if (isRunning)
        {
            GUILayout.Label($"Đang đẻ Bot... ({processedCount}/{botCount})");
            if (GUILayout.Button("Dừng lại")) isRunning = false;
        }
        else
        {
            if (GUILayout.Button("Bắt Đầu Đẻ Bot!"))
            {
                processedCount = 0;
                isRunning = true;
                CreateNextBot();
            }
        }
    }

    private void CreateNextBot()
    {
        if (!isRunning || processedCount >= botCount)
        {
            isRunning = false;
            EditorUtility.DisplayDialog("Xong!", $"Đã đẻ xong {processedCount} con Bot lên PlayFab!", "Ngon");
            return;
        }

        string customId = botNamePrefix + System.Guid.NewGuid().ToString().Substring(0, 8);
        string displayName = botNamePrefix + (processedCount + 1);

        // Bước 1: Đăng ký/Đăng nhập CustomID
        var loginRequest = new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = true,
            TitleId = PlayFabSettings.staticSettings.TitleId
        };

        PlayFabClientAPI.LoginWithCustomID(loginRequest, loginResult => {
            // Bước 2: Đặt tên hiển thị
            PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest {
                DisplayName = displayName
            }, nameResult => {
                // Bước 3: Set điểm Elo
                PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest {
                    Statistics = new List<StatisticUpdate> {
                        new StatisticUpdate { StatisticName = "elo", Value = startingElo }
                    }
                }, statResult => {
                    Debug.Log($"<color=green>[BotGen]</color> Đã tạo thành công: {displayName}");
                    processedCount++;
                    CreateNextBot(); // Đẻ con tiếp theo
                }, error => OnError(error));
            }, error => OnError(error));
        }, error => OnError(error));
    }

    private void OnError(PlayFabError error)
    {
        Debug.LogError($"[BotGen] Lỗi tại con Bot thứ {processedCount + 1}: " + error.ErrorMessage);
        isRunning = false;
    }
}
