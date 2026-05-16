using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    // 싱글톤 패턴으로 어디서든 쉽게 접근 가능하도록 설정
    public static StageManager Instance { get; private set; }

    public enum MISSION_TYPE
    {
        TIME_LIMIT,
        MOVE_LIMIT
    }

    private BlockRoot block_root;
    private SceneControl scene_control;
    private ShopRoot shop_root;
    private ScoreCounter score_counter;
    private DebuffRoot debuff_root;

    public int current_stage = 1; // 현재 스테이지 레벨

    // 현재 스테이지 미션 정보
    public MISSION_TYPE current_mission = MISSION_TYPE.TIME_LIMIT;

    public float time_limit = 60.0f;

    public int max_moves = 20;
    public int current_moves = 20;

    // 보스 스테이지 설정
    private static readonly Block.COLOR boss_zero_score_color = Block.COLOR.YELLOW;
    private const float boss_target_probability = 0.45f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        CacheComponents();
    }

    private void CacheComponents()
    {
        // 동일한 게임 오브젝트에 붙어있는 매니저 스크립트들을 캐싱
        block_root = GetComponent<BlockRoot>();
        scene_control = GetComponent<SceneControl>();
        shop_root = GetComponent<ShopRoot>();
        score_counter = GetComponent<ScoreCounter>();
        debuff_root = GetComponent<DebuffRoot>();
    }

    private void EnsureComponents()
    {
        if (block_root == null) block_root = GetComponent<BlockRoot>();
        if (scene_control == null) scene_control = GetComponent<SceneControl>();
        if (shop_root == null) shop_root = GetComponent<ShopRoot>();
        if (score_counter == null) score_counter = GetComponent<ScoreCounter>();
        if (debuff_root == null) debuff_root = GetComponent<DebuffRoot>();
    }

    // 상점에서 '다음 스테이지 시작'을 누르면 호출될 메인 함수
    public void NextStage()
    {
        EnsureComponents();

        current_stage++;
        Debug.Log("스테이지 " + current_stage + " 시작!");

        // 1. 상점 UI 닫기
        shop_root.step = ShopRoot.STEP.CLOSED;

        // 2. 기존 보드 블록 파괴 및 디버프 초기화
        block_root.ClearBoard();
        debuff_root.ClearDebuffs();
        block_root.ClearNegativeBlockPositions();
        block_root.ClearMoveLockPositions();

        // 3, 4. 현재 스테이지에 맞는 미션 / 목표 점수 / 레벨 설정
        SetupStage(current_stage);

        // 5. 상점에서 구매한 다음 스테이지 적용 효과 처리
        shop_root.ApplyPendingEffects();

        // 6. 새로운 보드 생성 (블록 재배치)
        block_root.initialSetUp();

        // 7. 게임 상태(입력 활성화, 타이머) 초기화 및 플레이 재개
        scene_control.ResetForNextStage();
    }

    // 현재 스테이지 번호에 따라 미션, 목표 점수, 레벨을 설정
    public void SetupStage(int stage)
    {
        EnsureComponents();

        switch (stage)
        {
            case 1:
                // 1스테이지: 시간 제한 미션
                current_mission = MISSION_TYPE.TIME_LIMIT;

                time_limit = 60.0f;
                max_moves = 0;
                current_moves = 0;

                score_counter.ResetStageScore(10000);

                if (block_root.level_control != null)
                {
                    block_root.level_control.selectLevel(0);
                }
                break;

            case 2:
                // 2스테이지: 이동 횟수 제한 미션
                current_mission = MISSION_TYPE.MOVE_LIMIT;

                max_moves = 20;
                current_moves = max_moves;

                score_counter.ResetStageScore(30000);

                if (block_root.level_control != null)
                {
                    block_root.level_control.selectLevel(1);
                }
                break;

            case 3:
                // 3스테이지: 보스 스테이지
                current_mission = MISSION_TYPE.MOVE_LIMIT;

                max_moves = 15;
                current_moves = max_moves;

                score_counter.ResetStageScore(50000);

                if (block_root.level_control != null)
                {
                    block_root.level_control.selectLevel(2);
                }

                // 보스 스테이지 효과 적용
                ApplyBossStageRule();

                break;

            default:
                Debug.LogWarning("[StageManager] 준비되지 않은 스테이지입니다: " + stage);
                break;
        }

        Debug.Log("[StageManager] Stage " + stage + " 설정 완료 / Mission: " + current_mission);
    }

    // 보스 스테이지 전용 규칙
    private void ApplyBossStageRule()
    {
        EnsureComponents();

        if (block_root == null || score_counter == null)
        {
            Debug.LogWarning("[StageManager] 보스 스테이지 적용 실패: 컴포넌트가 없습니다.");
            return;
        }

        // ScoreCounter의 block_scores가 아직 준비되지 않았을 가능성 대비
        if (score_counter.block_scores == null || score_counter.block_scores.Length != (int)Block.COLOR.NUM)
        {
            score_counter.block_scores = new int[(int)Block.COLOR.NUM];

            for (int i = 0; i < score_counter.block_scores.Length; i++)
            {
                score_counter.block_scores[i] = score_counter.default_block_score;
            }
        }

        // 1. 특정 블록 점수를 0점으로 변경
        block_root.SetBlockScore(boss_zero_score_color, 0);

        // 2. 특정 블록 확률 증가 + 나머지 블록 확률 균등 분배
        if (block_root.level_control != null)
        {
            block_root.SetProbabilityAndDistributeEqually(
                boss_zero_score_color,
                boss_target_probability
            );
        }

        Debug.Log(
            "[StageManager] 보스 스테이지 적용 완료 / " +
            boss_zero_score_color + " 점수 0 / 확률 " + boss_target_probability
        );
    }

    // 블록을 실제로 한 번 이동했을 때 호출
    public void UseMove()
    {
        if (current_mission != MISSION_TYPE.MOVE_LIMIT)
        {
            return;
        }

        if (current_moves > 0)
        {
            current_moves--;
        }

        Debug.Log("남은 이동 횟수: " + current_moves);
    }

    public bool IsTimeLimitMission()
    {
        return current_mission == MISSION_TYPE.TIME_LIMIT;
    }

    public bool IsMoveLimitMission()
    {
        return current_mission == MISSION_TYPE.MOVE_LIMIT;
    }

    public bool IsTimeOver(float current_time)
    {
        if (!IsTimeLimitMission())
        {
            return false;
        }

        return current_time >= time_limit;
    }

    public bool IsMoveOver()
    {
        if (!IsMoveLimitMission())
        {
            return false;
        }

        return current_moves <= 0;
    }

    public string GetMissionText()
    {
        switch (current_mission)
        {
            case MISSION_TYPE.TIME_LIMIT:
                return "제한 시간 안에\n점수 " + ScoreCounter.QUOTA_SCORE.ToString() + "점 도달";

            case MISSION_TYPE.MOVE_LIMIT:
                return "이동 횟수 " + max_moves.ToString() + "번 안에\n점수 " + ScoreCounter.QUOTA_SCORE.ToString() + "점 도달";
        }

        return "";
    }

    public string GetLimitText(float current_time)
    {
        switch (current_mission)
        {
            case MISSION_TYPE.TIME_LIMIT:
                int remain_time = Mathf.Max(0, Mathf.CeilToInt(time_limit - current_time));
                return "남은 시간 : " + remain_time.ToString() + "초";

            case MISSION_TYPE.MOVE_LIMIT:
                return "남은 이동 횟수 : " + current_moves.ToString();
        }

        return "";
    }

    public bool IsFinalStage()
    {
        return current_stage >= 3;
    }

    public void PlusCurrentMoves(int amount)
    {
        if (!IsMoveLimitMission())
        {
            return;
        }

        current_moves += amount;
        Debug.Log("이동 횟수 증가: " + amount + " / 현재 이동 횟수: " + current_moves);
    }
}