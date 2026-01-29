using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    // 초기 자금 설정
    private int currentGold = 1000;
    private int tomatoCount = 0;
    public int pricePerTomato = 50;

    private void Awake()
    {
        Instance = this;
    }
    //골드 잔액 확인
    public int GetCurrentGold() => currentGold;
    /// <summary>
    /// 수확할때마다 호출될 함수
    /// </summary>
    /// <param name="amount"></param>
    public void AddGoldGFromHarvest(int amount)
    {
        currentGold += amount;

        UIManager.Instance.UpdateGoldUI(currentGold);
    }

    public bool TrySpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            UIManager.Instance.UpdateGoldUI(currentGold);
            return true;
        }
        else
        {
            Debug.Log("골드가 부족합니다.");
            return false;
        }
    }
}
