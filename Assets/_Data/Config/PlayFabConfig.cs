using UnityEngine;

[CreateAssetMenu(menuName = "Config/PlayFabConfig")]
public class PlayFabConfig : ScriptableObject
{
    [Tooltip("PlayFab Title ID - lấy từ PlayFab Game Manager")]
    public string titleId = "";

    public bool IsValid => !string.IsNullOrWhiteSpace(titleId);
}
