using System;
using UnityEditor.ShaderGraph;
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

    private GridManager gridManager;   // 설치 데이터를 관리하는 GridManager 참조
    private GameObject previewInstance; // 씬에 생성되어 따라다닐 미리보기 인스턴스

    void Start()
    {
       /* // 미리보기 인스턴스를 생성하고 초기 설정 진행
        if (mudPreviewPrefab != null)
        {
            previewInstance = Instantiate(mudPreviewPrefab);
            mudPreviewPrefab.SetActive(false);

            // TryGetComponent: GameObject에 존재하는 경우 지정된 유형의 컴포넌트를 검색하려고 시도하고,
            // 발견되면 true, 발견되지 않으면 false를 반환한다.
            //public bool TryGetComponent<T>(out T component) where T : Component;
            *//* T: 가져오려는 컴포넌트의 타입
            component: 컴포넌트를 가져올 때 사용되는 out 매개변수 *//*

            // 미리보기 오브젝트가 마우스 레이캐스트를 방해하지 않도록 콜라이더 비활성화
            if (previewInstance.TryGetComponent<Collider>(out Collider col)) col.enabled = false;
        }*/
        // 씬에 존재하는 GridManager를 찾아 참조 연결
        if (gridManager == null) gridManager = FindAnyObjectByType<GridManager>();
    }
    void Update()
    {
        // 마우스 위치로부터 화면 안쪽으로 레이(광선)를 쏜다.
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        //Ray가 어떤 Collider에 맞으면 true를 반환하고, 그 충돌 정보는 hit에 담긴다.

        // 지정된 groundLayer(바닥)에 레이가 맞았을 때만 로직 실행
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            // [좌표 계산] 맞은 지점의 월드 좌표를 그리드 인덱스(0, 1, 2...)로 변환
            int xIdx = Mathf.FloorToInt(hit.point.x / cellSize);
            int zIdx = Mathf.FloorToInt(hit.point.z / cellSize);

            // [스냅 좌표] 바닥이 그리드 정중앙에 오도록 좌표 보정 (+cellSize/2)
            Vector3 snapPos = new Vector3(xIdx * cellSize + (cellSize / 2), 0, zIdx * cellSize + (cellSize / 2));
            
            // 모드에 따른 높이 결정
            // 밭 설치 모드면 0, 작물 설치 모드면 0.16f
            float targetY = isMudSelect ? 0f : 0.16f;

            // 프리뷰 처리 (targetY 반영)
            HandlePreview(snapPos, targetY, xIdx, zIdx);

            // 마우스 왼쪽 클릭 시 해당 위치에 실제 설치 시도
            if (Input.GetMouseButtonDown(0))
            {
                PlaceAt(snapPos, xIdx, targetY, zIdx);
            }
        }
        else
        {
            // 바닥을 벗어나면 미리보기를 숨김
            if (previewInstance != null)
            {
                previewInstance.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 선택된 모드에 맞춰 미리보기(Preview) 모델을 교체합니다.
    /// </summary>
    void UpdatePreviewInstance(GameObject newPrefab)
    {
        // 기존 미리보기가 있다면 삭제
        if(previewInstance != null)
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

