using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 마우스 입력을 받아 그리드 상에 오브젝트를 배치하고 
/// 배치 가능 여부를 시각적으로 보여주는 클래스
/// </summary>
public class GridSystem : MonoBehaviour
{
    public static GridSystem Instance;

    [Header("풀링 키")]
    [SerializeField] private string mudPoolKey = "MUD";
    [SerializeField] private string cropPoolKey = "CROP";
    [SerializeField] private string buildingPoolKey = "BUILDING";

    [Header("설정")]
    [SerializeField] private float cellSize = 10f;
    [SerializeField] private LayerMask groundLayer;

    [Header("프리뷰 프리팹")]
    [SerializeField] private GameObject mudPreviewPrefab;
    [SerializeField] private GameObject buildingPreviewPrefab;
    [SerializeField] private Material previewMaterial;


    [Header("가격")]
    [SerializeField] private int mudPrice = 50;

    [Header("배치 UI(V/X)")]
    [SerializeField] private GameObject placementUIPanel;
    // (x,z) 칸에 배치된 실제 오브젝트 저장
    private Dictionary<Vector2Int, GameObject> placedObjects = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int Key(int x, int z) => new Vector2Int(x, z);

    // ====== 상태 ======
    private enum PlaceState
    {
        None,            // 아무것도 선택 안 함(수확모드)
        Placing          // 배치 모드 활성화 상태
    }

    private PlaceState state = PlaceState.None;

    // ====== 선택 정보 ======
    private bool isMudSelect = false;
    private bool isBuildingSelect = false;
    private CropData currentSelectedCrop = null;

    // ====== 참조 ======
    private GridManager gridManager;
    private GameObject previewInstance;

    // ====== pending(확정 대기 - 건물용) ======
    private Vector3 pendingPosition;
    private int pendingX, pendingZ;

    // ====== 드래그 최적화 (밭/작물용) ======
    private int lastX = -1;
    private int lastZ = -1;

    private void Awake() => Instance = this;

    private void Start()
    {
        // 씬에 존재하는 GridManager를 찾아 참조 연결
        if (gridManager == null) gridManager = FindAnyObjectByType<GridManager>();
        // 시작 시 배치 UI는 꺼둠
        if (placementUIPanel != null) placementUIPanel.SetActive(false);
    }

    private void Update()
    {
        // 아무것도 선택되지 않은 상태(수확 모드 등)에서는 클릭 시 수확만 처리
        if (state == PlaceState.None)
        {
            HandleHarvestInput();
            return;
        }

        // 앵커 확정 상태(V/X 버튼 활성)에서는 드래그 입력을 잠시 멈춤
        if (placementUIPanel.activeSelf) return;

        if (Input.touchCount > 0) HandleMobilePlacement();
        else HandleEditorPlacement();
    }

    /// <summary>
    /// 수확 모드 입력 처리 (드래그 수확 지원)
    /// </summary>
    private void HandleHarvestInput()
    {
        // PC
        if (Input.touchCount == 0)
        {
            // 프레스 중이면 계속 수확 시도
            if (Input.GetMouseButton(0))
            {
                if (IsPointerOverUI()) return;
                ProcessHarvest(Input.mousePosition);
            }

            // 손 뗐으면 기록 초기화
            if (Input.GetMouseButtonUp(0))
            {
                lastX = -1;
                lastZ = -1;
            }
        }
        // Mobile
        else
        {
            Touch touch = Input.GetTouch(0);
            if (IsPointerOverUI(touch.fingerId)) return;

            if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                ProcessHarvest(touch.position);
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                lastX = -1;
                lastZ = -1;
            }
        }
    }
    /// <summary>
    /// 드래그 중 해당 칸을 수확 시도 (중복 방지)
    /// </summary>
    private void ProcessHarvest(Vector2 screenPos)
    {
        RaycastToGrid(screenPos, (x, z, hitPos) =>
        {
            // 같은 칸이면 스킵 (드래그 중 중복 수확 방지)
            if (x == lastX && z == lastZ) return;

            // 수확 시도
            TryHarvest(x, z);

            lastX = x;
            lastZ = z;
        });
    }


    /// <summary>
    /// 에디터(PC) 마우스 배치 입력 처리
    /// </summary>
    private void HandleEditorPlacement()
    {
        // 마우스 이동 시 프리뷰 갱신
        ProcessPlacement(Input.mousePosition);

        // 마우스를 누르고 있는 동안(드래그 포함) 실시간으로 설치를 시도
        if (Input.GetMouseButton(0))
        {
            if (IsPointerOverUI()) return;
            ProcessPlacement(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            // 마우스를 떼면 마지막 기록 초기화 (다시 클릭했을 때 바로 심기게)
            lastX = -1;
            lastZ = -1;
        }
    }

    /// <summary>
    /// 모바일 터치 배치 입력 처리
    /// </summary>
    private void HandleMobilePlacement()
    {
        if (Input.touchCount == 0) return;
        Touch touch = Input.GetTouch(0);
        if (IsPointerOverUI(touch.fingerId)) return;

        // 프리뷰 위치 및 설치 처리
        ProcessPlacement(touch.position);

        if (touch.phase == TouchPhase.Ended)
        {
            lastX = -1;
            lastZ = -1;
        }
    }

    /// <summary>
    /// 레이캐스트를 통해 그리드 좌표 계산 및 배치 로직 처리
    /// </summary>
    private void ProcessPlacement(Vector2 screenPos)
    {
        RaycastToGrid(screenPos, (x, z, hitPos) =>
        {
            Vector3 snapPos = GridToWorld(x, z);
            HandlePreview(snapPos, (isMudSelect || isBuildingSelect) ? 0f : 0.16f);

            // [건물 설치 로직: V/X 방식]
            if (isBuildingSelect)
            {
                // 마우스를 뗐을 때 해당 자리에 V/X UI를 띄움
                if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
                {
                    pendingPosition = snapPos;
                    pendingX = x;
                    pendingZ = z;

                    if (previewInstance != null) previewInstance.transform.position = snapPos;
                    ShowPlacementUI(snapPos);
                }
            }
            // [밭/작물 설치 로직: 드래그 즉시 설치 방식]
            else
            {
                // 마우스 버튼을 누르고 있는 동안에만 설치 진행
                if (!Input.GetMouseButton(0) && Input.touchCount == 0) return;

                // 드래그 중 이전 칸과 동일하면 건너뜀
                if (x == lastX && z == lastZ) return;

                // 이미 배치된 곳이면 취소(삭제)가 우선
                if (TryCancelPlacedAt(x, z))
                {
                    lastX = x;
                    lastZ = z;
                    return;
                }

                // 비어있거나 심을 수 있으면 설치
                if (CanPlace(x, z))
                {
                    PlaceAt(x, z);
                    lastX = x;
                    lastZ = z;
                }
            }
        });
    }

    private bool TryCancelPlacedAt(int x, int z)
    {
        // 밭 배치 모드일 때: 이미 Mud면 다시 누르면 Empty로
        if (isMudSelect && gridManager.GetTileType(x, z) == TileType.Mud)
        {
            RemovePlacedObject(x, z);
            gridManager.PlaceObject(x, z, TileType.Empty);

            // 환불하고 싶으면 여기서 AddGold(mudPrice)
            InventoryManager.Instance.AddGoldGFromHarvest(mudPrice);

            return true;
        }

        // 작물 배치 모드일 때: 이미 Crop이면 다시 누르면 Mud로
        if (!isMudSelect && !isBuildingSelect && gridManager.GetTileType(x, z) == TileType.Crop)
        {
            RemovePlacedObject(x, z);
            gridManager.PlaceObject(x, z, TileType.Mud);

            InventoryManager.Instance.AddGoldGFromHarvest(currentSelectedCrop.purchasePrice);

            return true;
        }

        return false;
    }

    /// <summary>
    /// 풀 반납
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    private void RemovePlacedObject(int x, int z)
    {
        var k = Key(x, z);
        if (!placedObjects.TryGetValue(k, out var obj) || obj == null)
            return;

        placedObjects.Remove(k);

        PoolManager.Instance.Release(obj);
    }

    /// <summary>
    /// 상점에서 아이템(밭, 작물, 건물)을 선택했을 때 호출되는 메서드
    /// </summary>
    public void SelectItemFromShop(CropData data, bool isMud, bool isBuilding = false)
    {
        isMudSelect = isMud;
        isBuildingSelect = isBuilding;
        currentSelectedCrop = (isMud || isBuilding) ? null : data;

        if (isMudSelect)
        {
            UpdatePreview(mudPreviewPrefab);
        }
        else if (isBuildingSelect)
        {
            UpdatePreview(buildingPreviewPrefab);
        }
        else
        {
            UpdateCropPreview(currentSelectedCrop);
        }

        state = PlaceState.Placing;
        if (placementUIPanel != null) placementUIPanel.SetActive(false);

        ShopManager.Instance?.CloseShop();
        UIManager.Instance.OnPlaceUI(true, data, isMud, isBuilding);
        Debug.Log($"[SelectItemFromShop] data={(data != null ? data.cropName : "NULL")} isMud={isMud} isBuilding={isBuilding}");

    }

    /// <summary>
    /// 작물 전용, 데이터 기반 프리뷰 생성
    /// </summary>
    /// <param name="data"></param>
    private void UpdateCropPreview(CropData data)
    {
        if (previewInstance != null)
            Destroy(previewInstance);

        if (data.growthStagePrefabs == null || data.growthStagePrefabs.Length == 0)
            return;

        GameObject seedPrefab = data.growthStagePrefabs[0];
        if (seedPrefab == null) return;

        previewInstance = Instantiate(seedPrefab);

        // 프리뷰니까 충돌 제거
        foreach (var c in previewInstance.GetComponentsInChildren<Collider>(true))
            c.enabled = false;
        
        // 프리뷰 머티리얼 적용
        ApplyPreviewMaterial(previewInstance);
    }
private void ApplyPreviewMaterial(GameObject previewObj)
{
    if (previewMaterial == null) return;

    var renderers = previewObj.GetComponentsInChildren<Renderer>(true);

    foreach (var r in renderers)
    {
        // 머티리얼 인스턴스 생성 (원본 보호)
        Material[] mats = new Material[r.sharedMaterials.Length];

        for (int i = 0; i < mats.Length; i++)
        {
            mats[i] = new Material(previewMaterial);
        }

        r.materials = mats;
    }
}


    /// <summary>
    /// 작물 수확 처리
    /// </summary>
    private void TryHarvest(int x, int z)
    {
        // 현재 칸이 작물 상태인지 확인
        if (gridManager.GetTileType(x, z) != TileType.Crop) return;

        Vector3 center = GridToWorld(x, z);
        // 주변의 콜라이더를 체크하여 Crop 오브젝트 탐색
        foreach (Collider col in Physics.OverlapSphere(center, cellSize * 0.4f))
        {
                if (col.TryGetComponent(out Crop crop) && crop.IsFullGrown)
            {
                int amountGold = crop.data.sellingPrice;
                // 골드 지급 및 경험치 지급
                InventoryManager.Instance.AddGoldGFromHarvest(amountGold);
                crop.cropUI.ShowGold(amountGold);
                if (ExpManager.Instance != null) ExpManager.Instance.AddExp(crop.data.expReward);

                // 수확 연출 및 데이터 갱신
                crop.Harvest();
                gridManager.PlaceObject(x, z, TileType.Mud);
                return;
            }
        }
    }

    /// <summary>
    /// V 버튼 클릭 시 호출 (배치 확정)
    /// </summary>
    public void OnClickConfirmPlacement()
    {
        // 현재 위치에 설치
        PlaceAt(pendingX, pendingZ);

        // 설치 후 계속 심을 수 있게 상태 유지 (에브리타운 방식)
        // 건물의 경우에도 프리뷰를 유지하여 다음 위치를 잡을 수 있게 함
        placementUIPanel.SetActive(false);
    }

    /// <summary>
    /// X 버튼 클릭 시 호출 (배치 취소)
    /// </summary>
    public void OnClickCancelPlacement() => ExitPlacement();

    /// <summary>
    /// 배치 완료 버튼 클릭 시 호출 (배치 모드 종료)
    /// </summary>
    public void OnClickExitPlacement() => ExitPlacement();

    /// <summary>
    /// 그리드 좌표에 실제 오브젝트를 설치
    /// </summary>
    private void PlaceAt(int x, int z)
    {
        Vector3 pos = GridToWorld(x, z);

        var k = Key(x, z);
        if (isMudSelect)
        {
            if (!InventoryManager.Instance.TrySpendGold(mudPrice)) return;

            GameObject mudObj = PoolManager.Instance.Get(mudPoolKey, pos, Quaternion.identity);
            placedObjects[k] = mudObj;

            gridManager.PlaceObject(x, z, TileType.Mud);
        }
        else if (isBuildingSelect)
        {
            GameObject bObj = PoolManager.Instance.Get(buildingPoolKey, pos, Quaternion.identity);
            placedObjects[k] = bObj;

            gridManager.PlaceObject(x, z, TileType.Crop);
        }
        else
        {
            if (!InventoryManager.Instance.TrySpendGold(currentSelectedCrop.purchasePrice)) return;

            GameObject cropObj = PoolManager.Instance.Get(cropPoolKey, pos, Quaternion.identity);
            placedObjects[k] = cropObj;

            gridManager.PlaceObject(x, z, TileType.Crop);
            cropObj.GetComponent<Crop>().Initialize(currentSelectedCrop);
        }
    }

    /// <summary>
    /// 배치 모드 종료 및 상태 초기화
    /// </summary>
    private void ExitPlacement()
    {
        state = PlaceState.None;
        isMudSelect = false;
        isBuildingSelect = false;
        currentSelectedCrop = null;
        if (previewInstance != null) previewInstance.SetActive(false);
        if (placementUIPanel != null) placementUIPanel.SetActive(false);

        UIManager.Instance.OnPlaceUI(false, null, false, false);
    }

    /// <summary>
    /// 배치 UI(V/X)를 월드 좌표 기준으로 표시
    /// </summary>
    private void ShowPlacementUI(Vector3 pos)
    {
        if (placementUIPanel == null) return;
        placementUIPanel.SetActive(true);
        // World Canvas 좌표 대입 및 카메라 방향 고정
        placementUIPanel.transform.position = pos + Vector3.up * 1.5f;
        placementUIPanel.transform.rotation = Camera.main.transform.rotation;
    }

    /// <summary>
    /// 프리뷰 프리팹 교체
    /// </summary>
    private void UpdatePreview(GameObject prefab)
    {
        if (previewInstance != null) Destroy(previewInstance);
        if (prefab == null) return;
        previewInstance = Instantiate(prefab);
        // 레이캐스트 방해 방지 (콜라이더 끄기)
        if (previewInstance.TryGetComponent<Collider>(out var col)) col.enabled = false;

        // 작물 프리뷰를 0단계로 적용
        if (!isMudSelect && !isBuildingSelect && currentSelectedCrop != null)
        {
            ApplySeedPreview(currentSelectedCrop);
        }
    }

    private void ApplySeedPreview(CropData data)
    {
        if (data.growthStagePrefabs == null || data.growthStagePrefabs.Length == 0) return;

        GameObject seedPrefab = data.growthStagePrefabs[0];
        if (seedPrefab == null) return;

        Transform root = previewInstance.transform.Find("Seed");
        if (root == null) return;

        //기존 프리뷰 모델 제거
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }
        // 씨앗 단계 프리뷰 생성
        GameObject previewModel = Instantiate(seedPrefab, root);

        if (previewModel.TryGetComponent<Collider>(out var c))
        {
            c.enabled = false;
        }
    }


    /// <summary>
    /// 프리뷰 위치 및 표시 처리
    /// </summary>
    private void HandlePreview(Vector3 pos, float y)
    {
        if (previewInstance == null) return;
        pos.y = y;
        previewInstance.SetActive(true);
        previewInstance.transform.position = pos;
    }

    /// <summary>
    /// 그리드 좌표를 월드 좌표(칸 중앙)로 변환
    /// </summary>
    private Vector3 GridToWorld(int x, int z)
    {
        return new Vector3(
            x * cellSize + cellSize / 2f,
            0f,
            z * cellSize + cellSize / 2f
        );
    }

    /// <summary>
    /// 해당 그리드 칸에 설치 가능한지 여부
    /// </summary>
    private bool CanPlace(int x, int z)
    {
        return isMudSelect
            ? gridManager.CanPlace(x, z)
            : gridManager.GetTileType(x, z) == TileType.Mud;
    }

    /// <summary>
    /// 마우스/터치가 UI 위에 있는지 확인
    /// </summary>
    private bool IsPointerOverUI(int fingerId = -1) => EventSystem.current != null && (fingerId == -1 ? EventSystem.current.IsPointerOverGameObject() : EventSystem.current.IsPointerOverGameObject(fingerId));

    /// <summary>
    /// 레이캐스트 공통 처리 유틸리티
    /// </summary>
    private void RaycastToGrid(Vector2 screenPos, System.Action<int, int, Vector3> onSuccess)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            int x = Mathf.FloorToInt(hit.point.x / cellSize);
            int z = Mathf.FloorToInt(hit.point.z / cellSize);
            onSuccess?.Invoke(x, z, hit.point);
        }
    }
}