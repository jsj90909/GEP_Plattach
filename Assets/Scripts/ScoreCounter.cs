using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreCounter : MonoBehaviour
{
    public struct Count
    { // 점수 관리용 구조체
        public int ignite; // 연쇄 수
        public int score; // 점수
        public int total_socre; // 합계 점수
    };

    public Count last; // 마지막(이번) 점수
    public Count best; // 최고 점수

    public static int QUOTA_SCORE = 10000; // 클리어 하는 데 필요한 점수

    public GUIStyle guistyle; // 폰트 스타일

    public int[] block_scores; // 블록 점수
    public int default_block_score = 10; // 블록 기본 점수

    void Start()
    {
        QUOTA_SCORE = 10000;

        this.last.ignite = 0;
        this.last.score = 0;
        this.last.total_socre = 0;

        this.guistyle.fontSize = 16;

        block_scores = new int[(int)Block.COLOR.NUM];

        for (int i = 0; i < block_scores.Length; ++i)
        {
            block_scores[i] = default_block_score;
        }
    }

    void OnGUI()
    { // 화면에 텍스트와 이미지 표시
        /*
        int x = 20;
        int y = 50;
        GUI.color = Color.black;
        this.print_value(x + 20, y, "연쇄 카운트", this.last.ignite);
        y += 30;
        this.print_value(x + 20, y, "가산 스코어", this.last.score);
        y += 30;
        this.print_value(x + 20, y, "합계 스코어", this.last.total_socre);
        y += 30;
        */
    }

    // 지정된 두 개의 데이터를 두 개의 행에 나눠 표시.
    public void print_value(int x, int y, string label, int value)
    {
        /*
        GUI.Label(new Rect(x, y, 100, 20), label, guistyle); // label을 표시
        y += 15;
        GUI.Label(new Rect(x + 20, y, 100, 20), value.ToString(), guistyle); // 다음 행에 value를 표시
        y += 15;
        */
    }

    // 연쇄 횟수를 가산
    public void addIgniteCount(int count)
    {
        this.last.ignite += count; // 연쇄 수에 count를 합산
        this.update_score(); // 점수 계산
    }

    public void addIgniteCount2(int count, int[] blockcolors)
    {
        this.last.ignite += count; // 연쇄 수에 count를 합산

        int[] finalscore = new int[blockcolors.Length];

        for (int i = 0; i < blockcolors.Length; ++i)
        {
            finalscore[i] = block_scores[i] * blockcolors[i];
        }

        this.update_score2(finalscore); // 점수 계산
    }

    // 연쇄 횟수를 리셋
    public void clearIgniteCount()
    {
        this.last.ignite = 0; // 연쇄 횟수 리셋
    }

    // 더해야 할 점수를 계산
    private void update_score()
    {
        this.last.score = this.last.ignite * 10; // 점수 갱신
    }

    private void update_score2(int[] finalscore)
    {
        int sum = 0;

        for (int i = 0; i < finalscore.Length; ++i)
        {
            sum += finalscore[i];
        }

        this.last.score = this.last.ignite * sum; // 점수 갱신
    }

    // 합계 점수를 갱신
    public void updateTotalScore()
    {
        this.last.total_socre += this.last.score;
    }

    // 게임을 클리어했는지 판정
    public bool isGameClear()
    {
        bool is_clear = false;

        // 현재 합계 점수가 클리어 기준 이상이면 클리어
        if (this.last.total_socre >= QUOTA_SCORE)
        {
            is_clear = true;
        }

        return is_clear;
    }

    // 스테이지별 목표 점수 설정 및 현재 점수 리셋
    public void ResetStageScore(int quota_score)
    {
        QUOTA_SCORE = quota_score;

        this.last.ignite = 0;
        this.last.score = 0;
        this.last.total_socre = 0;

        Debug.Log("현재 스테이지 목표 점수: " + QUOTA_SCORE);
    }

    // 기존 방식 유지용: 다음 스테이지 진입 시 목표 점수 증가 및 스테이지 점수 리셋
    public void NextStageSetup(int add_quota)
    {
        // 목표 점수는 다음 스테이지 난이도에 맞춰 증가
        QUOTA_SCORE += add_quota;

        // 현재 스테이지 획득 점수 및 연쇄 수 초기화
        this.last.ignite = 0;
        this.last.score = 0;
        this.last.total_socre = 0;

        Debug.Log("다음 스테이지 목표 점수: " + QUOTA_SCORE);
    }
}