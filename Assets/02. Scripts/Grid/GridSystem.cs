using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 마우스 입력을 받아 그리드 상에 오브젝트를 배치하고 
/// 배치 가능 여부를 시각적으로 보여주는 클래스
/// </summary>
public class GridSystem : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private float cellSize = 10f;       // 그리드 한 칸의 크기 (기본 Plane 10x10에 맞춤)
    [SerializeField] private LayerMask groundLayer;      // 레이캐스트가 감지할 바닥 레이어

    [Header("밭 설정")]
    [SerializeField] private GameObject mudPreviewPrefab;   // 설치 전 보여줄 투명한 미리보기용 프리팹
    [SerializeField] private GameObject mudPrefab;      // 실제로 설치될 완성된 오브젝트 프리팹

    [Header("작물 설정")]
    [SerializeField] private GameObject previewCropPrefab;
    [SerializeField] private GameObject cropPrefab;    // Crop 스크립트가 붙어있는 빈 오브젝트 프리팹

    [Header("선택된 상태")]
    [SerializeField] private bool isMudSelect = false; // 기본적으로 아무것도 선택X
    [SerializeField] private CropData currentSelectedCrop; // 선택된 씨앗 데이터

    [Header("수확 이펙트 설정")]
    [SerializeField] private GameObject floatingTextPrefab; // 위에서 만든 프리팹 연결
    [SerializeField] private Canvas worldCanvas;

    private GridManager gridManager;   // 설치 데이터를 관리하는 GridManager 참조
    private GameObject previewInstance; // 씬에 생성되어 따라다닐 미리보기 인스턴스
    [Header("모바일 조작 설정")]
    [SerializeField] private float touchThreshold = 20f; // 드래그와 클릭을 구분하는 임계값
    private Vector2 touchStartPos;

    void Start()
    {
        // 씬에 존재하는 GridManager를 찾아 참조 연결
        if (gridManager == null) gridManager = FindAnyObjectByType<GridManager>();
    }


    void Update()
    {
        // 1. 입력 처리 분기 (모바일 터치 vs 에디터 마우스)
        if (Input.touchCount > 0)
        {
            HandleMobileInput();
        }
        else if (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))
        {
            HandleEditorInput();
        }
        else
        {
            // 입력이 전혀 없으면 프리뷰 끄기
            if (previewInstance != null) previewInstance.SetActive(false);
        }
    }

    // --- 모바일 터치 처리 ---
    void HandleMobileInput()
    {
        Touch touch = Input.GetTouch(0);
        Vector2 touchPos = touch.position;

        if (touch.phase == TouchPhase.Began)
        {
            touchStartPos = touchPos;
        }

        // 손가락이 닿아 있는 동안은 프리뷰만 계속 보여줌 (설치는 안함)
        ProcessRaycast(touchPos, false);

        // 손가락을 뗐을 때, 움직인 거리가 짧다면 '클릭'으로 간주하여 실행
        if (touch.phase == TouchPhase.Ended)
        {
            float distance = Vector2.Distance(touchStartPos, touchPos);
            if (distance < touchThreshold)
            {
                ProcessRaycast(touchPos, true); // 실제 설치/수확 진행
            }

            if (previewInstance != null) previewInstance.SetActive(false);
        }
    }

    // --- 에디터 마우스 처리 ---
    void HandleEditorInput()
    {
        Vector2 mousePos = Input.mousePosition;

        // 마우스를 누르고 있는 동안은 프리뷰만 업데이트
        ProcessRaycast(mousePos, false);

        // 마우스를 뗄 때만 설치/수확 실행
        if (Input.GetMouseButtonUp(0))
        {
            ProcessRaycast(mousePos, true);
            if (previewInstance != null) previewInstance.SetActive(false);
        }
    }


    // 핵심 레이캐스트 로직 (통합) 
    void ProcessRaycast(Vector2 screenPos, bool isFinalAction)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            int xIdx = Mathf.FloorToInt(hit.point.x / cellSize);
            int zIdx = Mathf.FloorToInt(hit.point.z / cellSize);

            Vector3 snapPos = new Vector3(xIdx * cellSize + (cellSize / 2), 0, zIdx * cellSize + (cellSize / 2));
            float targetY = isMudSelect ? 0f : 0.16f;

            // 시각적 미리보기 업데이트
            HandlePreview(snapPos, targetY, xIdx, zIdx);

            // 실제 동작(설치/수확) 수행
            if (isFinalAction)
            {
                if (isMudSelect || currentSelectedCrop != null)
                {
                    PlaceAt(snapPos, xIdx, targetY, zIdx);
                }
                else
                {
                    TryHarvest(hit.point, xIdx, zIdx);
                }
            }
        }
        else
        {
            if (previewInstance != null) previewInstance.SetActive(false);
        }
    }

    void TryHarvest(Vector3 hitPoint, int x, int z)
    {
        // 현재 칸이 작물 상태인지 확인
        if (gridManager.GetTileType(x, z) == TileType.Crop)
        {
            // 해당 그리드 칸의 중심 좌표 계산
            Vector3 targetPos = new Vector3(x * cellSize + (cellSize / 2), 0, z * cellSize + (cellSize / 2));

            // 주변의 콜라이더를 체크하여 Crop 오브젝트 탐색
            Collider[] colliders = Physics.OverlapSphere(targetPos, cellSize * 0.4f);

            foreach (var col in colliders)
            {
                if (col.TryGetComponent<Crop>(out Crop crop))
                {
                    // 다 자랐는지 확인
                    if (crop.IsFullGrown)
                    {
                        // 판매 금액 결정
                        int reward = crop.data.sellingPrice;

                        // 골드 지급
                        InventoryManager.Instance.AddGoldGFromHarvest(reward);

                        //텍스트 띄우기
                        ShowFloatingText(targetPos, reward);

                        crop.Harvest();
                        gridManager.PlaceObject(x, z, TileType.Mud);

                        if (UIManager.Instance != null)
                        {
                            UIManager.Instance.AddHarvestCount();
                        }

                        Debug.Log($"[{x}, {z}] 수확 성공! 다시 심을 수 있습니다.");
                    }
                    else
                    {
                        Debug.Log("아직 다 자라지 않았습니다.");
                    }
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 모든 선택을 해제하고 수확 모드로 전환하는 버튼용 함수(아무것도 안 들고 있음)
    /// </summary>
    public void OnClickClearSelection()
    {
        isMudSelect = false;
        currentSelectedCrop = null;

        if (previewInstance != null)
        {
            previewInstance.SetActive(false);
        }
        Debug.Log("모든 선택 해제 - 수확 모드 활성화");

    }

    /// <summary>
    /// 선택된 모드에 맞춰 미리보기(Preview) 모델을 교체합니다.
    /// </summary>
    void UpdatePreviewInstance(GameObject newPrefab)
    {
        // 기존 미리보기가 있다면 삭제
        if (previewInstance != null)
        {
            Destroy(previewInstance);
        }
        //OnClickSelectCrop 새로운 프리팹으로 생성
        if (newPrefab != null)
        {
            // 원본 프리팹을 복제하여 previewInstance 변수에 할당
            previewInstance = Instantiate(newPrefab);
            previewInstance.SetActive(false);

            // 레이캐스트 방해 방지 (콜라이더 끄기)
            if (previewInstance.TryGetComponent<Collider>(out Collider col))
                col.enabled = false;
        }
    }

    /// <summary>
    /// 밭 선택 버튼
    /// </summary>
    public void OnClickSelectMud()
    {
        isMudSelect = true;
        currentSelectedCrop = null;
        // 미리보기를 밭용으로 교체
        UpdatePreviewInstance(mudPreviewPrefab);
        Debug.Log("밭 선택 모드");
    }

    public void OnClickSelectCrop(CropData cropdata)
    {
        isMudSelect = false;
        currentSelectedCrop = cropdata;
        // 미리보기를 작물용으로 교체
        UpdatePreviewInstance(previewCropPrefab);
        Debug.Log($"{cropdata.cropName} 씨앗 선택");
    }

    /// <summary>
    /// 미리보기 오브젝트를 마우스 위치로 이동시키고, 설치 가능 여부에 따라 색상을 변경하는 메서드
    /// </summary>
    void HandlePreview(Vector3 pos, float y, int x, int z)
    {
        if (previewInstance == null)
        {
            return;
        }

        // 아무 모드도 선택되지 않았다면 프리뷰를 끄고 리턴
        if (!isMudSelect && currentSelectedCrop == null)
        {
            previewInstance.SetActive(false);
            return;
        }
        pos.y = y;
        previewInstance.SetActive(true);
        previewInstance.transform.position = pos;

        if (gridManager == null) return;

        bool canPlaceVisually = false;
        if (isMudSelect)
        {
            //밭 설치 모드 : 해당 칸이 Empty여야 초록색
            canPlaceVisually = gridManager.CanPlace(x, z);
        }
        else if (currentSelectedCrop != null)
        {
            //씨앗 심기 모드 : 해당 칸이 Mud 여야 초록색(이미 Crop인 곳은 false가 됨)
            canPlaceVisually = (gridManager.GetTileType(x, z) == TileType.Mud);
        }

        // 시각적 피드백: 가능하면 초록색(Green), 불가능하면 빨간색(Red) 반투명 처리
        MeshRenderer mr = previewInstance.GetComponentInChildren<MeshRenderer>();
        if (mr != null)
        {
            mr.material.color = canPlaceVisually ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
        }
    }

    /// <summary>
    /// 최종적으로 GridManager의 승인을 받아 실제 오브젝트를 생성하고 점유 데이터를 기록하는 메서드
    /// </summary>
    void PlaceAt(Vector3 pos, int x, float y, int z)
    {
        // 밭 설치 모드일 때
        if (isMudSelect && gridManager.CanPlace(x, z))
        {
            pos.y = 0f; // 밭은 무조건 바닥에 설치
            Instantiate(mudPrefab, pos, Quaternion.identity);
            gridManager.PlaceObject(x, z, TileType.Mud);
            Debug.Log($"[{x}, {z}] 위치에 밭 설치 성공");
        }
        // 작물 설치 모드 (현재 칸이 정확히 Mud 상태여야만 함)
        // 만약 이미 작물을 심어서 TileType.Crop으로 변했다면, GetTileType은 Mud가 아니게 됨
        else if (!isMudSelect && currentSelectedCrop != null)
        {
            TileType currentTile = gridManager.GetTileType(x, z);
            if (gridManager.GetTileType(x, z) == TileType.Mud)
            {
                pos.y = 0.16f;
                PlantCrop(pos, x, z);
            }
            else if (currentTile == TileType.Crop)
            {
                Debug.Log("여기는 이미 작물이 자라고 있습니다!"); // 이 로그가 찍히는지 확인하세요.
            }
            else
            {
                Debug.Log("밭이 아닌 곳에는 심을 수 없습니다.");
            }
        }
    }
    /// <summary>
    /// 플로팅 텍스트(골드 획득 텍스트) 띄우기
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="amount"></param>
    void ShowFloatingText(Vector3 pos, int amount)
    {
        // 텍스트가 작물보다 약간 위에서 생성되도록 y값 조절
        pos.y += 1f;

        GameObject textObj = Instantiate(floatingTextPrefab, pos, Quaternion.identity, worldCanvas.transform);

        //텍스트 내용 변경
        if (textObj.TryGetComponent<TextMeshProUGUI>(out var tmp))
        {
            tmp.text = $"{amount}골드";
        }
    }

    /// <summary>
    /// 식물 심기
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="x"></param>
    /// <param name="z"></param>

    void PlantCrop(Vector3 pos, int x, int z)
    {
        // 작물 프리팹 생성
        GameObject newCrop = Instantiate(cropPrefab, pos, Quaternion.identity);

        gridManager.PlaceObject(x, z, TileType.Crop);

        // 생성된 작물에 Crop 스크립트를 가져와 데이터 전달
        if (newCrop.TryGetComponent<Crop>(out Crop crop))
        {
            crop.Initialize(currentSelectedCrop);
            Debug.Log($"{currentSelectedCrop.cropName}을 심었습니다/");
        }

        else
        {
            Debug.LogError($"cropPrefab 루트에 Crop 컴포넌트가 없습니다! 프리팹을 확인하세요: {newCrop.name}");
            gridManager.PlaceObject(x, z, TileType.Mud);
            Destroy(newCrop);
        }
    }
}

