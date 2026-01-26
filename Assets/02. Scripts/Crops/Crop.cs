using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crop : MonoBehaviour
{
    public CropData data;
    private int currentState = 0;
    private bool hasPlayedFullGrownEffect = false; // 파티클 중복 생성 방지용
    // 생성된 모델들을 미리 담아둘 리스트 (풀링)
    private List<GameObject> instantiatedModels = new List<GameObject>();

    [SerializeField] private CropUI cropUI;
    private Transform myTransform; // 트랜스포머 캐싱

    [SerializeField] private GameObject growthParticlePrefab;

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
        if (IsFullGrown)
        {
            if (!hasPlayedFullGrownEffect)
            {
                PlayGrowthParticle();
                hasPlayedFullGrownEffect = true;

                // UI 제거 로직을 이쪽으로 이동
                if (cropUI != null)
                {
                    Destroy(cropUI.gameObject);
                    cropUI = null;
                }
            }
            return;
        }
        // 성장 타이머 진행
        growthTimer += Time.deltaTime;

        // 전체 비율 계산 (현재 단계 + 현재 단계 내 진행도) / 총 단계
        // 예: 3단계 중 1단계 완료 후 2단계 50% 진행 중이면 value는 0.5
        float totalProgress = (currentState + (growthTimer / data.timeBetweenStages)) / (data.growthStagePrefabs.Length - 1);

        if (cropUI != null)
        {
            cropUI.SetFillAmount(totalProgress); 
            if(totalProgress >= 1f)
            {
                Destroy(cropUI.gameObject);
                cropUI = null;
            }
        }

        // 타이머가 간격에 도달하면 다음 단계로
        if (growthTimer >= data.timeBetweenStages)
        {
            Grow();
            growthTimer = 0f;
            if(IsFullGrown && cropUI != null)
            {
                Destroy(cropUI.gameObject);
                cropUI = null;
            }    
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

    void PlayGrowthParticle()
    {
        // 생성할 위치 설정 (작물 머리 위로 0.5f만큼 올림)
        Vector3 spawnPos = myTransform.position;

        // 원하는 회전값 설정 (-90, 0, 0)
        // Quaternion.Euler를 사용하면 우리가 아는 도(Degree) 단위 각도를 쿼터니언으로 변환
        Quaternion spawnRot = Quaternion.Euler(-90f, 0f, 0f);
        Instantiate(growthParticlePrefab, spawnPos, spawnRot);
    }

    /// <summary>
    /// 농작물의 초기 설정을 담당하는 함수
    /// 데이터 할당, UI 생성, 모델 풀링(미리 생성)을 수행
    /// </summary>
    public void Initialize(CropData cropData)
    {
        data = cropData; // 전달받은 작물 정보를 데이터 변수에 저장
        currentState = 0; // 성장 단계를 0(초기 상태)으로 초기화
        hasPlayedFullGrownEffect = false;

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
            bool isCurrent = (i == currentState);
            instantiatedModels[i].SetActive(isCurrent);

            if (isCurrent)
            {
                StopAllCoroutines();
                StartCoroutine(ScaleUpEffect(instantiatedModels[i].transform));
            }
        }
    }

    IEnumerator ScaleUpEffect(Transform target)
    {
        //연출시간
        float duration = 0.3f;
        float timer = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            //서서히 커지는 효과
            target.localScale = Vector3.Lerp(startScale, endScale, timer / duration);
            yield return null;
        }
        target.localScale = endScale;
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
