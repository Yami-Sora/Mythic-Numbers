using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Mythic/Card Data")]
public class CardDataSO : ScriptableObject
{
    public int id;              
    public string cardName;
    public Sprite artwork; 

    [Header("Stats")]
    [Range(1, 1000)] public int top;
    [Range(1, 1000)] public int right;
    [Range(1, 1000)] public int bottom;
    [Range(1, 1000)] public int left;

    public BaseSkillSO skill;

    // add vào đây các lỗ mà thẻ này có. 
    // VD Thẻ cùi có 1 lỗ Top. Thẻ VIP có đủ 4 lỗ Top, Bottom, Left, Right.
    [Header("Hệ Thống Lỗ Khảm (Sockets)")]
    public List<GemDirection> availableSockets;
}