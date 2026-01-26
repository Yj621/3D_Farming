using UnityEngine;
using UnityEngine.UI;

public class CropUI : MonoBehaviour
{
    [Header("작물 성장률 관련")]
    [SerializeField] private SpriteRenderer sr;
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
    }


    public void SetFillAmount(float amount)
    {
        if (sr == null) return;

        amount = Mathf.Clamp01(amount);
        mpb.SetFloat(FillAmountId, amount);
        sr.SetPropertyBlock(mpb);
    }

}