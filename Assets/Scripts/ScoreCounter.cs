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

    public int QUOTA_SCORE = 10000; // 클리어 하는 데 필요한 점수

    public GUIStyle guistyle; // 폰트 스타일

    //public int[] block_scores; // 블록 점수
    public int default_block_score = 100; // 블록 기본 점수

    public bool[] boss_nullified_colors; // 보스 기믹 등으로 인한 0점 처리 여부
    public int[] joker_score_overrides;  // 조커로 인한 점수 고정 값 (0이면 적용 안 됨)

    private int current_multiplier = 1;
    private float multiplier_timer = 0.0f;
    public float MultiplierTimer => multiplier_timer;
    public int CurrentMultiplier => current_multiplier;

    private StageManager stage_manager;

    void Start()
    {
        //QUOTA_SCORE = 10000;

        this.last.ignite = 0;
        this.last.score = 0;
        this.last.total_socre = 0;

        this.guistyle.fontSize = 16;

        //block_scores = new int[(int)Block.COLOR.NUM];

        //for (int i = 0; i < block_scores.Length; ++i)
        //{
        //    block_scores[i] = default_block_score;
        //}

        boss_nullified_colors = new bool[(int)Block.COLOR.NUM];
        joker_score_overrides = new int[(int)Block.COLOR.NUM];

        current_multiplier = 1;
        multiplier_timer = 0.0f;

        stage_manager = this.gameObject.GetComponent<StageManager>();
    }

    void Update()
    {
        if (this.multiplier_timer > 0.0f)
        {
            this.multiplier_timer -= Time.deltaTime;

            // 시간이 만료되면 배수 초기화
            if (this.multiplier_timer <= 0.0f)
            {
                this.current_multiplier = 1;
                this.multiplier_timer = 0.0f;
                Debug.Log("[ScoreCounter] 점수 배수 버프 효과가 종료되었습니다.");
            }
        }
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

    public void addIgniteCount2(int count, int[] blockcolors, Block.COLOR last_matched_color)
    {
        if (stage_manager.IsBossStage())
        {
            if (last_matched_color == stage_manager.GetBossZeroScoreColorIndex())
            {
                this.last.score = 0;
                //this.last.ignite = 0;
                return;
            }
        }
        this.last.ignite += count; // 연쇄 수에 count를 합산

        int[] finalscore = new int[blockcolors.Length];

        for (int i = 0; i < blockcolors.Length; ++i)
        {
            //finalscore[i] = block_scores[i] * blockcolors[i];
            finalscore[i] = GetBlockScore(i) * blockcolors[i];
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
        this.last.score = this.last.ignite * default_block_score; // 점수 갱신
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

    // 다음 스테이지로 넘어갈 때 버프/디버프 초기화용 메서드
    public void ResetStageModifiers()
    {
        for (int i = 0; i < (int)Block.COLOR.NUM; ++i)
        {
            boss_nullified_colors[i] = false;
            joker_score_overrides[i] = 0;
        }

        this.current_multiplier = 1;
        this.multiplier_timer = 0.0f;
    }

    // 우선순위에 따른 블록 점수 계산
    public int GetBlockScore(int color_index)
    {
        // 1순위: 보스 및 디버프로 인한 무효화 (무조건 0점, 0에 무엇을 곱해도 0)
        if (boss_nullified_colors[color_index])
        {
            return 0;
        }

        int calculated_score = default_block_score;

        // 2순위: 조커로 인한 점수 고정 변경 적용
        if (joker_score_overrides[color_index] > 0)
        {
            calculated_score = joker_score_overrides[color_index];
        }

        // 3순위: 아이템 버프로 인한 최종 점수 배수 적용
        return calculated_score * current_multiplier;
    }

    public void ActivateScoreMultiplier(int multiplier, float duration)
    {
        this.current_multiplier = multiplier;
        this.multiplier_timer = duration;
        Debug.Log($"[ScoreCounter] 점수 {multiplier}배 버프 활성화! 지속시간: {duration}초");
    }

    public string GetQuotaScoreText()
    {
        return $"{QUOTA_SCORE}";
    }
}