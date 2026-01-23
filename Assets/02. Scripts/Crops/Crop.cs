using System.Collections.Generic;
using UnityEngine;

public class Crop : MonoBehaviour
{
    public CropData data;
    private int currentState = 0;

    // 생성된 모델들을 미리 담아둘 리스트 (풀링)
    private List<GameObject> instantiatedModels = new List<GameObject>();

    [SerializeField] private GameObject cropUIPrefab;
    private CropUI cropUI;
    private Transform myTransform; // 트랜스포머 캐싱

    // 수확이 가능한지 
    public bool IsFullGrown => currentState >= data.growthStagePrefabs.Length - 1; 
    private float growthTimer = 0f; // 현재 단계에서의 경과 시간

    private void Awake()
    {
        // 내 transform을 미리 캐싱하여 get_transform 호출 최소화
        myTransform = transform;
    }

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

    /// <summary>
    /// 농작물의 초기 설정을 담당하는 함수
    /// 데이터 할당, UI 생성, 모델 풀링(미리 생성)을 수행
    /// </summary>
    public void Initialize(CropData cropData)
    {
        data = cropData; // 전달받은 작물 정보를 데이터 변수에 저장
        currentState = 0; // 성장 단계를 0(초기 상태)으로 초기화

        if (cropUI == null)
        {
            GameObject canvasObj = GameObject.Find("World Space Canvas");

            if (canvasObj != null && cropUIPrefab != null)
            {
                // UI 프리팹을 캔버스의 자식으로 생성
                GameObject uiObj = Instantiate(cropUIPrefab, canvasObj.transform);
                // 생성된 UI 객체에서 CropUI 컴포넌트를 가져오기
                cropUI = uiObj.GetComponent<CropUI>();
                // UI가 현재 작물(myTransform)을 따라다니도록 타겟을 지정
                cropUI.Setup(myTransform);
            }
        }

        // 성장에 필요한 모든 모델을 게임 시작(초기화) 시점에 미리 생성
        PreInstantiateModels();

        // 현재 단계(currentState)에 맞는 모델만 화면에 보이게 설정
        UpdateModel();
    }

    /// <summary>
    /// 모든 성장 단계별 모델을 미리 인스턴스화(Instantiate)하여 리스트에 저장
    /// </summary>
    void PreInstantiateModels()
    {
        // 혹시 이미 리스트에 모델이 있다면 메모리 정리를 위해 파괴하고 리스트를 비움
        foreach (var m in instantiatedModels) if (m != null) Destroy(m);
        instantiatedModels.Clear();

        //  CropData에 등록된 성장 단계별 프리팹 개수만큼 반복문
        for (int i = 0; i < data.growthStagePrefabs.Length; i++)
        {
            // 모델을 현재 작물의 자식(myTransform)으로 생성
            GameObject go = Instantiate(data.growthStagePrefabs[i], myTransform);

            // 생성된 모델의 위치와 회전값을 부모(작물) 위치에 맞게 초기화
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            // 일단은 모든 모델을 비활성화(꺼둠) 상태
            go.SetActive(false);

            // 생성된 모델 객체를 리스트에 순서대로 담기
            instantiatedModels.Add(go);
        }
    }

    /// <summary>
    /// 현재 성장 단계(currentState)와 일치하는 모델만 활성화하고 나머지는 비활성화
    /// </summary>
    void UpdateModel()
    {
        // 미리 생성해둔 모델 리스트를 처음부터 끝까지 확인
        for (int i = 0; i < instantiatedModels.Count; i++)
        {
            // 리스트의 인덱스(i)가 현재 성장 단계(currentState)와 같으면 true, 아니면 false를 전달
            // 결과적으로 i번째 모델만 SetActive(true)가 되고 나머지는 모두 꺼짐
            instantiatedModels[i].SetActive(i == currentState);
        }
    }

    /// <summary>
    /// 수확 함수
    /// </summary>
    public void Harvest()
    {
        Debug.Log($"{data.cropName} 수확 완료!");
        Destroy(gameObject);
    }



}
