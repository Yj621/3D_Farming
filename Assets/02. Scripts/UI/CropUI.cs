using UnityEngine;
using UnityEngine.UI;

public class CropUI : MonoBehaviour
{
    [Header("작물 성장률 관련")]
    public GameObject progressbar;

    SpriteRenderer sr;
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private MaterialPropertyBlock mpb;

    [Header("플로팅 텍스트")]
    [SerializeField] private Transform floatingAnchor; // 텍스트 뜰 위치(작물 위)

    private void Awake()
    {
        sr = progressbar.GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>(true);
        if (floatingAnchor == null) floatingAnchor = transform; // 없으면 자기 자신 기준
    }


    public void SetFillAmount(float amount)
    {
        if (sr == null) return;

        amount = Mathf.Clamp01(amount);
        mpb.SetFloat(FillAmountId, amount);
        sr.SetPropertyBlock(mpb);
        progressbar.SetActive(false);
    }

    /// <summary>
    /// 골드 획득 플로팅 텍스트 표시
    /// </summary>
    /// <param name="amount"></param>
    public void ShowGold(int amount)
    {
        Debug.Log($"[ShowGold] amount={amount}, WUIMgr={(WorldUIManager.Instance != null)}");
        if (WorldUIManager.Instance == null) return;

        Vector3 pos = floatingAnchor.position + Vector3.up * 0.5f;
        WorldUIManager.Instance.ShowFloatingText(pos, $"{amount}골드");
    }

    /// <summary>
    /// 범용 플로팅 텍스트 표시
    /// </summary>
    /// <param name="text"></param>
    public void ShowText(string text)
    {
        if (WorldUIManager.Instance == null) return;

        Vector3 pos = floatingAnchor.position + Vector3.up * 0.5f;
        WorldUIManager.Instance.ShowFloatingText(pos, text);
    }

}