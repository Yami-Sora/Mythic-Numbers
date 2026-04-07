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
}