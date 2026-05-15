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
            this.score_counter.block_scores[(int)Block.COLOR.BLUE] = 10000; // 블록 점수 변경 조커
            //this.block_root.SetProbability(Block.COLOR.MAGENTA, 15); // 블록 색상 확률 변경 조커
            //this.block_root.SetExactProbability(Block.COLOR.MAGENTA, 0.15f);
            this.block_root.SetEqualProbabilities(); // 모든 블록 색상 확률을 동일하게 조커
            //this.block_root.SetProbabilityAndDistributeEqually(Block.COLOR.MAGENTA, 0.5f); // 특정 블록 색상 확률을 변경하고 나머지 블록 색상 확률을 동일하게 조커

            Debug.Log("Block Scores: " + FormatBlockScores());
        }
    }

    private string FormatBlockScores()
    {
        string[] block_score_strings = new string[this.score_counter.block_scores.Length];
        for (int i = 0; i < this.score_counter.block_scores.Length; ++i)
            block_score_strings[i] = $"{(Block.COLOR)i}: {this.score_counter.block_scores[i]}";

        return string.Join(" | ", block_score_strings);
    }
}
