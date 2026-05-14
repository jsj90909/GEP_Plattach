using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebuffRoot : MonoBehaviour
{
    // 인스펙터에서 프리팹을 할당하세요
    public GameObject moveLockPrefab;
    public GameObject scoreNullPrefab;

    private BlockRoot block_root = null;

    // 생성된 디버프 오브젝트들을 관리할 리스트 (나중에 삭제하기 위함)
    private List<GameObject> debuffInstances = new List<GameObject>();

    void Start()
    {
        this.block_root = this.gameObject.GetComponent<BlockRoot>();
    }

    void Update()
    {
        // Enter 키를 누르면 전체 맵에 디버프 생성 테스트
        if (Input.GetKeyDown(KeyCode.Return))
        {
            // 기존에 생성된 디버프가 있다면 제거
            ClearDebuffs();

            //HashSet<Vector2Int> positions = new HashSet<Vector2Int>();
            //for (int i = 0; i < Block.BLOCK_NUM_X; ++i)
            //{
            //    for (int j = 0; j < Block.BLOCK_NUM_Y; ++j)
            //    {
            //        Vector2Int pos = new Vector2Int(i, j);
            //        positions.Add(pos);

            //        CreateScoreNull(pos);
            //    }
            //}
            //this.block_root.SetNegativeBlockPositions(positions);

            HashSet<Vector2Int> moveLockPositions = new HashSet<Vector2Int>();
            for (int i = 0; i < Block.BLOCK_NUM_X; ++i)
            {
                    Vector2Int pos = new Vector2Int(i, 4);
                    moveLockPositions.Add(pos);
                    CreateMoveLock(pos);
            }
            this.block_root.SetMoveLockPositions(moveLockPositions);

            this.block_root.SetHeatTime(0.5f);
        }
    }

    // 이동 잠금 프리팹 생성 및 배치
    public void CreateMoveLock(Vector2Int pos)
    {
        GameObject instance = InstantiatePrefabAtGrid(moveLockPrefab, pos);
        if (instance != null)
        {
            debuffInstances.Add(instance);
        }
    }

    public void CreateMoveLock(HashSet<Vector2Int> positions)
    {
        foreach (Vector2Int pos in positions)
        {
            CreateMoveLock(pos);
        }
    }

    // 점수 무효화 프리팹 생성 및 배치
    public void CreateScoreNull(Vector2Int pos)
    {
        GameObject instance = InstantiatePrefabAtGrid(scoreNullPrefab, pos);
        if (instance != null)
        {
            debuffInstances.Add(instance);
        }
    }

    public void CreateScoreNull(HashSet<Vector2Int> positions)
    {
        foreach (Vector2Int pos in positions)
        {
            CreateScoreNull(pos);
        }
    }

    // 공통 생성 로직: 좌표 계산 및 생성
    private GameObject InstantiatePrefabAtGrid(GameObject prefab, Vector2Int pos)
    {
        if (prefab == null) return null;

        Block.iPosition iPos;
        iPos.x = pos.x;
        iPos.y = pos.y;

        // BlockRoot의 정적 메서드로 월드 좌표 계산
        Vector3 worldPos = BlockRoot.calcBlockPosition(iPos);

        // 블록(Z=0)보다 카메라 쪽에 가깝게 배치
        worldPos.z = -0.6f;

        // 프리팹 생성 (부모를 지정하지 않아 정적으로 유지됨)
        GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity);
        return obj;
    }

    public void InstantiateDebuff(string debuffType, HashSet<Vector2Int> pos)
    {
        switch (debuffType)
        {
            case "MoveLock":
                CreateMoveLock(pos);
                break;
            case "NegativeBlock":
                CreateScoreNull(pos);
                break;
            default:
                Debug.LogWarning("Unknown debuff type: " + debuffType);
                break;
        }
    }

    // 화면상의 모든 디버프 오브젝트 삭제
    public void ClearDebuffs()
    {
        foreach (GameObject obj in debuffInstances)
        {
            if (obj != null) Destroy(obj);
        }
        debuffInstances.Clear();
    }
}