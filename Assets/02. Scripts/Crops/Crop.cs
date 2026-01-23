using UnityEngine;

public class Crop : MonoBehaviour
{
    public CropData data;
    private int currentState = 0;
    private GameObject currentModel;
    [SerializeField] private GameObject cropUIPrefab;
    private CropUI cropUI;

    // 수확이 가능한지 
    public bool IsFullGrown => currentState >= data.growthStagePrefabs.Length - 1;private float growthTimer = 0f; // 현재 단계에서의 경과 시간

void Update()
{
    if (IsFullGrown) return;

    // 성장 타이머 진행
    growthTimer += Time.deltaTime;
    
    // 전체 비율 계산 (현재 단계 + 현재 단계 내 진행도) / 총 단계
    // 예: 3단계 중 1단계 완료 후 2단계 50% 진행 중이면 value는 0.5
    float totalProgress = (currentState + (growthTimer / data.timeBetweenStages)) / (data.growthStagePrefabs.Length - 1);
    
    if (cropUI != null)
    {
        cropUI.UpdateProgress(totalProgress);
    }

    // 타이머가 간격에 도달하면 다음 단계로
    if (growthTimer >= data.timeBetweenStages)
    {
        Grow();
        growthTimer = 0f;
    }
}

// 기존 Grow()에서 Invoke 관련 코드 제거
void Grow()
{
    if (currentState < data.growthStagePrefabs.Length - 1)
    {
        currentState++;
        UpdateModel();
    }
}    
    
    public void Initialize(CropData cropData)
    {
        data = cropData;
        currentState = 0; // 0단계부터 시작 보장
        GameObject canvasObj = GameObject.Find("World Space Canvas");
        if (canvasObj != null && cropUIPrefab != null)
        {
            GameObject uiObj = Instantiate(cropUIPrefab, canvasObj.transform);
            cropUI = uiObj.GetComponent<CropUI>();
            cropUI.Setup(this.transform); // 나를 따라다니라고 지정
            Debug.Log("UI 생성 시도");
        }

        // 데이터가 정상인지 검사
        if (data == null || data.growthStagePrefabs == null || data.growthStagePrefabs.Length == 0)
        {
            Debug.LogError("CropData 또는 성장 프리팹 배열이 비어있습니다.");
            return;
        }

        UpdateModel();

        // Invoke 지연 시간 재확인 (0이면 실행 안됨)
        float interval = data.timeBetweenStages;
        if (interval <= 0) interval = 2.0f; // 기본값 방어 코드

        // 기존 예약된 Grow가 있다면 취소 후 재등록
        CancelInvoke(nameof(Grow));
        InvokeRepeating(nameof(Grow), interval, interval);


        Debug.Log($"{data.cropName} 성장 시작. 주기: {interval}초");
    }

    /// <summary>
    /// 수확 함수
    /// </summary>
    public void Harvest()
    {
        Debug.Log($"{data.cropName} 수확 완료!");
        Destroy(gameObject);
    }


    void UpdateModel()
    {
        if (currentModel != null) Destroy(currentModel);

        // 자식으로 생성하되, 생성된 모델의 Local Position을 0으로 맞춤
        currentModel = Instantiate(data.growthStagePrefabs[currentState], transform);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
    }
}
