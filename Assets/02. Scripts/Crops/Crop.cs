using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crop : MonoBehaviour, IPoolable
{
    public CropData data;

    public CropUI cropUI;
    public CropUI UI => cropUI;


    [Header("파티클")]
    [SerializeField] private ParticleSystem fullGrownParticle; // Crop 자식에 붙어있는 ParticleSystem
    private bool hasPlayedFullGrownEffect = false;

    // 모델 풀링(성장 단계 프리팹 미리 생성)
    private readonly List<GameObject> instantiatedModels = new List<GameObject>();

    private Transform myTransform;
    private int currentState = 0;
    private float growthTimer = 0f;

    public bool IsFullGrown => data != null && currentState >= data.growthStagePrefabs.Length - 1;


    private void Awake()
    {
        myTransform = transform;

        // 파티클은 기본 꺼두는 걸 권장
        if (fullGrownParticle != null)
            fullGrownParticle.gameObject.SetActive(false);

        if (cropUI == null) cropUI = GetComponentInChildren<CropUI>(true);

    }

    void Update()
    {
        if (data == null) return;

        if (IsFullGrown)
        {
            if (!hasPlayedFullGrownEffect)
            {
                PlayFullGrownParticle();
                hasPlayedFullGrownEffect = true;
            }
            return;

        }
        // 성장 타이머 진행
        growthTimer += Time.deltaTime;

        // 전체 비율 계산 (현재 단계 + 현재 단계 내 진행도) / 총 단계
        // 예: 3단계 중 1단계 완료 후 2단계 50% 진행 중이면 value는 0.5
        float totalProgress =
                 (currentState + (growthTimer / data.timeBetweenStages)) /
                 (data.growthStagePrefabs.Length - 1);

        if (cropUI != null)
        {
            cropUI.SetFillAmount(totalProgress);
        }


        if (growthTimer >= data.timeBetweenStages)
        {
            Grow();
            growthTimer = 0f;
        }
    }

    // 성장 진행률 업데이트도 여기로 통일
    public void UpdateProgress(float t01)
    {
        cropUI?.SetFillAmount(t01);
    }


    void Grow()
    {
        if (currentState < data.growthStagePrefabs.Length - 1)
        {
            currentState++;
            UpdateModel();
        }
    }

    void PlayFullGrownParticle()
    {
        if (fullGrownParticle == null) return;

        // Crop 자식이라 위치/회전은 프리팹에서 잡아두는 게 제일 깔끔
        fullGrownParticle.gameObject.SetActive(true);
        fullGrownParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        fullGrownParticle.Play(true);
    }


    /// <summary>
    /// 농작물의 초기 설정을 담당하는 함수
    /// 데이터 할당, UI 생성, 모델 풀링(미리 생성)을 수행
    /// </summary>
    public void Initialize(CropData cropData)
    {
        // UI 자동 참조(인스펙터에 안 꽂아도 되게)
        if (cropUI == null)
            cropUI = GetComponentInChildren<CropUI>(true);

        // 심을 때 UI 다시 켜고 0으로 리셋
        if (cropUI != null)
        {
            cropUI.gameObject.SetActive(true);
            cropUI.SetFillAmount(0f);
            data = cropData; // 전달받은 작물 정보를 데이터 변수에 저장
            currentState = 0; // 성장 단계를 0(초기 상태)으로 초기화
            growthTimer = 0f;
            hasPlayedFullGrownEffect = false;

            // 파티클 리셋
            ResetParticle();

            if (cropUI == null)
                cropUI = GetComponentInChildren<CropUI>(true);

            if (cropUI != null)
            {
                cropUI.gameObject.SetActive(true);
                cropUI.SetFillAmount(0f);
            }


            // 성장에 필요한 모든 모델을 게임 시작(초기화) 시점에 미리 생성
            PreInstantiateModels();

            // 현재 단계(currentState)에 맞는 모델만 화면에 보이게 설정
            UpdateModel();
        }
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
                go.transform.localScale = Vector3.one;

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
                var model = instantiatedModels[i];
                if (model == null) continue;

                bool isCurrent = (i == currentState);

                // 현재 단계만 보이게 처리
                model.SetActive(isCurrent);

                if (isCurrent)
                {
                    // 연출 코루틴은 현재 모델에만 적용
                    StopAllCoroutines();
                    StartCoroutine(ScaleUpEffect(model.transform));
                }
            }
        }


        IEnumerator ScaleUpEffect(Transform target)
        {
            float duration = 0.3f;
            float t = 0f;

            Vector3 start = Vector3.zero;
            Vector3 end = Vector3.one;

            target.localScale = start;

            while (t < duration)
            {
                t += Time.deltaTime;
                target.localScale = Vector3.Lerp(start, end, t / duration);
                yield return null;
            }
            target.localScale = end;
        }
        void ResetParticle()
        {
            if (fullGrownParticle == null) return;

            fullGrownParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fullGrownParticle.gameObject.SetActive(false);
        }
    /// <summary>
    /// 수확 함수
    /// </summary>
    public void Harvest()
    {
        // 수확 시 파티클 끄기
        ResetParticle();
        Debug.Log($"{data.cropName} 수확 완료!");

        PoolManager.Instance.Release(gameObject);
    }

    public void OnReturnToPool()
    {
        StopAllCoroutines();

        growthTimer = 0f;
        currentState = 0;
        hasPlayedFullGrownEffect = false;

        ResetParticle();

        // UI 정리
        if (cropUI != null)
        {
            cropUI.SetFillAmount(0f);
        }

        // 모델 비활성화
        for (int i = 0; i < instantiatedModels.Count; i++)
        {
            if (instantiatedModels[i] != null)
                instantiatedModels[i].SetActive(false);
        }

        data = null;
    }

    public void OnGetFromPool()
    {
        // 풀에서 꺼내질 때 호출됨
        // Initialize에서 대부분 초기화하지만, 안전하게 최소 리셋 처리

        hasPlayedFullGrownEffect = false;
        growthTimer = 0f;
        currentState = 0;

        ResetParticle();
    }

}
