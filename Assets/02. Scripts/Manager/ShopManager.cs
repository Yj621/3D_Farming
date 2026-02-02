using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;
    [SerializeField] private GameObject shopPanel; // 상점 전체 부모 객체

    private void Awake() => Instance = this;

    // 상점 열기 (상점 버튼에 연결)
    public void OpenShop() => shopPanel.SetActive(true);

    // 상점 닫기 (X 버튼이나 아이템 선택 시 호출)
    public void CloseShop() => shopPanel.SetActive(false);
}