using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class PlayFabCatalogExporter : EditorWindow
{
    [MenuItem("Assets/YamiTools/PlayFab/Export Items (Merge & Update CustomData)")]
    public static void ExportAndMerge()
    {
        // 1. Chọn file gốc sếp tải từ PlayFab về
        string originalPath = EditorUtility.OpenFilePanel("Chọn file JSON tải từ PlayFab (Legacy Catalog)", "", "json");
        if (string.IsNullOrEmpty(originalPath)) return;

        string originalJson = File.ReadAllText(originalPath);
        JObject root;

        try
        {
            root = JObject.Parse(originalJson);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Exporter] File JSON gốc không hợp lệ: " + e.Message);
            return;
        }

        // Lấy mảng Catalog hiện có
        JArray catalogArray = (JArray)root["Catalog"];
        if (catalogArray == null)
        {
            Debug.LogError("[Exporter] Không tìm thấy mảng 'Catalog' trong file JSON!");
            return;
        }

        // Tạo Dictionary để truy xuất nhanh Item theo ID trong JSON
        Dictionary<string, JObject> catalogDict = new Dictionary<string, JObject>();
        foreach (JObject item in catalogArray)
        {
            string id = item["ItemId"]?.ToString();
            if (!string.IsNullOrEmpty(id))
            {
                catalogDict[id] = item;
            }
        }

        // 2. Lấy danh sách ItemDataSO từ Unity
        ItemDataSO[] allItems = Resources.LoadAll<ItemDataSO>("ItemDataSO");
        int addedCount = 0;
        int updatedCount = 0;

        foreach (var item in allItems)
        {
            if (item == null || string.IsNullOrEmpty(item.itemID)) continue;

            // Tạo chuỗi CustomData mới từ Unity
            string newCustomData = null;
            if (item.type == ItemDataSO.ItemType.Gem)
            {
                var customDataObj = new
                {
                    direction = item.directionTag.ToString(),
                    bonusStat = item.bonusStat,
                    colorLevel = (int)item.colorLevel
                };
                newCustomData = JsonConvert.SerializeObject(customDataObj);
            }

            if (catalogDict.ContainsKey(item.itemID))
            {
                // --- TRƯỜNG HỢP ĐÃ CÓ: CẬP NHẬT DỮ LIỆU ---
                JObject existingItem = catalogDict[item.itemID];
                existingItem["DisplayName"] = item.itemName;
                existingItem["ItemClass"] = item.type.ToString();
                existingItem["CustomData"] = newCustomData;
                existingItem["IsStackable"] = item.type != ItemDataSO.ItemType.Gem;
                updatedCount++;
            }
            else
            {
                // --- TRƯỜNG HỢP CHƯA CÓ: THÊM MỚI ---
                JObject newItem = new JObject();
                newItem["ItemId"] = item.itemID;
                newItem["ItemClass"] = item.type.ToString();
                newItem["CatalogVersion"] = root["CatalogVersion"]?.ToString() ?? "MainCatalog";
                newItem["DisplayName"] = item.itemName;
                newItem["Description"] = null;
                newItem["VirtualCurrencyPrices"] = new JObject();
                newItem["RealCurrencyPrices"] = null;
                newItem["Tags"] = new JArray();
                newItem["CustomData"] = newCustomData;
                newItem["Consumable"] = new JObject
                {
                    ["UsageCount"] = null,
                    ["UsagePeriod"] = null,
                    ["UsagePeriodGroup"] = null
                };
                newItem["Container"] = null;
                newItem["Bundle"] = null;
                newItem["CanBecomeCharacter"] = false;
                newItem["IsStackable"] = item.type != ItemDataSO.ItemType.Gem;
                newItem["IsTradable"] = false;
                newItem["ItemImageUrl"] = null;
                newItem["IsLimitedEdition"] = false;
                newItem["InitialLimitedEditionCount"] = 0;
                newItem["ActivatedMembership"] = null;

                catalogArray.Add(newItem);
                addedCount++;
            }
        }

        // 3. Lưu lại file mới
        string savePath = EditorUtility.SaveFilePanel("Lưu file Catalog Hợp Thể", Path.GetDirectoryName(originalPath), "Merged_Catalog", "json");
        if (!string.IsNullOrEmpty(savePath))
        {
            string finalJson = root.ToString(Formatting.Indented);
            File.WriteAllText(savePath, finalJson);
            EditorUtility.DisplayDialog("Thành công!", $"Đã xử lý xong sếp ơi!\n- Thêm mới: {addedCount}\n- Cập nhật CustomData: {updatedCount}\nSếp Up lại file này lên PlayFab nhé!", "OK");
        }
    }
}
