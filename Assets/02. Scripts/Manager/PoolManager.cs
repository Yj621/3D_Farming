using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 풀에서 꺼낼 때/돌려보낼 때 필요한 초기화/정리를 구현하는 인터페이스
/// </summary>
public interface IPoolable
{
    void OnGetFromPool();
    void OnReturnToPool();
}

/// <summary>
/// 프리팹을 미리 생성해두고(Get) / 다시 돌려보내는(Release) 간단 풀 매니저
/// Instantiate/Destroy 반복을 줄여 성능과 GC를 개선한다
/// </summary>
/// 
public class PoolManager : MonoBehaviour
{

    [System.Serializable]
    public class PoolConfig
    {
        public string key;          // 풀을 구분하는 키 (예: "MUD", "CROP")
        public GameObject prefab;   // 풀링할 대상 프리팹
        public int prewarm = 20;    // 시작 시 미리 만들어둘 개수
    }

    [SerializeField] private List<PoolConfig> pools = new List<PoolConfig>();
    [SerializeField] private Transform poolRoot; // 비활성 오브젝트 보관 부모(없으면 this)

    // key -> 비활성 오브젝트 큐
    private readonly Dictionary<string, Queue<GameObject>> dict = new();

    // 생성된 오브젝트 -> 어떤 key 풀 소속인지 역참조
    private readonly Dictionary<GameObject, string> reverseKey = new();

    // key -> 프리팹 참조
    private readonly Dictionary<string, GameObject> prefabByKey = new();

    public static PoolManager Instance { get; private set; }

    void Awake()
    {
        // 싱글톤 유지
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // poolRoot가 비어있으면 자기 자신을 루트로 사용
        if (poolRoot == null) poolRoot = transform;

        // 등록 + 프리워밍
        foreach (var p in pools)
        {
            if (p.prefab == null || string.IsNullOrEmpty(p.key)) continue;

            dict[p.key] = new Queue<GameObject>(p.prewarm);
            prefabByKey[p.key] = p.prefab;

            // 미리 생성 후 Release로 큐에 쌓기
            for (int i = 0; i < p.prewarm; i++)
            {
                var go = CreateNew(p.key);
                Release(go);
            }
        }
    }

    /// <summary>
    /// 해당 key의 프리팹으로 새 오브젝트를 생성 (풀 부족 시 확장용)
    /// </summary>
    GameObject CreateNew(string key)
    {
        var prefab = prefabByKey[key];
        var go = Instantiate(prefab, poolRoot);

        // 디버깅용 이름
        go.name = $"{prefab.name} (Pooled:{key})";

        // 역참조 테이블에 등록
        reverseKey[go] = key;

        // 기본 비활성 상태로 시작
        go.SetActive(false);
        return go;
    }

    /// <summary>
    /// 풀에서 오브젝트를 꺼내 원하는 위치/회전으로 배치 후 활성화한다
    /// </summary>
    public GameObject Get(string key, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (!dict.ContainsKey(key))
        {
            Debug.LogError($"[Pool] Unknown key: {key}");
            return null;
        }

        // 큐에 있으면 꺼내고, 없으면 새로 생성(확장)
        GameObject go = dict[key].Count > 0 ? dict[key].Dequeue() : CreateNew(key);

        // 부모 지정(원하면)
        if (parent != null) go.transform.SetParent(parent, false);
        else go.transform.SetParent(null);

        // 위치/회전 적용 후 활성화
        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);

        // 풀에서 꺼낼 때 초기화 훅
        if (go.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnGetFromPool();

        return go;
    }

    /// <summary>
    /// 오브젝트를 풀로 돌려보내 비활성화하고 큐에 다시 쌓는다
    /// </summary>
    public void Release(GameObject go)
    {
        if (go == null) return;

        // 풀에서 만든 게 아니라면 그냥 파괴
        if (!reverseKey.TryGetValue(go, out var key))
        {
            Destroy(go);
            return;
        }

        // 풀로 돌아갈 때 정리 훅
        if (go.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnReturnToPool();

        // 비활성화 + 루트 아래로 정리
        go.SetActive(false);
        go.transform.SetParent(poolRoot, false);

        // 다시 큐에 넣기
        dict[key].Enqueue(go);
    }
}
