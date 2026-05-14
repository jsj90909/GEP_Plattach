using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    // 싱글톤 패턴으로 어디서든 쉽게 접근 가능하도록 설정
    public static StageManager Instance { get; private set; }

    private BlockRoot block_root;
    private SceneControl scene_control;
    private ShopRoot shop_root;
    private ScoreCounter score_counter;
    private DebuffRoot debuff_root;

    public int current_stage = 1; // 현재 스테이지 레벨

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        // 동일한 게임 오브젝트에 붙어있는 매니저 스크립트들을 캐싱
        block_root = GetComponent<BlockRoot>();
        scene_control = GetComponent<SceneControl>();
        shop_root = GetComponent<ShopRoot>();
        score_counter = GetComponent<ScoreCounter>();
        debuff_root = GetComponent<DebuffRoot>();
    }

    // 상점에서 '다음 스테이지 시작'을 누르면 호출될 메인 함수
    public void NextStage()
    {
        current_stage++;
        Debug.Log("스테이지 " + current_stage + " 시작!");

        // 1. 상점 UI 닫기
        shop_root.step = ShopRoot.STEP.CLOSED;

        // 2. 기존 보드 블록 파괴 및 디버프 초기화
        block_root.ClearBoard();
        debuff_root.ClearDebuffs();
        block_root.ClearNegativeBlockPositions();
        block_root.ClearMoveLockPositions();

        // 3. 목표 점수 갱신
        score_counter.NextStageSetup(100000);

        // 4. 레벨 난이도 변경 (스테이지에 맞춰 레벨 데이터 선택)
        // 레벨 데이터 인덱스가 부족하면 LevelControl에서 무작위 처리됨
        block_root.level_control.selectLevel(current_stage - 1);

        // 5. 상점에서 구매한 다음 스테이지 적용 효과 처리
        shop_root.ApplyPendingEffects();

        // 6. 새로운 보드 생성 (블록 재배치)
        block_root.initialSetUp();

        // 7. 게임 상태(입력 활성화, 타이머) 초기화 및 플레이 재개
        scene_control.ResetForNextStage();
    }
}