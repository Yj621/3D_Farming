using UnityEngine;
using UnityEngine.UI;

public class CropUI : MonoBehaviour
{
    [SerializeField] private Slider progressSlider;
    private Transform target; 
    private Vector3 offset = new Vector3(0, 1.5f, 0); 
    private Transform camTransform;

    void Start()
    {
        if (Camera.main != null)
            camTransform = Camera.main.transform;
    }

    // 초기화: 어떤 작물을 따라갈지 설정
   public void Setup(Transform cropTransform)
{
    target = cropTransform;
    Debug.Log($"{target.name}에 UI 부착됨!"); // 로그가 찍히는지 확인
}

void LateUpdate()
{
    if (target == null)
    {
        Debug.Log("타겟이 없어서 UI 삭제됨");
        Destroy(gameObject);
        return;
    }

    // 일단 보이기 시작하는지 확인하기 위해 offset을 크게 줘보세요 (예: 3f)
    transform.position = target.position + offset;

    if (camTransform != null)
    {
        transform.LookAt(transform.position + camTransform.forward);
    }
}

    public void UpdateProgress(float value)
    {
        if (progressSlider != null)
        {
            progressSlider.value = value;
            
            // 최적화: 성장이 끝나면 UI 파괴 (또는 SetActive(false))
            if (value >= 1f) 
            {
                Destroy(gameObject);
            }
        }
    }
}