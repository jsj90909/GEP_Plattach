using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneControl : MonoBehaviour
{
    private BlockRoot block_root = null;

    private ScoreCounter score_counter = null;

    private ShopRoot shop_root = null; // 상점 루트 ShopRoot

    public enum STEP
    {
        NONE = -1,
        PLAY = 0,
        CLEAR,
        GAMEOVER,
        NUM,
    }; // 상태 정보 없음, 플레이 중, 클리어, 게임오버, 상태의 종류

    public STEP step = STEP.NONE; // 현재 상태
    public STEP next_step = STEP.NONE; // 다음 상태
    public float step_timer = 0.0f; // 경과 시간
    private float clear_time = 0.0f; // 클리어 시간
    public GUIStyle guistyle; // 폰트 스타일

    // 씬이 시작될 때 텍스트 데이터를 읽고 레벨 선택을 할 수 있게
    void Start()
    {
        // 필요한 컴포넌트들을 먼저 가져옴
        this.block_root = this.gameObject.GetComponent<BlockRoot>();
        this.score_counter = this.gameObject.GetComponent<ScoreCounter>();
        this.shop_root = this.gameObject.GetComponent<ShopRoot>();

        // BlockRoot의 레벨 데이터 초기화
        this.block_root.create(); // create() 메서드에서 초기 설정

        // 첫 스테이지는 NextStage()를 거치지 않으므로 여기서 직접 스테이지 설정
        if (StageManager.Instance != null)
        {
            StageManager.Instance.SetupStage(StageManager.Instance.current_stage);
        }

        // 보드를 생성하기 '전'에 상점의 이월된 효과(require_blocks 변경 등)를 미리 적용
        if (this.shop_root != null)
        {
            this.shop_root.ApplyPendingEffects();
        }

        // 변경된 상태를 기반으로 BlockRoot 스크립트의 initialSetUp() 호출
        this.block_root.initialSetUp();

        this.next_step = STEP.PLAY; // 다음 상태를 '플레이 중'으로

        if (this.guistyle == null)
        {
            this.guistyle = new GUIStyle();
        }

        this.guistyle.fontSize = 24; // 폰트 크기를 24로
    }

    void Update()
    {
        this.step_timer += Time.deltaTime;

        if (this.next_step == STEP.NONE)
        { // 상태 변화 대기
            switch (this.step)
            {
                case STEP.CLEAR:
                    // 마지막 스테이지 클리어 후에는 클릭하면 타이틀로 이동
                    if (StageManager.Instance != null && StageManager.Instance.IsFinalStage())
                    {
                        if (Input.GetMouseButtonDown(0))
                        {
                            SceneManager.LoadScene("TitleScene");
                        }
                    }
                    // ShopRoot가 없을 때만 기존처럼 타이틀로 돌아가게 함
                    else if (this.shop_root == null)
                    {
                        if (Input.GetMouseButtonDown(0))
                        {
                            SceneManager.LoadScene("TitleScene");
                        }
                    }
                    break;

                case STEP.GAMEOVER:
                    // 게임오버 상태에서 클릭하면 타이틀로 이동
                    if (Input.GetMouseButtonDown(0))
                    {
                        SceneManager.LoadScene("TitleScene");
                    }
                    break;

                case STEP.PLAY:
                    // 1. 클리어 조건을 먼저 검사
                    // 마지막 이동으로 목표 점수에 도달했을 경우 GAMEOVER보다 CLEAR가 우선
                    if (this.score_counter.isGameClear())
                    {
                        this.next_step = STEP.CLEAR;
                    }
                    // 2. 클리어하지 못한 경우에만 실패 조건 검사
                    else if (StageManager.Instance != null)
                    {
                        if (StageManager.Instance.IsTimeOver(this.step_timer) ||
                            StageManager.Instance.IsMoveOver())
                        {
                            this.next_step = STEP.GAMEOVER;
                        }
                    }
                    break;
            }
        }

        while (this.next_step != STEP.NONE)
        { // 상태가 변화했다면
            this.step = this.next_step;
            this.next_step = STEP.NONE;

            switch (this.step)
            {
                case STEP.PLAY:
                    break;

                case STEP.CLEAR:
                    this.block_root.enabled = false; // block_root를 정지
                    this.clear_time = this.step_timer; // 경과 시간을 클리어 시간으로 설정

                    // 마지막 스테이지가 아니면 클리어 후 상점 열기
                    if (this.shop_root != null)
                    {
                        if (StageManager.Instance == null || !StageManager.Instance.IsFinalStage())
                        {
                            this.shop_root.OpenShop();
                        }
                    }

                    Debug.Log("스테이지 클리어");
                    break;

                case STEP.GAMEOVER:
                    this.block_root.enabled = false; // 게임 진행 정지
                    Debug.Log("게임 오버");
                    break;
            }

            this.step_timer = 0.0f;
        }
    }

    // 화면에 클리어 / 게임오버 메시지를 표시
    void OnGUI()
    {
        if (this.guistyle == null)
        {
            this.guistyle = new GUIStyle();
            this.guistyle.fontSize = 24;
        }

        switch (this.step)
        {
            case STEP.CLEAR:
                // 마지막 스테이지 클리어 때만 최종 클리어 메시지 표시
                if (StageManager.Instance != null && StageManager.Instance.IsFinalStage())
                {
                    GUI.color = Color.black;

                    GUI.Label(
                        new Rect(Screen.width / 2.0f - 160.0f, 40.0f, 320.0f, 40.0f),
                        "게임 클리어!",
                        guistyle
                    );

                    GUI.Label(
                        new Rect(Screen.width / 2.0f - 180.0f, 80.0f, 360.0f, 40.0f),
                        "화면을 클릭하면 타이틀로 돌아갑니다.",
                        guistyle
                    );

                    GUI.color = Color.white;
                }
                break;

            case STEP.GAMEOVER:
                GUI.color = Color.black;

                GUI.Label(
                    new Rect(Screen.width / 2.0f - 120.0f, 40.0f, 240.0f, 40.0f),
                    "게임 오버",
                    guistyle
                );

                GUI.Label(
                    new Rect(Screen.width / 2.0f - 180.0f, 80.0f, 360.0f, 40.0f),
                    "화면을 클릭하면 타이틀로 돌아갑니다.",
                    guistyle
                );

                GUI.color = Color.white;
                break;
        }
    }

    // 스테이지 전환 시 게임 플로우 리셋
    public void ResetForNextStage()
    {
        this.step_timer = 0.0f; // 경과 시간 리셋
        this.clear_time = 0.0f;

        this.block_root.enabled = true; // Clear 상태에서 꺼버렸던 BlockRoot 컴포넌트를 다시 켬

        this.step = STEP.PLAY; // 상태를 다시 플레이 중으로 강제 변경
        this.next_step = STEP.NONE;
    }

    public void PlusTime(float time)
    {
        this.step_timer += time;
    }
}