using System;
using UnityEngine;

/// <summary>
/// 마우스 입력을 받아 그리드 상에 오브젝트를 배치하고 
/// 배치 가능 여부를 시각적으로 보여주는 클래스
/// </summary>
public class GridSystem : MonoBehaviour
{
    [Header("설정")]
    public float cellSize = 10f;       // 그리드 한 칸의 크기 (기본 Plane 10x10에 맞춤)
    public LayerMask groundLayer;      // 레이캐스트가 감지할 바닥 레이어

    [Header("프리팹")]
    public GameObject previewPrefab;   // 설치 전 보여줄 투명한 미리보기용 프리팹
    public GameObject realPrefab;      // 실제로 설치될 완성된 오브젝트 프리팹

    private GridManager gridManager;   // 설치 데이터를 관리하는 GridManager 참조
    private GameObject previewInstance; // 씬에 생성되어 따라다닐 미리보기 인스턴스

    void Start()
    {
        // 미리보기 인스턴스를 생성하고 초기 설정 진행
        if (previewPrefab != null)
        {
            previewInstance = Instantiate(previewPrefab);
            previewPrefab.SetActive(false);

            // TryGetComponent: GameObject에 존재하는 경우 지정된 유형의 컴포넌트를 검색하려고 시도하고,
            // 발견되면 true, 발견되지 않으면 false를 반환한다.
            //public bool TryGetComponent<T>(out T component) where T : Component;
            /* T: 가져오려는 컴포넌트의 타입
            component: 컴포넌트를 가져올 때 사용되는 out 매개변수 */

            // 미리보기 오브젝트가 마우스 레이캐스트를 방해하지 않도록 콜라이더 비활성화
            if (previewInstance.TryGetComponent<Collider>(out Collider col)) col.enabled = false;
        }
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

            // [스냅 좌표] 오브젝트가 그리드 정중앙에 오도록 좌표 보정 (+cellSize/2)
            Vector3 snapPos = new Vector3(xIdx * cellSize + (cellSize / 2), 0, zIdx * cellSize + (cellSize / 2));

            // 미리보기 위치 업데이트 및 색상 변경 함수 호출
            HandlePreview(snapPos, xIdx, zIdx);

            // 마우스 왼쪽 클릭 시 해당 위치에 실제 설치 시도
            if (Input.GetMouseButtonDown(0))
            {
                PlaceAt(snapPos, xIdx, zIdx);
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

        /// <summary>
        /// 미리보기 오브젝트를 마우스 위치로 이동시키고, 설치 가능 여부에 따라 색상을 변경하는 메서드
        /// </summary>
        void HandlePreview(Vector3 pos, int x, int z)
        {
            if (previewInstance == null)
            {
                return;
            }
            previewInstance.SetActive(true);
            previewInstance.transform.position = pos;

            // GridManager에게 현재 칸이 비어있는지 확인 요청
            bool canPlace = gridManager.CanPlace(x, z);

            // 시각적 피드백: 가능하면 초록색(Green), 불가능하면 빨간색(Red) 반투명 처리
            MeshRenderer mr = previewInstance.GetComponentInChildren<MeshRenderer>();
            if(mr != null)
            {
                mr.material.color = canPlace ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
            }
        }

        /// <summary>
        /// 최종적으로 GridManager의 승인을 받아 실제 오브젝트를 생성하고 점유 데이터를 기록하는 메서드
        /// </summary>
        void PlaceAt(Vector3 pos, int x, int z)
        {
            if (gridManager.CanPlace(x, z))
            {
                // 실제 오브젝트 생성
                Instantiate(realPrefab, pos, Quaternion.identity);

                // GridManager에 해당 좌표가 점유되었음을 기록 (중복 설치 방지)
                gridManager.PlaceObject(x, z);

                Debug.Log($"[{x}, {z}] 위치에 설치 성공");
            }
            else
            {
                Debug.Log("설치 불가 지역");
            }
        }
    }
}
