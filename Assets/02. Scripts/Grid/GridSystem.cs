using UnityEngine;
using UnityEngine.EventSystems;

public class GridSystem : MonoBehaviour
{
    public static GridSystem Instance;

    [Header("풀링 키")]
    [SerializeField] private string mudPoolKey = "MUD";
    [SerializeField] private string cropPoolKey = "CROP";

    [Header("설정")]
    [SerializeField] private float cellSize = 10f;
    [SerializeField] private LayerMask groundLayer;

    [Header("프리뷰 프리팹")]
    [SerializeField] private GameObject mudPreviewPrefab;
    [SerializeField] private GameObject previewCropPrefab;

    [Header("가격")]
    [SerializeField] private int mudPrice = 50;

    [Header("배치 UI(V/X)")]
    [SerializeField] private GameObject placementUIPanel;

    [Header("연속 설치 방향")]
    [Tooltip("오른쪽으로 연속 설치: stepX=1, stepZ=0")]
    [SerializeField] private int stepX = 1;
    [SerializeField] private int stepZ = 0;

    [Header("모바일 조작")]
    [SerializeField] private float touchThreshold = 20f;
    private Vector2 touchStartPos;

    // ====== 상태 ======
    private enum PlaceState
    {
        None,            // 아무것도 선택 안 함(수확모드)
        Aiming,          // 아이템 선택됨, 프리뷰 따라다님 (첫 클릭으로 앵커 잡기 전)
        Anchored         // 앵커(기준 위치) 확정됨, V/X 활성화
    }

    private PlaceState state = PlaceState.None;

    // ====== 선택 정보 ======
    private bool isMudSelect = false;
    private CropData currentSelectedCrop = null;

    // ====== 참조 ======
    private GridManager gridManager;
    private GameObject previewInstance;

    // ====== pending(확정 대기) ======
    private Vector3 pendingPosition;
    private int pendingX, pendingZ;

    // ====== 드래그 최적화 ======
    private int lastX = -1;
    private int lastZ = -1;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (gridManager == null) gridManager = FindAnyObjectByType<GridManager>();
        if (placementUIPanel != null) placementUIPanel.SetActive(false);
    }

    private void Update()
    {
        // 앵커 확정 상태(Anchored)에서는 "땅 클릭으로 위치 변경"을 기본적으로 막음.
        // (원하면 Aiming으로 되돌리는 '위치 다시 선택' 버튼을 제공)
        if (Input.touchCount > 0) HandleMobileInput();
        else HandleEditorInput();
    }

    /// <summary>
    /// 에디터(PC) 마우스 입력 처리
    /// </summary>
    private void HandleEditorInput()
    {
        // 마우스 이동 시 프리뷰는 항상 갱신
        ProcessRaycast(Input.mousePosition, false);

        // 클릭은 Down 시점에서만 처리
        if (Input.GetMouseButtonDown(0))
        {
            // UI 위 클릭이면 월드 입력 차단
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
                return;

            ProcessRaycast(Input.mousePosition, true);
        }
    }

    /// <summary>
    /// 모바일 터치 입력 처리
    /// </summary>
    private void HandleMobileInput()
    {
        Touch touch = Input.GetTouch(0);

        // UI 터치 시 월드 입력 무시
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return;

        // 프리뷰 위치 갱신
        ProcessRaycast(touch.position, false);

        // 터치 종료 시 클릭으로 간주
        if (touch.phase == TouchPhase.Ended)
            ProcessRaycast(touch.position, true);
    }

    /// <summary>
    /// Raycast를 통해 그리드 좌표 계산 및 배치 로직 처리
    /// </summary>
    private void ProcessRaycast(Vector2 screenPos, bool isClick)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        // 바닥에 맞지 않으면 무시
        if (!Physics.Raycast(ray, out RaycastHit hit, 999f, groundLayer))
            return;

        // 월드 좌표를 그리드 좌표로 변환
        int x = Mathf.FloorToInt(hit.point.x / cellSize);
        int z = Mathf.FloorToInt(hit.point.z / cellSize);

        // 그리드 범위 밖이면 무시
        if (x < 0 || z < 0 || x >= gridManager.width || z >= gridManager.height)
            return;

        Vector3 snapPos = GridToWorld(x, z);

        // 프리뷰 표시 (조준 중 또는 기준 확정 상태)
        if (state == PlaceState.Aiming || state == PlaceState.Anchored)
        {
            float y = isMudSelect ? 0f : 0.16f;
            HandlePreview(snapPos, y);
        }

        // 클릭이 아니면 여기서 종료
        if (!isClick) return;

        // 수확 모드
        if (state == PlaceState.None)
        {
            TryHarvest(x, z);
            return;
        }

        // 첫 클릭 → 기준 위치 확정
        if (state == PlaceState.Aiming && CanPlace(x, z))
        {
            SetPending(x, z);
            ShowPlacementUI(pendingPosition);
            state = PlaceState.Anchored;
        }
    }

    /// <summary>
    /// 상점에서 아이템을 선택했을 때 호출되는 메서드
    /// </summary>
    public void SelectItemFromShop(CropData data, bool isMud)
    {
        // 선택 정보 저장
        isMudSelect = isMud;
        currentSelectedCrop = isMud ? null : data;

        // 프리뷰 교체
        UpdatePreview(isMud ? mudPreviewPrefab : previewCropPrefab);

        // 배치 조준 상태로 전환
        state = PlaceState.Aiming;

        // V/X 버튼은 아직 숨김
        placementUIPanel.SetActive(false);

        // 상점 닫기
        ShopManager.Instance?.CloseShop();
    }

    /// <summary>
    /// V 버튼 클릭 시 호출 (배치 확정)
    /// </summary>
    public void OnClickConfirmPlacement()
    {
        if (state != PlaceState.Anchored) return;

        // 현재 위치에 설치
        PlaceAt(pendingX, pendingZ);

        // 다음 칸으로 이동 가능하면 연속 배치
        if (TryMoveNextCell())
        {
            ShowPlacementUI(pendingPosition);
        }
        else
        {
            ExitPlacement();
        }
    }

    /// <summary>
    /// X 버튼 클릭 시 호출 (배치 취소)
    /// </summary>
    public void OnClickCancelPlacement()
    {
        ExitPlacement();
    }

    /// <summary>
    /// 기준 위치를 다시 선택하고 싶을 때 호출
    /// </summary>
    public void OnClickReselectAnchor()
    {
        state = PlaceState.Aiming;
        placementUIPanel.SetActive(false);
    }

    /// <summary>
    /// 그리드 좌표에 실제 오브젝트를 설치
    /// </summary>
    private void PlaceAt(int x, int z)
    {
        Vector3 pos = GridToWorld(x, z);

        if (isMudSelect)
        {
            if (!InventoryManager.Instance.TrySpendGold(mudPrice)) return;

            PoolManager.Instance.Get(mudPoolKey, pos, Quaternion.identity);
            gridManager.PlaceObject(x, z, TileType.Mud);
        }
        else
        {
            if (!InventoryManager.Instance.TrySpendGold(currentSelectedCrop.purchasePrice)) return;

            GameObject crop = PoolManager.Instance.Get(cropPoolKey, pos, Quaternion.identity);
            gridManager.PlaceObject(x, z, TileType.Crop);
            crop.GetComponent<Crop>().Initialize(currentSelectedCrop);
        }
    }

    /// <summary>
    /// 작물 수확 처리
    /// </summary>
    private void TryHarvest(int x, int z)
    {
        if (gridManager.GetTileType(x, z) != TileType.Crop) return;

        Vector3 center = GridToWorld(x, z);
        foreach (Collider col in Physics.OverlapSphere(center, cellSize * 0.4f))
        {
            if (!col.TryGetComponent(out Crop crop)) continue;
            if (!crop.IsFullGrown) return;

            crop.Harvest();
            gridManager.PlaceObject(x, z, TileType.Mud);
            return;
        }
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
    /// 기준 위치(pending) 설정
    /// </summary>
    private void SetPending(int x, int z)
    {
        pendingX = x;
        pendingZ = z;
        pendingPosition = GridToWorld(x, z);
    }

    /// <summary>
    /// 다음 칸으로 이동 (연속 배치용)
    /// </summary>
    private bool TryMoveNextCell()
    {
        int nx = pendingX + stepX;
        int nz = pendingZ + stepZ;

        if (nx < 0 || nz < 0 ||
            nx >= gridManager.width || nz >= gridManager.height)
            return false;

        if (!CanPlace(nx, nz)) return false;

        SetPending(nx, nz);
        return true;
    }

    /// <summary>
    /// 배치 모드 종료 및 상태 초기화
    /// </summary>
    private void ExitPlacement()
    {
        state = PlaceState.None;
        placementUIPanel.SetActive(false);
        previewInstance.SetActive(false);
    }

    /// <summary>
    /// 배치 UI(V/X)를 월드 좌표 기준으로 표시
    /// </summary>
    private void ShowPlacementUI(Vector3 pos)
    {
        placementUIPanel.SetActive(true);
        placementUIPanel.transform.position = pos + Vector3.up * 1.5f;
        placementUIPanel.transform.rotation = Camera.main.transform.rotation;
    }

    /// <summary>
    /// 프리뷰 프리팹 교체
    /// </summary>
    private void UpdatePreview(GameObject prefab)
    {
        if (previewInstance != null)
            Destroy(previewInstance);

        previewInstance = Instantiate(prefab);
        previewInstance.GetComponent<Collider>().enabled = false;
    }

    /// <summary>
    /// 프리뷰 위치 및 표시 처리
    /// </summary>
    private void HandlePreview(Vector3 pos, float y)
    {
        pos.y = y;
        previewInstance.SetActive(true);
        previewInstance.transform.position = pos;
    }
}