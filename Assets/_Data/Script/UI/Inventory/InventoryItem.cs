using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class InventoryItem
{
    public ItemDataSO data; // Chứa Icon, Tên, Loại...
    public int amount;      // Số lượng đang có

    public InventoryItem(ItemDataSO data, int amount)
    {
        this.data = data;
        this.amount = amount;
    }
}