using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("골드 UI")]
    [SerializeField] private TextMeshProUGUI txt_Gold;
    private int currentGold = 0;

    [Header("수확 UI")]
    [SerializeField] private TextMeshProUGUI txt_Harvest;
    private int harvestCount = 0;

    private void Awake()
    {
        Instance = this;
    }


    /// <summary>
    /// 골드 추가 및 UI 갱신
    /// </summary>
    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateGoldUI(amount);
    }


    /// <summary>
    /// 수확 개수 추가 및 UI 갱신
    /// </summary>
    public void AddHarvestCount()
    {
        harvestCount++;
        UpdateHarvestUI(harvestCount);
    }

    public void UpdateGoldUI(int gold)
    {
        if (txt_Gold != null) txt_Gold.text = $"현재 돈 : {gold}";
    }

    public void UpdateHarvestUI(int count)
    {
        if (txt_Harvest != null) txt_Harvest.text = $"수확한 작물 : {count}개";
    }
}
