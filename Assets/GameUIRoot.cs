using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUIRoot : MonoBehaviour
{
    private ScoreCounter score_counter = null;
    private SceneControl scene_control = null;
    private ShopRoot shop_root = null;
    private ItemRoot item_root = null;

    private GUIStyle panel_style;
    private GUIStyle title_style;
    private GUIStyle text_style;
    private GUIStyle button_style;

    private bool gui_style_initialized = false;

    // 기준 해상도
    private const float BASE_WIDTH = 1920.0f;
    private const float BASE_HEIGHT = 1080.0f;

    // 좌우 공통 UI 기준값
    private const float SIDE_TOP = 25.0f;
    private const float SIDE_BOTTOM = 1020.0f;
    private const float SIDE_MARGIN = 30.0f;
    private const float SIDE_WIDTH = 410.0f;
    private const float PANEL_GAP = 30.0f;

    // 좌측 패널 크기
    private const float JOKER_PANEL_HEIGHT = 250.0f;

    // 우측 패널 크기
    private const float DEBUFF_PANEL_HEIGHT = 250.0f;
    private const float ITEM_PANEL_HEIGHT = 230.0f;

    void Start()
    {
        this.score_counter = this.gameObject.GetComponent<ScoreCounter>();
        this.scene_control = this.gameObject.GetComponent<SceneControl>();
        this.shop_root = this.gameObject.GetComponent<ShopRoot>();
        this.item_root = this.gameObject.GetComponent<ItemRoot>();
    }

    void OnGUI()
    {
        this.createGUIStyle();

        // 상점이 열려 있을 때는 인게임 UI 숨김
        if (this.shop_root != null && this.shop_root.IsOpen())
        {
            return;
        }

        this.drawLeftTopJokerPanel();
        this.drawLeftInfoPanel();

        this.drawRightTopDebuffPanel();
        this.drawRightItemPanel();
        this.drawRightBossPanel();
    }

    private void createGUIStyle()
    {
        if (this.gui_style_initialized)
        {
            return;
        }

        this.panel_style = new GUIStyle(GUI.skin.box);

        this.title_style = new GUIStyle();
        this.title_style.fontSize = 34;
        this.title_style.normal.textColor = Color.white;
        this.title_style.alignment = TextAnchor.MiddleCenter;
        this.title_style.wordWrap = true;

        this.text_style = new GUIStyle();
        this.text_style.fontSize = 28;
        this.text_style.normal.textColor = Color.black;
        this.text_style.alignment = TextAnchor.UpperLeft;
        this.text_style.wordWrap = true;

        this.button_style = new GUIStyle(GUI.skin.button);
        this.button_style.fontSize = 28;
        this.button_style.alignment = TextAnchor.MiddleCenter;

        this.gui_style_initialized = true;
    }

    private Rect scaleRect(float x, float y, float w, float h)
    {
        float sx = Screen.width / BASE_WIDTH;
        float sy = Screen.height / BASE_HEIGHT;

        return new Rect(x * sx, y * sy, w * sx, h * sy);
    }

    private void drawPanel(Rect rect)
    {
        GUI.color = new Color(0.65f, 0.65f, 0.65f, 0.78f);
        GUI.Box(rect, "", this.panel_style);
        GUI.color = Color.white;
    }

    private float getLeftX()
    {
        return SIDE_MARGIN;
    }

    private float getRightX()
    {
        return BASE_WIDTH - SIDE_MARGIN - SIDE_WIDTH;
    }

    private void drawLeftTopJokerPanel()
    {
        float x = this.getLeftX();
        float y = SIDE_TOP;
        float w = SIDE_WIDTH;
        float h = JOKER_PANEL_HEIGHT;

        Rect rect = this.scaleRect(x, y, w, h);
        this.drawPanel(rect);

        GUI.Label(
            this.scaleRect(x, y + 70.0f, w, 60.0f),
            "조커",
            this.title_style
        );

        string joker_name = "없음";

        // ShopRoot에 GetCurrentJokerName()을 추가한 경우 사용
        joker_name = ShopRoot.GetCurrentJokerName();

        GUI.Label(
            this.scaleRect(x + 35.0f, y + 145.0f, w - 70.0f, 80.0f),
            joker_name,
            this.text_style
        );
    }

    private void drawLeftInfoPanel()
    {
        float x = this.getLeftX();

        float y = SIDE_TOP + JOKER_PANEL_HEIGHT + PANEL_GAP;
        float w = SIDE_WIDTH;

        // 좌측 아래 패널은 SIDE_BOTTOM에 정확히 맞춰 끝나게 계산
        float h = SIDE_BOTTOM - y;

        Rect rect = this.scaleRect(x, y, w, h);
        this.drawPanel(rect);

        int total_score = 0;
        int add_score = 0;
        int ignite_count = 0;

        if (this.score_counter != null)
        {
            total_score = this.score_counter.last.total_socre;
            add_score = this.score_counter.last.score;
            ignite_count = this.score_counter.last.ignite;
        }

        int time = 0;

        if (this.scene_control != null)
        {
            time = Mathf.CeilToInt(this.scene_control.step_timer);
        }

        GUI.Label(
            this.scaleRect(x + 45.0f, y + 80.0f, w - 90.0f, 260.0f),
            "현재 점수 : " + total_score.ToString() + "\n" +
            "가산 점수 : " + add_score.ToString() + "\n" +
            "연쇄 수 : " + ignite_count.ToString() + "\n" +
            "시간 : " + time.ToString() + "초",
            this.text_style
        );

        GUI.Label(
            this.scaleRect(x + 45.0f, y + 380.0f, w - 90.0f, 220.0f),
            "현재 미션 :\n" +
            "점수 " + ScoreCounter.QUOTA_SCORE.ToString() + "점 도달",
            this.text_style
        );
    }

    private void drawRightTopDebuffPanel()
    {
        float x = this.getRightX();
        float y = SIDE_TOP;
        float w = SIDE_WIDTH;
        float h = DEBUFF_PANEL_HEIGHT;

        Rect rect = this.scaleRect(x, y, w, h);
        this.drawPanel(rect);

        GUI.Label(
            this.scaleRect(x, y + 70.0f, w, 60.0f),
            "디버프",
            this.title_style
        );

        string debuff_name = "없음";

        // ShopRoot에 GetCurrentDebuffName()을 추가한 경우 사용
        debuff_name = ShopRoot.GetCurrentDebuffName();

        GUI.Label(
            this.scaleRect(x + 35.0f, y + 145.0f, w - 70.0f, 80.0f),
            debuff_name,
            this.text_style
        );
    }

    private void drawRightItemPanel()
    {
        float x = this.getRightX();

        float y = SIDE_TOP + DEBUFF_PANEL_HEIGHT + PANEL_GAP;
        float w = SIDE_WIDTH;
        float h = ITEM_PANEL_HEIGHT;

        Rect rect = this.scaleRect(x, y, w, h);
        this.drawPanel(rect);

        GUI.Label(
            this.scaleRect(x, y + 30.0f, w, 50.0f),
            "사용 아이템",
            this.title_style
        );

        string item_name = "없음";
        bool has_item = false;

        if (this.item_root != null && this.item_root.HasItem())
        {
            has_item = true;
            item_name = this.item_root.GetCurrentItemName();
        }

        if (has_item)
        {
            if (GUI.Button(
                this.scaleRect(x + 50.0f, y + 105.0f, w - 100.0f, 70.0f),
                item_name,
                this.button_style))
            {
                Debug.Log("[GameUIRoot] 사용 아이템 사용: " + item_name);

                // 아직 실제 효과는 구현 전이므로 아이템 제거만 처리
                this.item_root.ClearItem();
            }
        }
        else
        {
            GUI.Label(
                this.scaleRect(x, y + 115.0f, w, 70.0f),
                "없음",
                this.title_style
            );
        }
    }

    private void drawRightBossPanel()
    {
        float x = this.getRightX();

        float y = SIDE_TOP + DEBUFF_PANEL_HEIGHT + PANEL_GAP + ITEM_PANEL_HEIGHT + PANEL_GAP;
        float w = SIDE_WIDTH;

        // 우측 마지막 패널도 SIDE_BOTTOM에 정확히 맞춰 끝나게 계산
        float h = SIDE_BOTTOM - y;

        Rect rect = this.scaleRect(x, y, w, h);
        this.drawPanel(rect);

        GUI.Label(
            this.scaleRect(x, y + h / 2.0f - 40.0f, w, 60.0f),
            "보스능력",
            this.title_style
        );

        GUI.Label(
            this.scaleRect(x + 50.0f, y + h / 2.0f + 40.0f, w - 100.0f, 80.0f),
            "아직 없음",
            this.text_style
        );
    }
}