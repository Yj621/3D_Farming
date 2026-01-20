using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 0.5f;
    [SerializeField] private float mouseMoveSpeed = 20f; // 에디터용 드래그 속도

    [Header("줌 설정")]
    [SerializeField] private float zoomSpeed = 0.1f;
    [SerializeField] private float mouseZoomSpeed = 500f; // 에디터용 휠 속도
    [SerializeField] private float minHeight = 5f;
    [SerializeField] private float maxHeight = 40f;

    private Vector2 lastMousePos;

    private void Update()
    {
        HandleMovement();
        HandleZoom();

        // 다음 프레임 계산을 위해 마우스 위치 저장
        lastMousePos = Input.mousePosition;
    }

    void HandleMovement()
    {
        // 1. 모바일 드래그 (한 손가락)
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;
                // 모바일은 Y축 대신 Z축으로 이동해야 바닥을 따라 움직입니다.
                Vector3 move = new Vector3(-delta.x * moveSpeed, 0, -delta.y * moveSpeed) * Time.deltaTime;
                transform.Translate(move, Space.World);
            }
        }
        // 2. 에디터용 우클릭 드래그 (마우스 우클릭 유지 시)
        else if (Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - (Vector3)lastMousePos;
            Vector3 move = new Vector3(-delta.x * mouseMoveSpeed * 0.01f, 0, -delta.y * mouseMoveSpeed * 0.01f);
            transform.Translate(move, Space.World);
        }
    }

    void HandleZoom()
    {
        float deltaDist = 0;

        // 1. 휠 입력은 터치 여부와 상관없이 항상 체크 (에디터 테스트용)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            deltaDist = scroll * mouseZoomSpeed;
        }

        // 2. 만약 터치가 2개라면 핀치 줌으로 deltaDist 덮어쓰기
        if (Input.touchCount == 2)
        {
            Touch t1 = Input.GetTouch(0);
            Touch t2 = Input.GetTouch(1);
            float currentDist = Vector2.Distance(t1.position, t2.position);
            float prevDist = Vector2.Distance(t1.position - t1.deltaPosition, t2.position - t2.deltaPosition);
            deltaDist = (currentDist - prevDist) * zoomSpeed;
        }

        // 3. 최종 적용 (이 부분은 동일)
        if (Mathf.Abs(deltaDist) > 0.0001f)
        {
            Vector3 zoomDir = Camera.main.transform.forward * deltaDist;
            Vector3 nextPos = transform.position + zoomDir;

            if (nextPos.y >= minHeight && nextPos.y <= maxHeight)
            {
                transform.position = nextPos;
            }
        }
    }
}