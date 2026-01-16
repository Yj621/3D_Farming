using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header ("농장 크기")]
    public int width = 10;
    public int height = 10;

    //타일 점유 상태를 저장하는 2차원 배열(false : 비어있음, true : 설치됨)
    private bool[,] isOccupied;

    private void Awake()
    {
        isOccupied = new bool[width, height];
    }

    /// <summary>
    /// 설치 가능한지 체크하는 함수
    /// </summary>
    public bool CanPlace(int x, int z)
    {
        if(x < 0 || x >= width || z < 0 || z >= height)
            return false;
        return !isOccupied[x, z];
    }

    public void PlaceObject(int x, int z)
    {
        isOccupied[x, z] = true;
    }
}
