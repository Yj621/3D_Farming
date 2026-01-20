using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f; // 위로 올라가는 속도
    [SerializeField] private float destoryTime = 1f; // 사라지는 시간

    private TextMeshProUGUI floatText;
    private Color alpha;

    private void Start()
    {
        floatText = GetComponent<TextMeshProUGUI>();
        alpha = floatText.color;

        // 지정된 시간 뒤에 스스로 삭제
        Destroy(gameObject, destoryTime);
    }

    private void Update()
    {
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

        // 서서히 투명해지기
        alpha.a = Mathf.Lerp(alpha.a , 0, Time.deltaTime * ( 1/ destoryTime ) );
        floatText.color = alpha;
    }
}
