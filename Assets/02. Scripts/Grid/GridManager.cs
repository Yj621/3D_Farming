using UnityEngine;

/// <summary>
/// 농장의 물리적인 공간 데이터를 관리하는 클래스
/// 어떤 타일이 비어있고, 어떤 타일이 점유되었는지를 2차원 배열로 저장함
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("농장 크기 설정")]
    public int width = 10;  // 가로 타일 개수
    public int height = 10; // 세로 타일 개수

    // 타일의 점유 상태를 저장하는 2차원 배열 데이터 구조
    // false : 비어있음 (설치 가능)
    // true  : 이미 무언가 설치됨 (설치 불가)
    private bool[,] isOccupied;

    /// <summary>
    /// 게임 시작 시 설정된 크기에 맞춰 배열 메모리를 할당
    /// </summary>
    private void Awake()
    {
        // width x height 크기의 바둑판 모양 데이터를 생성
        isOccupied = new bool[width, height];
    }

    /// <summary>
    /// 특정 좌표(x, z)에 오브젝트를 설치할 수 있는지 검사
    /// </summary>
    /// <param name="x">검사할 그리드 X 인덱스</param>
    /// <param name="z">검사할 그리드 Z 인덱스</param>
    /// <returns>설치 가능하면 true, 불가능하면 false</returns>
    public bool CanPlace(int x, int z)
    {
        // 인덱스가 맵의 범위를 벗어났는지 먼저 체크 (배열 오류 방지)
        if (x < 0 || x >= width || z < 0 || z >= height)
        {
            return false; // 맵 밖은 설치 불가
        }

        // 해당 칸의 점유 상태를 반전시켜 반환
        // !isOccupied[x, z] 의 의미:
        // 점유(true)면 설치불가(false) 반환, 비었으면(false) 설치가능(true) 반환
        return !isOccupied[x, z];
    }

    /// <summary>
    /// 오브젝트 설치가 확정되었을 때 해당 칸의 상태를 '점유됨'으로 변경
    /// </summary>
    /// <param name="x">점유할 그리드 X 인덱스</param>
    /// <param name="z">점유할 그리드 Z 인덱스</param>
    public void PlaceObject(int x, int z)
    {
        // 맵 범위 안일 때만 실행
        if (x >= 0 && x < width && z >= 0 && z < height)
        {
            isOccupied[x, z] = true;
            Debug.Log($"데이터 업데이트: [{x}, {z}] 지점이 점유되었습니다.");
        }
    }
}
