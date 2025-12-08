using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class FusionObjectPool : Fusion.Behaviour, INetworkObjectProvider
{
    [InlineHelp]
    public bool DelayIfSceneManagerIsBusy = true;

    private Dictionary<NetworkPrefabId, Queue<NetworkObject>> _free = new Dictionary<NetworkPrefabId, Queue<NetworkObject>>();

    // Số lượng object tối đa được giữ trong mỗi pool. 
    // 0 hoặc số âm nghĩa là pool tất cả các object được giải phóng (không giới hạn).
    [SerializeField]
    private int _maxPoolCount = 0;

    protected NetworkObject InstantiatePrefab(NetworkRunner runner, NetworkObject prefab, NetworkPrefabId contextPrefabId)
    {
        var result = default(NetworkObject);

        // Tìm hàng đợi (Queue) rảnh cho prefab này VÀ hàng đợi không được rỗng.
        // Nếu có, trả về object rảnh để tái sử dụng.
        if (_free.TryGetValue(contextPrefabId, out var freeQ))
        {
            if (freeQ.Count > 0)
            {
                result = freeQ.Dequeue();
                result.gameObject.SetActive(true);
                Debug.Log($"<color=green>♻️ [POOL] Đã tái sử dụng: {result.name}</color>");
                return result;
            }
        }
        else
        {
            _free.Add(contextPrefabId, new Queue<NetworkObject>());
        }

        // -- Tại thời điểm này, hàng đợi rảnh chưa được tạo hoặc đang rỗng. 
        // Tạo (Instantiate) object mới hoàn toàn.
        result = Instantiate(prefab);
        Debug.Log($"<color=yellow>⭐️ [POOL] Phải tạo mới tinh: {result.name}</color>");

        return result;
    }

    protected void DestroyPrefabInstance(NetworkRunner runner, NetworkPrefabId prefabId, NetworkObject instance)
    {
        if (_free.TryGetValue(prefabId, out var freeQ) == false)
        {
            // Không có hàng đợi rảnh cho prefab này. Nên hủy bỏ (Destroy) luôn.
            Destroy(instance.gameObject);
            return;
        }
        else if (_maxPoolCount > 0 && freeQ.Count >= _maxPoolCount)
        {
            // Pool đã đạt số lượng object tối đa đã định nghĩa. Nên hủy bỏ để tiết kiệm bộ nhớ.
            Destroy(instance.gameObject);
            return;
        }

        // Tìm thấy hàng đợi rảnh. Nên cache (lưu trữ) lại.
        freeQ.Enqueue(instance);

        // Làm cho object ngừng hoạt động (inactive) thay vì Destroy.
        instance.gameObject.SetActive(false);
    }

    public NetworkObjectAcquireResult AcquirePrefabInstance(NetworkRunner runner, in NetworkPrefabAcquireContext context,
        out NetworkObject instance)
    {

        instance = null;

        if (DelayIfSceneManagerIsBusy && runner.SceneManager.IsBusy)
        {
            return NetworkObjectAcquireResult.Retry;
        }

        NetworkObject prefab;
        try
        {
            prefab = runner.Prefabs.Load(context.PrefabId, isSynchronous: context.IsSynchronous);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load prefab: {ex}");
            return NetworkObjectAcquireResult.Failed;
        }

        if (!prefab)
        {
            return NetworkObjectAcquireResult.Retry;
        }

        instance = InstantiatePrefab(runner, prefab, context.PrefabId);
        Assert.Check(instance);

        if (context.DontDestroyOnLoad)
        {
            runner.MakeDontDestroyOnLoad(instance.gameObject);
        }
        else
        {
            runner.MoveToRunnerScene(instance.gameObject);
        }

        runner.Prefabs.AddInstance(context.PrefabId);
        return NetworkObjectAcquireResult.Success;
    }

    public void ReleaseInstance(NetworkRunner runner, in NetworkObjectReleaseContext context)
    {
        var instance = context.Object;

        // Chỉ pool các prefab.
        if (!context.IsBeingDestroyed)
        {
            if (context.TypeId.IsPrefab)
            {
                DestroyPrefabInstance(runner, context.TypeId.AsPrefabId, instance);
            }
            else
            {
                Destroy(instance.gameObject);
            }
        }

        if (context.TypeId.IsPrefab)
        {
            runner.Prefabs.RemoveInstance(context.TypeId.AsPrefabId);
        }
    }

    public void SetMaxPoolCount(int count)
    {
        _maxPoolCount = count;
    }

    public NetworkPrefabId GetPrefabId(NetworkRunner runner, NetworkObjectGuid prefabGuid)
    {
        return runner.Prefabs.GetId(prefabGuid);
    }
}