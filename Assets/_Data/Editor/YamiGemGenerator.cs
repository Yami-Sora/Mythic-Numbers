using UnityEngine;
using UnityEditor; // Thư viện này chỉ chạy trong lúc sếp làm game, build ra sẽ tự hủy nên không sợ nặng máy

public class YamiGemGenerator
{
    // Tạo 1 cái nút bấm ngay menu chuột phải
    [MenuItem("Assets/YamiTools/🌟 Đúc Full 7 Cấp Ngọc")]
    public static void GenerateGemChain()
    {
        // Lấy cái file SO mà sếp đang chọn
        ItemDataSO baseGem = Selection.activeObject as ItemDataSO;

        if (baseGem == null || baseGem.type != ItemDataSO.ItemType.Gem)
        {
            Debug.LogWarning("Sếp phải click chọn 1 file ItemDataSO là Ngọc (Gem) màu Trắng mới đúc được!");
            return;
        }

        string path = AssetDatabase.GetAssetPath(baseGem);
        string folder = path.Substring(0, path.LastIndexOf('/'));

        ItemDataSO currentGem = baseGem;

        // (Chạy từ màu hiện tại đến màu số 7 - Đỏ)
        for (int i = (int)baseGem.colorLevel + 1; i <= 7; i++)
        {
            ColorLv nextColor = (ColorLv)i;
            string newFileName = $"{baseGem.name}_{nextColor}";
            string newPath = $"{folder}/{newFileName}.asset";

            // Nhờ Unity Copy cái file hiện tại ra file mới
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(currentGem), newPath);
            AssetDatabase.Refresh();

            // Mở cái file vừa Copy ra để chỉnh sửa thông số
            ItemDataSO nextGem = AssetDatabase.LoadAssetAtPath<ItemDataSO>(newPath);

            // Tự động set thông tin
            nextGem.itemID = $"{baseGem.itemID}_{nextColor}";
            nextGem.itemName = $"{baseGem.itemName} [{nextColor}]";
            nextGem.colorLevel = nextColor;
            nextGem.bonusStat = currentGem.bonusStat * 2; // Ví dụ: Cấp sau chỉ số x2 cấp trước (Sếp tự sửa số này tùy ý)
            nextGem.nextLevelGem = null; // Xóa dây cũ

            // Nối file hiện tại vào file mới
            currentGem.nextLevelGem = nextGem;

            // Lưu lại
            EditorUtility.SetDirty(currentGem);
            EditorUtility.SetDirty(nextGem);

            // Chuyển sang file tiếp theo để lặp lại
            currentGem = nextGem;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"<color=green>Ầm ầm! Lò rèn đã đúc xong bộ dây chuyền ngọc từ {baseGem.itemName}! Sếp check lại thư mục nhé!</color>");
    }
}