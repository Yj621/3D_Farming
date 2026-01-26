using System.Collections;
using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private TMP_Text tmp;
    [SerializeField] private float moveSpeed = 1.5f; // 월드 좌표 기준 속도
    [SerializeField] private float duration = 1.0f;

    private RectTransform rt;
    private Transform mainCamTransform;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (tmp == null) tmp = GetComponentInChildren<TMP_Text>(true);

        // 매 프레임 Camera.main을 호출하는 건 성능에 좋지 않으므로 미리 캐싱합니다.
        if (Camera.main != null)
            mainCamTransform = Camera.main.transform;
    }

    // 캔버스가 World Space일 때 카메라를 바라보게 함
    private void LateUpdate()
    {
        if (mainCamTransform != null)
        {
            // 방법 1: 카메라와 평행하게 회전 (가장 깔끔함)
            transform.rotation = mainCamTransform.rotation;

            // 방법 2: 카메라를 직접 쳐다보게 함 (원근감에 따라 약간씩 기울어짐)
            // transform.LookAt(transform.position + mainCamTransform.forward);
        }
    }

    public void Setup(Vector3 worldPos, string text)
    {
        if (tmp == null) tmp = GetComponentInChildren<TMP_Text>(true);
        tmp.text = text;

        // 월드 좌표로 직접 설정 (World Space Canvas인 경우)
        transform.position = worldPos;

        StopAllCoroutines();
        StartCoroutine(MoveAndReturn());
    }

    private IEnumerator MoveAndReturn()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // 위로 이동 (World Space이므로 transform.Translate 사용 가능)
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);
            yield return null;
        }

        WorldUIManager.Instance.ReturnToPool(this);
    }
}