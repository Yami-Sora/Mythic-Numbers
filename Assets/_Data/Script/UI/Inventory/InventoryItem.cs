using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public ItemDataSO data;
    public int amount;    

    public InventoryItem(ItemDataSO data, int amount)
    {
        this.data = data;
        this.amount = amount;
    }
}