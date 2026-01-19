using UnityEngine;

public enum TileType { Empty, Mud, Crop };

/// <summary>
/// 농장의 물리적인 공간 데이터를 관리하는 클래스
/// 어떤 타일이 비어있고, 어떤 타일이 점유되었는지를 2차원 배열로 저장함
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("농장 크기 설정")]
    public int width = 10;  // 가로 타일 개수
    public int height = 10; // 세로 타일 개수

    private TileType[,] tileTypes; // bool 대신 TileType 사용

    /// <summary>
    /// 게임 시작 시 설정된 크기에 맞춰 배열 메모리를 할당
    /// </summary>
    private void Awake()
    {
        tileTypes = new TileType[width, height];
    }

    /// <summary>
    /// 특정 좌표(x, z)에 오브젝트를 설치할 수 있는지 검사
    /// </summary>
    /// <param name="x">검사할 그리드 X 인덱스</param>
    /// <param name="z">검사할 그리드 Z 인덱스</param>
    /// <returns>해당 칸이 Empty일때만 true, 불가능하면 false</returns>
    public bool CanPlace(int x, int z)
    {
        // 맵 범위 체크
        if (x < 0 || x >= width || z < 0 || z >= height)
        {
            return false;
        }

        // 해당 칸이 Empty 상태일 때만 true 반환
        // (isOccupied 배열을 삭제했으므로 tileTypes만 확인하면 됩니다)
        return tileTypes[x, z] == TileType.Empty;
    }

    /// <summary>
    /// 오브젝트 설치가 확정되었을 때 데이터 갱신
    /// </summary>
    public void PlaceObject(int x, int z, TileType type)
    {
        // 맵 범위 안일 때만 실행
        if (x >= 0 && x < width && z >= 0 && z < height)
        {
            tileTypes[x, z] = type;
            Debug.Log($"<color=yellow>데이터 갱신:</color> [{x},{z}] 번지 타일이 {type} 상태가 되었습니다.");
        }
    }

    /// <summary>
    /// 현재 타일의 타입을 반환하는 함수
    /// </summary>
    public TileType GetTileType(int x, int z)
    {
        if(x < 0 || x >= width || z < 0 || z>=height) return TileType.Empty;
        return tileTypes[x, z];
    }
}
