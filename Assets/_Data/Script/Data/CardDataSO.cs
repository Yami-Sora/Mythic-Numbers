using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Mythic/Card Data")]
public class CardDataSO : ScriptableObject
{
    public enum CardRate { N, R, SR, SSR }

    public int cardID;              
    public string cardName;

    [Header("Visuals")]
    public Sprite cardImage;
    public Sprite cardFrame;

    [Header("Phân Bậc (Rate)")]
    public CardRate rate = CardRate.N; // Mặc định là N (Bình thường)

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