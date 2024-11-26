using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    public int width = 30; // 미로 너비
    public int height = 30; // 미로 높이
    public int difficulty = 1; // 난이도 (1: 쉬움, 2: 중간, 3: 어려움)
    public GameObject[] prefabs; // 프리팹 배열 (1~15번 프리팹)

    private int[,] maze;

    void Start()
    {
        GenerateMaze(); // 미로 데이터 생성
        DrawMaze();     // 미로를 화면에 배치
    }

    void GenerateMaze()
    {
        maze = new int[width, height];
        Dictionary<int, List<int>> cellSets = new Dictionary<int, List<int>>(); // 세트 관리
        int setCounter = 1;

        // 초기 세트 할당 (첫 번째 행)
        for (int x = 0; x < width; x++)
        {
            maze[x, 0] = setCounter;
            cellSets[setCounter] = new List<int> { x };
            setCounter++;
        }

        for (int y = 0; y < height - 1; y++)
        {
            // 1. 수평 벽 제거: 난이도에 따른 확률로 세트를 병합
            for (int x = 0; x < width - 1; x++)
            {
                if (Random.value < GetWallRemoveChance())
                {
                    int set1 = maze[x, y];
                    int set2 = maze[x + 1, y];

                    if (set1 != set2)
                    {
                        MergeSets(set1, set2, cellSets);
                        maze[x + 1, y] = set1;
                    }
                }
            }

            // 2. 수직 벽 제거: 각 세트에 적어도 하나의 연결을 보장
            Dictionary<int, bool> connected = new Dictionary<int, bool>();
            for (int x = 0; x < width; x++)
            {
                int currentSet = maze[x, y];
                if (!connected.ContainsKey(currentSet)) connected[currentSet] = false;

                if (Random.value < GetWallRemoveChance() || !connected[currentSet])
                {
                    maze[x, y + 1] = currentSet;
                    connected[currentSet] = true;
                }
            }

            // 3. 새로운 행 초기화: 수직 벽으로 분리된 칸은 새로운 세트로 설정
            for (int x = 0; x < width; x++)
            {
                if (maze[x, y + 1] == 0)
                {
                    maze[x, y + 1] = setCounter;
                    cellSets[setCounter] = new List<int> { x };
                    setCounter++;
                }
                else
                {
                    cellSets[maze[x, y + 1]].Add(x);
                }
            }
        }

        // 마지막 행 처리: 모든 세트를 병합
        for (int x = 0; x < width - 1; x++)
        {
            int set1 = maze[x, height - 1];
            int set2 = maze[x + 1, height - 1];

            if (set1 != set2)
            {
                MergeSets(set1, set2, cellSets);
            }
        }
    }

    void MergeSets(int set1, int set2, Dictionary<int, List<int>> cellSets)
    {
        foreach (int cell in cellSets[set2])
        {
            cellSets[set1].Add(cell);
            maze[cell, 0] = set1;
        }
        cellSets.Remove(set2);
    }

    float GetWallRemoveChance()
    {
        switch (difficulty)
        {
            case 1: return 0.7f; // 쉬움: 많은 벽 제거
            case 2: return 0.5f; // 중간
            case 3: return 0.3f; // 어려움: 적은 벽 제거
            default: return 0.5f;
        }
    }

    void DrawMaze()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int prefabIndex = GetPrefabIndex(x, y); // 현재 위치의 프리팹 결정
                if (prefabIndex >= 0 && prefabIndex < prefabs.Length)
                {
                    Instantiate(prefabs[prefabIndex], new Vector3(x, y, 0), Quaternion.identity);
                }
            }
        }
    }

    /// <summary>
    /// 0b0000 (0)	전부 뚫림			        1번
    /// 0b1000 (8)	위쪽만 막힘		            2번
    /// 0b1100 (12)	위쪽, 왼쪽 막힘		        3번
    /// 0b1110 (14)	위쪽, 왼쪽, 아래쪽 막힘	    4번
    /// 0b1101 (13)	위쪽, 왼쪽, 오른쪽 막힘	    5번
    /// 0b1001 (9)	위쪽, 오른쪽 막힘		    6번
    /// 0b1011 (11)	위쪽, 오른쪽, 아래쪽 막힘	7번
    /// 0b0100 (4)	왼쪽만 막힘		            8번
    /// 0b0110 (6)	왼쪽, 아래쪽 막힘		    9번
    /// 0b0111 (7)	왼쪽, 아래쪽, 오른쪽 막힘	10번
    /// 0b0101 (5)	왼쪽, 오른쪽 막힘		    11번
    /// 0b0001 (1)	오른쪽만 막힘		        12번
    /// 0b0011 (3)	오른쪽, 아래쪽 막힘	        13번
    /// 0b0010 (2)	아래쪽만 막힘		        14번
    /// 0b1010 (10)	아래쪽, 위쪽 막힘		    15번
    /// 
    /// </summary>
    /// <param name="x">가로(열) 방향의 좌표, 0일 때 벽, 1 이상일 때 길로 간주</param>
    /// <param name="y">세로(행) 방향의 좌표, 0일 때 벽, 1 이상일 때 길로 간주</param>
    /// <returns></returns>
    int GetPrefabIndex(int x, int y)
    {
        // 현재 칸의 벽 상태를 비트 플래그로 계산
        int wallStatus = 0;
        if (y + 1 >= height || maze[x, y + 1] == 0) wallStatus |= 0b1000; // 위쪽 막힘
        if (x - 1 < 0 || maze[x - 1, y] == 0) wallStatus |= 0b0100;       // 왼쪽 막힘
        if (y - 1 < 0 || maze[x, y - 1] == 0) wallStatus |= 0b0010;       // 아래쪽 막힘
        if (x + 1 >= width || maze[x + 1, y] == 0) wallStatus |= 0b0001;  // 오른쪽 막힘

        // 벽 상태에 따른 프리팹 인덱스 반환
        return wallStatus; // wallStatus 값은 1~15 범위
    }
}
