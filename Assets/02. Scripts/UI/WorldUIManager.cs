using System.Collections.Generic;
using UnityEngine;

public class WorldUIManager : MonoBehaviour
{
    public static WorldUIManager Instance;

    [Header("참조")]
    [SerializeField] private Canvas worldCanvas;
    
    [Header("풀링 설정")]
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private int poolSize = 20;

    // 비활성화된 객체들을 담아둘 큐
    private Queue<FloatingText> textPool = new Queue<FloatingText>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewTextInstance();
        }
    }

    private void CreateNewTextInstance()
    {
        GameObject obj = Instantiate(floatingTextPrefab, worldCanvas.transform);
        obj.SetActive(false);
        textPool.Enqueue(obj.GetComponent<FloatingText>());
    }

    // 풀에서 텍스트 꺼내기
    public void ShowFloatingText(Vector3 position, string text)
    {
        FloatingText ft = null;

        if (textPool.Count > 0) ft = textPool.Dequeue();
        else 
        {
            // 풀이 모자라면 새로 생성
            GameObject obj = Instantiate(floatingTextPrefab, worldCanvas.transform);
            ft = obj.GetComponent<FloatingText>();
        }

        ft.gameObject.SetActive(true);
        ft.Setup(position, text);
    }

    // 사용 완료 후 풀에 반납
    public void ReturnToPool(FloatingText ft)
    {
        ft.gameObject.SetActive(false);
        textPool.Enqueue(ft);
    }

    public Transform CanvasTransform => worldCanvas.transform;
}