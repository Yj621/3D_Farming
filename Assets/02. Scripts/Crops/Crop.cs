using UnityEngine;

public class Crop : MonoBehaviour
{
    private CropData data;
    private int currentState = 0;
    private GameObject currentModel;

    public void Initialize(CropData cropData)
    {
        data = cropData;
        currentState = 0; // 0단계부터 시작 보장

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

    void Grow()
    {
        if (currentState < data.growthStagePrefabs.Length - 1)
        {
            currentState++;
            UpdateModel();
            Debug.Log($"{gameObject.name}가 {currentState}단계로 성장했습니다.");
        }
        else
        {
            CancelInvoke("Grow");
        }
    }
    
    void UpdateModel()
    {
        if(currentModel != null) Destroy(currentModel);

        // 자식으로 생성하되, 생성된 모델의 Local Position을 0으로 맞춤
        currentModel = Instantiate(data.growthStagePrefabs[currentState], transform);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
    }
}
