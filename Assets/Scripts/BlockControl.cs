using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block
{ // 블록에 관한 정보
    public static float COLLISION_SIZE = 1.0f; // 블록의 충돌 크기
    public static float VANISH_TIME = 3.0f; // 불 붙고 사라질 때까지의 시간
    public struct iPosition
    { // 그리드에서의 좌표를 나타내는 구조체
        public int x; // X 좌표
        public int y; // Y 좌표
    }
    public enum COLOR
    { // 블록 색상
        NONE = -1, // 색 지정 없음
        PINK = 0, BLUE, YELLOW, GREEN, // 분홍색, 파란색, 노란색, 녹색
        MAGENTA, ORANGE, GRAY, // 마젠타, 주황색, 그레이
        NUM, // 컬러가 몇 종류인지 나타냄(=7)
        FIRST = PINK, LAST = ORANGE,// 초기 컬러(분홍색), 최종 컬러(주황색)
        NORMAL_COLOR_NUM = GRAY, // 보통 컬러(회색 이외의 색)의 수
    };
    public enum DIR4
    { // 상하좌우 네 방향
        NONE = -1, // 방향지정 없음
        RIGHT, LEFT, UP, DOWN, // 우. 좌, 상, 하
        NUM, // 방향이 몇 종류 있는지 나타냄(=4)
    };
    public static int BLOCK_NUM_X = 9; // 블록을 배치할 수 있는 X방향 최대수
    public static int BLOCK_NUM_Y = 9; // 블록을 배치할 수 있는 Y방향 최대수

    public enum STEP
    {
        // 상태 정보 없음, 대기 중, 잡혀 있음, 떨어진 순간, 슬라이드 중, 소멸 중, 재생성 중, 낙하 중, 크게 슬라이드 중, 상태 종류
        NONE = -1, IDLE = 0, GRABBED, RELEASED, SLIDE, VACANT, RESPAWN, FALL, LONG_SLIDE, NUM,
    };
}

public class BlockControl : MonoBehaviour
{
    public Block.COLOR color = (Block.COLOR)0; // 블록 색
    public BlockRoot block_root = null; // 블록의 신
    public Block.iPosition i_pos; // 블록 좌표

    public Block.STEP step = Block.STEP.NONE; // 지금 상태
    public Block.STEP next_step = Block.STEP.NONE; // 다음 상태
    private Vector3 position_offset_initial = Vector3.zero; // 교체 전 위치
    public Vector3 position_offset = Vector3.zero; // 교체 후 위치

    public float vanish_timer = -1.0f; // 블록이 사라질 때까지의 시간
    public Block.DIR4 slide_dir = Block.DIR4.NONE; // 슬라이드된 방향
    public float step_timer = 0.0f; // 블록이 교체된 때의 이동시간 등

    public Material opague_material; // 불투명 머티리얼
    public Material transparent_material; // 반투명 머티리얼

    private struct StepFall
    {
        public float velocity; // 낙하 속도.
    }
    private StepFall fall;

    // Start is called before the first frame update
    void Start()
    {
        this.setColor(this.color);
        this.next_step = Block.STEP.IDLE; // 다음 블록을 대기중으로
    }

    // Update is called once per frame
    void Update()
    { // 블록이 잡혔을 때에 블록을 크게 하고 그렇지 않을 때는 원래대로 돌아감
        Vector3 mouse_position; // 마우스 위치
        this.block_root.unprojectMousePosition(out mouse_position, Input.mousePosition); // 마우스 위치 획득
        Vector2 mouse_position_xy = new Vector2(mouse_position.x, mouse_position.y); // 획득한 마우스 위치를 X와 Y만으로 함

        if (this.vanish_timer >= 0.0f)
        { // 타이머가 0 이상이면
            this.vanish_timer -= Time.deltaTime; // 타이머의 값을 줄임
            if (this.vanish_timer < 0.0f)
            { // 타이머가 0 미만이면
                if (this.step != Block.STEP.SLIDE)
                { // 슬라이드 중이 아니라면
                    this.vanish_timer = -1.0f;
                    this.next_step = Block.STEP.VACANT; // 상태를 ‘소멸 중’으로
                }
                else
                {
                    this.vanish_timer = 0.0f;
                }
            }
        }

        this.step_timer += Time.deltaTime;
        float slide_time = 0.2f;
        if (this.next_step == Block.STEP.NONE)
        { // '상태 정보 없음'의 경우
            switch (this.step)
            {
                case Block.STEP.SLIDE:
                    if (this.step_timer >= slide_time)
                    { // 슬라이드 중인 블록이 소멸되면 VACANT(사라진) 상태로 이행
                        if (this.vanish_timer == 0.0f)
                        {
                            this.next_step = Block.STEP.VACANT;
                        }
                        else
                        {
                            this.next_step = Block.STEP.IDLE; // vanish_timer가 0이 아니면 IDLE(대기) 상태로 이행
                        }
                    }
                    break;
                case Block.STEP.IDLE:
                    this.GetComponent<Renderer>().enabled = true;
                    break;
                case Block.STEP.FALL:
                    if (this.position_offset.y <= 0.0f)
                    {
                        this.next_step = Block.STEP.IDLE;
                        this.position_offset.y = 0.0f;
                    }
                    break;
            }
        }

        while (this.next_step != Block.STEP.NONE)
        { // '다음 블록' 상태가 '정보 없음' 이외인 동안 -> '다음 블록' 상태가 변경된 경우
            this.step = this.next_step;
            this.next_step = Block.STEP.NONE;
            switch (this.step)
            {
                case Block.STEP.IDLE: // '대기' 상태
                    this.position_offset = Vector3.zero;
                    this.transform.localScale = Vector3.one * 1.0f; break; // 블록 표시 크기를 보통 크기로 함
                case Block.STEP.GRABBED: // ＇잡힌＇ 상태
                    this.transform.localScale = Vector3.one * 1.2f; break; // 블록 표시 크기를 크게 한다
                case Block.STEP.RELEASED: // '떨어져 있는' 상태
                    this.position_offset = Vector3.zero;
                    this.transform.localScale = Vector3.one * 1.0f; break; // 블록 표시 크기를 보통 사이즈로 함
                case Block.STEP.VACANT:
                    this.position_offset = Vector3.zero;
                    this.setVisible(false); // 블록을 표시하지 않게
                    break;
                case Block.STEP.RESPAWN:
                    // 색을 랜덤하게 선택하여 블록을 그 색으로 설정.
                    int color_index = Random.Range(0, (int)Block.COLOR.NORMAL_COLOR_NUM);
                    this.setColor((Block.COLOR)color_index);
                    this.next_step = Block.STEP.IDLE;
                    break;
                case Block.STEP.FALL:
                    this.setVisible(true); // 블록을 표시.
                    this.fall.velocity = 0.0f; // 낙하 속도 리셋.
                    break;
            }
            this.step_timer = 0.0f;
        } // 그리드 좌표를 실제 좌표(씬의 좌표)로 변환하고

        switch (this.step)
        {
            case Block.STEP.GRABBED: // 잡힌 상태
                this.slide_dir = this.calcSlideDir(mouse_position_xy); break; // 잡힌 상태일 때는 항상 슬라이드 방향을 체크
            case Block.STEP.SLIDE: // 슬라이드(교체) 중
                float rate = this.step_timer / slide_time; // 블록을 서서히 이동하는 처리
                rate = Mathf.Min(rate, 1.0f);
                rate = Mathf.Sin(rate * Mathf.PI / 2.0f);
                this.position_offset = Vector3.Lerp(this.position_offset_initial, Vector3.zero, rate); break;
            case Block.STEP.FALL:
                this.fall.velocity += Physics.gravity.y * Time.deltaTime * 0.3f; // 중력의 영향을 부여
                this.position_offset.y += this.fall.velocity * Time.deltaTime; // 세로 방향 위치를 계산
                if (this.position_offset.y < 0.0f)
                { // 다 내려왔다면
                    this.position_offset.y = 0.0f; // 그 자리에 머무른다
                }
                break;
        }

        Vector3 position = BlockRoot.calcBlockPosition(this.i_pos) + this.position_offset; // position_offset을 추가
        this.transform.position = position; // 실제 위치를 새로운 위치로 변경

        this.setColor(this.color);
        if (this.vanish_timer >= 0.0f)
        {
            // 레벨 데이터의 연소 시간이 반영되도록
            // 현재 레벨의 연소시간으로 설정
            float vanish_time = this.block_root.level_control.getVanishTime();

            Color color0 = Color.Lerp(this.GetComponent<Renderer>().material.color, Color.white, 0.5f); // 현재 색과 흰색의 중간 색
            Color color1 = Color.Lerp(this.GetComponent<Renderer>().material.color, Color.black, 0.5f); // 현재 색과 검은색의 중간 색
            if (this.vanish_timer < Block.VANISH_TIME / 2.0f)
            { // 불붙는 연출 시간이 절반을 지났다면
                color0.a = this.vanish_timer / (Block.VANISH_TIME / 2.0f); // 투명도(a)를 설정
                color1.a = color0.a;
                this.GetComponent<Renderer>().material = this.transparent_material;
            } // 반투명 머티리얼을 적용
            float rate = 1.0f - this.vanish_timer / Block.VANISH_TIME; // vanish_timer가 줄어들수록 1에 가까워짐
            this.GetComponent<Renderer>().material.color = Color.Lerp(color0, color1, rate); // 서서히 색을 바꿈
        }
    }


    public void setColor(Block.COLOR color)
    { // 특정 color로 블록을 칠함
        this.color = color; // 이번에 지정된 색을 멤버 변수에 보관
        Color color_value; // Color 클래스는 색을 나타냄
        switch (this.color)
        { // 칠할 색에 따라서 갈라짐
            default:
            case Block.COLOR.PINK:
                color_value = new Color(1.0f, 0.5f, 0.5f); break;
            case Block.COLOR.BLUE:
                color_value = Color.blue; break;
            case Block.COLOR.YELLOW:
                color_value = Color.yellow; break;
            case Block.COLOR.GREEN:
                color_value = Color.green; break;
            case Block.COLOR.MAGENTA:
                color_value = Color.magenta; break;
            case Block.COLOR.ORANGE:
                color_value = new Color(1.0f, 0.46f, 0.0f); break;
        }
        this.GetComponent<Renderer>().material.color = color_value; // 색상변경
    }

    public void beginGrab()
    { // 잡혔을 때 호출
        this.next_step = Block.STEP.GRABBED;
    }
    public void endGrab()
    { // 놓았을 때 호출
        this.next_step = Block.STEP.IDLE;
    }
    public bool isGrabbable()
    { // 잡을 수 있는 상태 인지 판단
        bool is_grabbable = false;
        switch (this.step)
        {
            case Block.STEP.IDLE: // '대기' 상태일 때만.
                is_grabbable = true; // true(잡을 수 있다)를 반환
                break;
        }
        return (is_grabbable);
    }
    public bool isContainedPosition(Vector2 position)
    { // 지정된 마우스 좌표가 자신과 겹치는지 반환
        bool ret = false;
        Vector3 center = this.transform.position;
        float h = Block.COLLISION_SIZE / 2.0f;
        do
        {
            if (position.x < center.x - h || center.x + h < position.x) { break; } // X 좌표가 자신과 겹치지 않으면 루프를 빠져 나감
            if (position.y < center.y - h || center.y + h < position.y) { break; } // Y 좌표가 자신과 겹치지 않으면 루프를 빠져 나감
            ret = true; // X 좌표, Y 좌표 모두 겹쳐 있으면 true(겹쳐 있음)를 반환
        } while (false);
        return (ret);
    }

    public Block.DIR4 calcSlideDir(Vector2 mouse_position) { // 마우스 위치를 바탕으로 슬라이드된 방향을 구함
        Block.DIR4 dir = Block.DIR4.NONE;
        Vector2 v = mouse_position - new Vector2(this.transform.position.x, this.transform.position.y); // 지정된 mouse_positio과 현재 위치의 차이
        if (v.magnitude > 0.1f)
        { // 벡터의 크기가 0.1보다 작으면 슬라이드 하지 않은 걸로 간주
            if (v.y > v.x)
            {
                if (v.y > -v.x)
                {
                    dir = Block.DIR4.UP;
                }
                else { dir = Block.DIR4.LEFT; }
            }
            else
            {
                if (v.y > -v.x)
                {
                    dir = Block.DIR4.RIGHT;
                }
                else { dir = Block.DIR4.DOWN; }
            }
        }
        return (dir);
    }
    public float calcDirOffset(Vector2 position, Block.DIR4 dir)
    { // 현재 위치와 슬라이드할 곳의 거리가 어느 정도인가 반환
        float offset = 0.0f;
        Vector2 v = position - new Vector2(this.transform.position.x, this.transform.position.y); // 지정된 위치와 블록의 현재 위치의 차를 나타내는 벡터
        switch (dir)
        { // 지정된 방향에 따라 갈라짐
            case Block.DIR4.RIGHT: offset = v.x; break;
            case Block.DIR4.LEFT: offset = -v.x; break;
            case Block.DIR4.UP: offset = v.y; break;
            case Block.DIR4.DOWN: offset = -v.y; break;
        }
        return (offset);
    }
    public void beginSlide(Vector3 offset)
    { // 이동 시작을 알리는 메서드
        this.position_offset_initial = offset;
        this.position_offset = this.position_offset_initial;
        this.next_step = Block.STEP.SLIDE;
    } // 상태를 SLIDE로 변경

    public void toVanishing()
    {// 연소 시간 값을 상수값에서 현시점의 레벨에서 정해진 값으로 수정
     // ＇사라질 때까지 걸리는 시간＇을 규정값으로 리셋
     //this.vanish_timer = Block.VANISH_TIME;
     // 현재 레벨의 연소시간으로 설정.
        float vanish_time = this.block_root.level_control.getVanishTime();
        this.vanish_timer = vanish_time;
    }
    public bool isVanishing()
    {
        // vanish_timer가 0보다 크면 true
        bool is_vanishing = (this.vanish_timer > 0.0f);
        return (is_vanishing);
    }
    public void rewindVanishTimer()
    {
        // '사라질 때까지 걸리는 시간'을 규정값으로 리셋
        //this.vanish_timer = Block.VANISH_TIME;
        // 현재 레벨의 연소시간으로 설정.
        float vanish_time = this.block_root.level_control.getVanishTime();
        this.vanish_timer = vanish_time;
    }
    public bool isVisible()
    {
        // 그리기 가능(renderer.enabled가 true) 상태라면 표시
        bool is_visible = this.GetComponent<Renderer>().enabled;
        return (is_visible);
    }
    public void setVisible(bool is_visible)
    {
        // 그리기 가능 설정에 인수를 대입
        this.GetComponent<Renderer>().enabled = is_visible;
    }
    public bool isIdle()
    {
        bool is_idle = false;
        // 현재 블록 상태가 '대기 중'이고, 다음 블록 상태가 '없음'이면
        if (this.step == Block.STEP.IDLE && this.next_step == Block.STEP.NONE)
        {
            is_idle = true;
        }
        return (is_idle);
    }

    public void beginFall(BlockControl start)
    { // 낙하 시작 처리
        this.next_step = Block.STEP.FALL;
        this.position_offset.y = (float)(start.i_pos.y - this.i_pos.y)
        * Block.COLLISION_SIZE; // 지정된 블록에서 좌표를 계산
    }
    public void beginRespawn(int start_ipos_y)
    { // 색이 바꿔 낙하 상태로 하고 지정한 위치에 재배치
        this.position_offset.y = (float)(start_ipos_y - this.i_pos.y)
        * Block.COLLISION_SIZE; // 지정 위치까지 y좌표를 이동
        this.next_step = Block.STEP.FALL;
        //int color_index = Random.Range((int)Block.COLOR.FIRST, (int)Block.COLOR.LAST + 1);
        //this.setColor((Block.COLOR)color_index);

        // 블록의 출현 확률을 반영한다.
        // 현재 레벨의 출현 확률을 바탕으로 블록의 색을 결정
        Block.COLOR color = this.block_root.selectBlockColor();
        this.setColor(color);
    }
    public bool isVacant()
    { // 블록이 비표시(그리드상의 위치가 텅 빔)로 되어 있다면 true를 반환
        bool is_vacant = false;
        if(this.step == Block.STEP.VACANT && this.next_step == Block.STEP.NONE) {
            is_vacant = true;
        }
        return (is_vacant);
    }
    public bool isSliding()
    { // 교체 중(슬라이드 중)이라면 true를 반환
        bool is_sliding = (this.position_offset.x != 0.0f);
        return (is_sliding);
    }
}
