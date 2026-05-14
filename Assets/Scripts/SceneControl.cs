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
        NONE = -1, PLAY = 0, CLEAR, NUM,
    }; // 상태 정보 없음, 플레이 중, 클리어, 상태의 종류(= 2)

    public STEP step = STEP.NONE; // 현재 상태
    public STEP next_step = STEP.NONE; // 다음 상태
    public float step_timer = 0.0f; // 경과 시간
    private float clear_time = 0.0f; // 클리어 시간
    public GUIStyle guistyle; // 폰트 스타일

    // 씬이 시작될 때 텍스트 데이터를 읽고 레벨 선택을 할 수 있게
    void Start()
    {
        // BlockRoot 스크립트를 가져옴
        this.block_root = this.gameObject.GetComponent<BlockRoot>();
        this.block_root.create(); // create() 메서드에서 초기 설정

        // 1. 상점 루트를 먼저 가져옵니다.
        this.shop_root = this.gameObject.GetComponent<ShopRoot>();

        // 2. 보드를 생성하기 '전'에 상점의 이월된 효과(require_blocks 변경 등)를 미리 적용합니다.
        if (this.shop_root != null)
        {
            this.shop_root.ApplyPendingEffects();
        }

        // 3. 이제 변경된 상태를 기반으로 BlockRoot 스크립트의 initialSetUp()을 호출
        this.block_root.initialSetUp();

        this.score_counter = this.gameObject.GetComponent<ScoreCounter>(); // ScoreCounter 가져오기

        this.next_step = STEP.PLAY; // 다음 상태를 '플레이 중'으로
        this.guistyle.fontSize = 24; // 폰트 크기를 24로
    }

    void Update()
    {
        this.step_timer += Time.deltaTime;

        if (this.next_step == STEP.NONE)
        { // 상태 변화 대기 -----.
            switch (this.step)
            {
                case STEP.CLEAR:
                    // ShopRoot가 없을 때만 기존처럼 타이틀로 돌아가게 함
                    // ShopRoot가 있으면 상점이 버튼 처리를 담당함
                    if (this.shop_root == null)
                    {
                        if (Input.GetMouseButtonDown(0))
                        {
                            SceneManager.LoadScene("TitleScene");
                        }
                    }
                    break;

                case STEP.PLAY:
                    if (this.score_counter.isGameClear())
                    {
                        this.next_step = STEP.CLEAR;
                    } // 클리어 조건을 만족하면, 클리어 상태로 이행
                    break;
            }
        }

        while (this.next_step != STEP.NONE)
        { // 상태가 변화했다면 ------
            this.step = this.next_step;
            this.next_step = STEP.NONE;

            switch (this.step)
            {
                case STEP.CLEAR:
                    this.block_root.enabled = false; // block_root를 정지
                    this.clear_time = this.step_timer; // 경과 시간을 클리어 시간으로 설정

                    // 클리어 후 상점 열기
                    if (this.shop_root != null)
                    {
                        this.shop_root.OpenShop();
                    }
                    break;
            }

            this.step_timer = 0.0f;
        }
    }


    // 화면에 클리어한 시간과 메시지를 표시
    void OnGUI()
    {
        /*
        switch (this.step)
        {
            case STEP.PLAY:
                GUI.color = Color.black;

                // 경과 시간을 표시
                GUI.Label(
                    new Rect(40.0f, 10.0f, 200.0f, 20.0f),
                    "시간" + Mathf.CeilToInt(this.step_timer).ToString() + "초",
                    guistyle
                );

                GUI.color = Color.white;
                break;

            case STEP.CLEAR:
                GUI.color = Color.black;

                // 「☆클리어-！☆」라는 문자열을 표시
                GUI.Label(
                    new Rect(Screen.width / 2.0f - 80.0f, 20.0f, 200.0f, 20.0f),
                    "☆클리어-!☆",
                    guistyle
                );

                // 클리어 시간을 표시
                GUI.Label(
                    new Rect(Screen.width / 2.0f - 80.0f, 40.0f, 200.0f, 20.0f),
                    "클리어 시간" + Mathf.CeilToInt(this.clear_time).ToString() + "초",
                    guistyle
                );

                GUI.color = Color.white;
                break;
        }
        */
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

}