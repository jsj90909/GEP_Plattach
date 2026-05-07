using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Joker
{
    // 조커 클래스
}
public class JokerRoot : MonoBehaviour
{
    private ScoreCounter score_counter = null; // 점수 카운터 ScoreCounter
    private BlockRoot block_root = null; // 블록 루트 BlockRoot

    // Start is called before the first frame update
    void Start()
    {
        this.score_counter = this.gameObject.GetComponent<ScoreCounter>();
        this.block_root = this.gameObject.GetComponent<BlockRoot>();
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    for (int i = 0; i < this.score_counter.block_score.Length; ++i)
        //        this.score_counter.block_score[i] = 10;
        //}
        if (Input.GetKeyDown(KeyCode.Return))
        {
            this.score_counter.block_scores[(int)Block.COLOR.MAGENTA] = 100; // 블록 점수 변경 조커

            HashSet<Vector2Int> positions = new HashSet<Vector2Int>();
            for (int i = 0; i < Block.BLOCK_NUM_X; ++i)
            {
                for (int j = 0; j < Block.BLOCK_NUM_Y; ++j)
                {
                    positions.Add(new Vector2Int(i, j));
                }
            }
            this.block_root.SetNegativeBlockPositions(positions); // 특정 구역 점수 무효화 디버프
        }
    }
}
