using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f; 
    [SerializeField] private float destoryTime = 1f; 
    private Transform camTransform; 
    private TextMeshProUGUI floatText;
    private Color originalColor;
    private float timer;

    private void Awake()
    {
        floatText = GetComponent<TextMeshProUGUI>();
        originalColor = floatText.color;
        if (Camera.main != null) camTransform = Camera.main.transform;
    }

    // 풀에서 꺼낼 때 초기화하는 함수
    public void Setup(Vector3 position, string text)
    {
        transform.position = position + Vector3.up; // 약간 위에서 생성
        floatText.text = text;
        floatText.color = originalColor;
        timer = 0;
    }

    private void Update()
    {
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime, Space.World);

        timer += Time.deltaTime;
        float progress = timer / destoryTime;

        // 투명도 조절
        Color c = floatText.color;
        c.a = Mathf.Lerp(originalColor.a, 0, progress);
        floatText.color = c;

        // 시간이 다 되면 풀로 반납
        if (timer >= destoryTime)
        {
            WorldUIManager.Instance.ReturnToPool(this);
        }
    }

    private void LateUpdate()
    {
        if (camTransform == null) return;
        transform.LookAt(transform.position + camTransform.forward);
    }
}