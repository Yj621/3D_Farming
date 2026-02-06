using UnityEngine;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    [Header("아이템 설정")]
    public CropData cropData; // 토마토 데이터 등 (밭일 경우 비워둠)
    public bool isMud;        // 밭 아이템이라면 체크

    [Header("해금 설정")]
    public int unlockLevel;   // 해금 레벨
    public GameObject lockObject; // 자물쇠 이미지

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        // 레벨업 이벤트 구독
        if (ExpManager.Instance != null)
            ExpManager.Instance.OnLevelUp += OnLevelUp;

        RefreshUI();
    }
    private void OnDisable()
    {
        // 반드시 해제
        if (ExpManager.Instance != null)
            ExpManager.Instance.OnLevelUp -= OnLevelUp;
    }

    private void Start()
    {
        button.onClick.AddListener(OnClickItem);
        RefreshUI();
    }

    private void OnLevelUp(int newLevel)
    {
        RefreshUI();
    }
    public void RefreshUI()
    {
        // 경험치 매니저의 레벨을 체크하여 잠금 처리
        bool isLocked = ExpManager.Instance.currentLevel < unlockLevel;
        if (lockObject != null)
            lockObject.SetActive(isLocked);

        button.interactable = !isLocked;
    }

    public void OnClickItem()
    {
        // GridSystem의 함수를 직접 호출 (매개변수 2개 전달 가능)
        GridSystem.Instance.SelectItemFromShop(cropData, isMud);
    }
}
