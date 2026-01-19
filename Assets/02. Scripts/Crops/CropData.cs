using UnityEngine;

[CreateAssetMenu(fileName = "CropData", menuName = "Scriptable Objects/CropData")]
public class CropData : ScriptableObject
{
    public string cropName;
    public GameObject[] growthStagePrefabs; // 4단계 프리팹(씨앗-새싹-성장-수확)
    public float timeBetweenStages; //단계별 성장 시간
}
