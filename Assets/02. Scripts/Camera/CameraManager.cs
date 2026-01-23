using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 1.0f;       // 모바일 터치 이동 감도
    [SerializeField] private float mouseMoveSpeed = 5.0f;  // 에디터 우클릭 드래그 감도

    [Header("Orthographic 줌 설정")]
    private Camera cam;
    [SerializeField] private float zoomSpeed = 0.02f;     // 핀치 줌 감도
    [SerializeField] private float mouseZoomSpeed = 5f;    // 휠 줌 감도
    [SerializeField] private float minSize = 5f;
    [SerializeField] private float maxSize = 20f;

    private Vector2 lastMousePos;

    private void Start()
    {
        cam = GetComponentInChildren<Camera>();
        // 초기 마우스 위치 설정
        lastMousePos = Input.mousePosition;
    }

    private void Update()
    {
        // 줌과 이동을 분리하여 실행
        HandleZoom();
        HandleMovement();

        // 마우스 버튼을 놓았을 때만 위치 업데이트 (드래그 중에는 업데이트 안함)
        if (!Input.GetMouseButton(1))
        {
            lastMousePos = Input.mousePosition;
        }
    }

    void HandleMovement()
{
    Vector3 inputDir = Vector3.zero;

    // 1. 모바일 드래그 (한 손가락)
    if (Input.touchCount == 1)
    {
        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Moved)
        {
            inputDir = new Vector3(-touch.deltaPosition.x * moveSpeed, 0, -touch.deltaPosition.y * moveSpeed);
        }
    }
    // 2. 에디터용 우클릭 드래그 (더 민감하게 수정)
    else if (Input.GetMouseButton(1))
    {
        // 현재 마우스 위치와 이전 프레임 위치의 차이 계산
        Vector2 currentMousePos = Input.mousePosition;
        Vector2 delta = currentMousePos - lastMousePos;

        // 마우스가 실제로 움직였을 때만 방향 설정
        if (delta.sqrMagnitude > 0.01f) 
        {
            inputDir = new Vector3(-delta.x * mouseMoveSpeed, 0, -delta.y * mouseMoveSpeed);
        }
    }

    if (inputDir != Vector3.zero)
    {
        // 카메라의 Y축 회전값 반영 (아이소메트릭 보정)
        Quaternion cameraRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        Vector3 worldMoveDir = cameraRotation * inputDir;

        // Orthographic Size에 따른 속도 보정
        float zoomFactor = (cam != null) ? (cam.orthographicSize / 10f) : 1f;
        
        // 이동 적용 (Time.deltaTime을 곱해 프레임 독립성 확보)
        transform.Translate(worldMoveDir * zoomFactor * Time.deltaTime, Space.World);
    }
}
    void HandleZoom()
    {
        if (cam == null) return;

        float zoomDelta = 0;

        // 휠 입력
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0) zoomDelta = -scroll * mouseZoomSpeed;

        // 핀치 입력
        else if (Input.touchCount == 2)
        {
            Touch t1 = Input.GetTouch(0);
            Touch t2 = Input.GetTouch(1);
            float currentDist = Vector2.Distance(t1.position, t2.position);
            float prevDist = Vector2.Distance(t1.position - t1.deltaPosition, t2.position - t2.deltaPosition);
            zoomDelta = -(currentDist - prevDist) * zoomSpeed;
        }

        if (Mathf.Abs(zoomDelta) > 0.001f)
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + zoomDelta, minSize, maxSize);
        }
    }
}