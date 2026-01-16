using System;
using UnityEngine;

public class GridSystem : MonoBehaviour
{
    public float cellSize = 1f;
    private GridManager gridManager;

    [Header("프리팹")]
    public GameObject previewPrefab; //반투명한 미리보기용 프리팹
    public GameObject realPrefab;

    private GameObject previewInstance;

    void Start()
    {
        if (previewPrefab != null)
        {
            previewInstance = Instantiate(previewPrefab);
            previewPrefab.SetActive(false);

            // TryGetComponent: GameObject에 존재하는 경우 지정된 유형의 컴포넌트를 검색하려고 시도하고,
            // 발견되면 true, 발견되지 않으면 false를 반환한다.
            //public bool TryGetComponent<T>(out T component) where T : Component;
            /* T: 가져오려는 컴포넌트의 타입
            component: 컴포넌트를 가져올 때 사용되는 out 매개변수 */
            if (previewInstance.TryGetComponent<Collider>(out Collider col)) col.enabled = false;
        }
        if (gridManager == null) gridManager = FindAnyObjectByType<GridManager>();
    }
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        //Ray가 어떤 Collider에 맞으면 true를 반환하고, 그 충돌 정보는 hit에 담긴다.
        if (Physics.Raycast(ray, out hit))
        {
            int xIdx = Mathf.FloorToInt(hit.point.x / cellSize);
            int zIdx = Mathf.FloorToInt(hit.point.z / cellSize);

            Vector3 snapPos = new Vector3(xIdx * cellSize + (cellSize / 2), 0, cellSize + (cellSize / 2));

            HandlePreview(snapPos, xIdx, zIdx);

            if (Input.GetMouseButtonDown(0))
            {
                PlaceAt(snapPos, xIdx, zIdx);
            }
        }
        else
        {
            if (previewInstance != null)
            {
                previewInstance.SetActive(false);
            }
        }

        void HandlePreview(Vector3 pos, int x, int z)
        {
            if (previewInstance == null)
            {
                return;
            }
            previewInstance.SetActive(true);
            previewInstance.transform.position = pos;


        }
    }
}
