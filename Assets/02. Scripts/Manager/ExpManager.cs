using System;
using UnityEngine;

public class ExpManager : MonoBehaviour
{
    public static ExpManager Instance;

    [Header("레벨 설정")]
    public int currentLevel = 1;
    [SerializeField] private int currentEXP = 0;
    [SerializeField] private int expToNextLevel = 100;

    public event Action<int> OnLevelUp;
    private void Awake()
    {
        Instance = this;
    }

    public void AddExp(int amount)
    {
        currentEXP += amount;
        if (currentEXP >= expToNextLevel)
        {
            LevelUp();
        }
        // TODO : UI 업데이트 호출
    }
    public void DebugLevelUp()
    {
        LevelUp();
    }

    public void LevelUp()
    {
        currentEXP -= expToNextLevel;
        currentLevel++;
        // 필요 경험치 증가
        expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.5f);

        //레벨업 이벤트 호출
        OnLevelUp?.Invoke(currentLevel);
    }
}
