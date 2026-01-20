using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    private int currentGold = 0;
    private int tomatoCount = 0;
    public int pricePerTomato = 50;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 수확할때마다 호출될 함수
    /// </summary>
    /// <param name="amount"></param>
   public void AddGoldGFromHarvest(int amount)
    {
        currentGold += amount;

        UIManager.Instance.UpdateGoldUI(currentGold);   
    }
}
