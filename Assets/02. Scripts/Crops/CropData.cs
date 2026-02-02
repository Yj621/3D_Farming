using UnityEngine;

[CreateAssetMenu(fileName = "CropData", menuName = "Scriptable Objects/CropData")]
public class CropData : ScriptableObject
{
    public string cropName;
    public float timeBetweenStages; //단계별 성장 시간
    // 성장 단계별 프리팹 배열 (0: 씨앗, 1: 새싹, 2: 성장기, 3: 수확기)
    public GameObject[] growthStagePrefabs;
    // 해금 레벨
    public int unlockLevel;
    // 수확 시 경험치
    public int expReward;
    public int sellingPrice;
    public int purchasePrice;
}
