using System;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class AuthService
{
    private readonly PlayFabConfig _config;
    private const string DeviceIdKey = "PlayFabDeviceId";
    public string UserId => PlayFab.PlayFabSettings.staticPlayer.PlayFabId;

    public AuthService(PlayFabConfig config)
    {
        _config = config;
    }

    // Kiểm tra đã đăng nhập chưa thông qua SDK
    public bool IsLoggedIn => PlayFabClientAPI.IsClientLoggedIn();

    public void LoginAnonymous(Action onSuccess, Action<string> onError)
    {
        if (!_config.IsValid)
        {
            onError?.Invoke("Title ID chưa được cấu hình trong PlayFabConfig.");
            return;
        }

        var request = new LoginWithCustomIDRequest
        {
            CustomId = GetOrCreateDeviceId(),
            CreateAccount = true
        };

        // Gọi API bằng SDK chuẩn
        PlayFabClientAPI.LoginWithCustomID(request,
            result => {
                Debug.Log($"[PlayFab] Đăng nhập OK. PlayFabId: {result.PlayFabId}");
                onSuccess?.Invoke();
            },
            error => {
                string msg = error.GenerateErrorReport();
                Debug.LogWarning($"[PlayFab] Lỗi đăng nhập: {msg}");
                onError?.Invoke(msg);
            }
        );
    }

    public System.Threading.Tasks.Task<bool> LoginAnonymousAsync()
    {
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        LoginAnonymous(
            () => tcs.TrySetResult(true),
            err => {
                Debug.LogWarning($"[PlayFab] LoginAnonymousAsync failed: {err}");
                tcs.TrySetResult(false);
            }
        );
        return tcs.Task;
    }

    private static string GetOrCreateDeviceId()
    {
        string id = PlayerPrefs.GetString(DeviceIdKey, "");
        if (string.IsNullOrEmpty(id))
        {
            // Code cũ: dùng device ID - không dùng được khi test trên cùng thiết bị
            // id = SystemInfo.deviceUniqueIdentifier;
            // if (string.IsNullOrEmpty(id) || id == "n/a")
            //     id = Guid.NewGuid().ToString();
            
            // Dùng GUID để mỗi lần test là player mới
            id = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(DeviceIdKey, id);
        }
        return id;
    }

    public static void ResetDeviceId()
    {
        PlayerPrefs.DeleteKey(DeviceIdKey);
    }
}