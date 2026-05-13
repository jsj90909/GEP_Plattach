using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ShopRoot : MonoBehaviour
{
    public enum STEP
    {
        NONE = -1,
        CLOSED = 0,
        DEBUFF_SELECT,
        JOKER_SELECT,
        ITEM_SELECT,
        DONE,
    }

    public STEP step = STEP.CLOSED;

    private ScoreCounter score_counter = null;
    private BlockRoot block_root = null;

    // 씬을 다시 로드해도 골드가 유지되도록 static 사용
    public static int player_gold = 0;

    // 다음 스테이지에 적용할 선택 결과 저장
    private static string pending_debuff_name = "";
    private static string pending_joker_name = "";
    private static string pending_item_name = "";

    private string selected_debuff_name = "";
    private string selected_joker_name = "";
    private string selected_item_name = "";

    private string message = "";

    public GUIStyle title_style;
    public GUIStyle text_style;
    public GUIStyle button_style;

    private bool gui_style_initialized = false;

    // UI 배치값
    private const float CARD_WIDTH = 980.0f;
    private const float CARD_HEIGHT = 165.0f;
    private const float CARD_START_Y = 170.0f;
    private const float CARD_INTERVAL = 205.0f;

    private class DebuffData
    {
        public string name;
        public string description;
        public int reward_gold;

        public DebuffData(string name, string description, int reward_gold)
        {
            this.name = name;
            this.description = description;
            this.reward_gold = reward_gold;
        }
    }

    private class JokerData
    {
        public string name;
        public string description;
        public int price;

        public JokerData(string name, string description, int price)
        {
            this.name = name;
            this.description = description;
            this.price = price;
        }
    }

    private class ItemData
    {
        public string name;
        public string description;

        public ItemData(string name, string description)
        {
            this.name = name;
            this.description = description;
        }
    }

    private List<DebuffData> debuff_list = new List<DebuffData>();
    private List<JokerData> joker_list = new List<JokerData>();
    private List<ItemData> item_list = new List<ItemData>();

    void Start()
    {
        this.score_counter = this.gameObject.GetComponent<ScoreCounter>();
        this.block_root = this.gameObject.GetComponent<BlockRoot>();

        this.createShopData();

        // GameScene을 다시 로드한 뒤, 이전 상점 선택 효과를 다음 프레임에 적용
        StartCoroutine(this.applyPendingEffectsNextFrame());
    }

    private IEnumerator applyPendingEffectsNextFrame()
    {
        yield return null;

        if (pending_debuff_name != "")
        {
            this.applyDebuffByName(pending_debuff_name);
        }

        if (pending_joker_name != "")
        {
            this.applyJokerByName(pending_joker_name);
        }

        if (pending_item_name != "")
        {
            this.applyItemByName(pending_item_name);
        }
    }

    private void createGUIStyle()
    {
        if (this.gui_style_initialized)
        {
            return;
        }

        this.title_style = new GUIStyle();
        this.title_style.fontSize = 52;
        this.title_style.normal.textColor = Color.white;
        this.title_style.alignment = TextAnchor.MiddleCenter;

        this.text_style = new GUIStyle();
        this.text_style.fontSize = 26;
        this.text_style.normal.textColor = Color.white;
        this.text_style.wordWrap = true;

        // GUI.skin.button은 반드시 OnGUI 안에서만 접근해야 함
        this.button_style = new GUIStyle(GUI.skin.button);
        this.button_style.fontSize = 24;
        this.button_style.alignment = TextAnchor.MiddleCenter;

        this.gui_style_initialized = true;
    }

    private void createShopData()
    {
        // 디버프 선택지
        debuff_list.Add(new DebuffData(
            "불타는 시간 감소",
            "다음 스테이지에서 블록이 더 빨리 사라집니다.",
            100
        ));

        debuff_list.Add(new DebuffData(
            "특정 구역 점수 무효화",
            "다음 스테이지에서 일부 구역의 블록 점수가 무효화됩니다.",
            150
        ));

        debuff_list.Add(new DebuffData(
            "4개 매치 필요",
            "다음 스테이지에서 3개가 아니라 4개 이상 연결해야 점수가 납니다.",
            200
        ));

        // 조커 선택지
        joker_list.Add(new JokerData(
            "파란색 블록 점수 증가",
            "파란색 블록 점수를 100점으로 변경합니다.",
            100
        ));

        joker_list.Add(new JokerData(
            "마젠타 블록 점수 증가",
            "마젠타 블록 점수를 100점으로 변경합니다.",
            120
        ));

        joker_list.Add(new JokerData(
            "매치 요구 수 감소",
            "아직 실제 효과는 구현하지 않은 조커입니다.",
            150
        ));

        // 사용 아이템 선택지
        item_list.Add(new ItemData(
            "셔플",
            "나중에 블록을 섞는 아이템으로 구현할 예정입니다."
        ));

        item_list.Add(new ItemData(
            "시간 정지",
            "나중에 일정 시간 타이머를 멈추는 아이템으로 구현할 예정입니다."
        ));

        item_list.Add(new ItemData(
            "폭탄",
            "나중에 특정 위치 주변 블록을 제거하는 아이템으로 구현할 예정입니다."
        ));
    }

    public void OpenShop()
    {
        this.step = STEP.DEBUFF_SELECT;

        this.selected_debuff_name = "";
        this.selected_joker_name = "";
        this.selected_item_name = "";

        this.message = "디버프를 선택하면 골드를 받습니다.";
    }

    public bool IsOpen()
    {
        return this.step != STEP.CLOSED;
    }

    void OnGUI()
    {
        this.createGUIStyle();

        if (this.step == STEP.CLOSED)
        {
            return;
        }

        this.drawBackPanel();

        switch (this.step)
        {
            case STEP.DEBUFF_SELECT:
                this.drawDebuffSelect();
                break;

            case STEP.JOKER_SELECT:
                this.drawJokerSelect();
                break;

            case STEP.ITEM_SELECT:
                this.drawItemSelect();
                break;

            case STEP.DONE:
                this.drawDone();
                break;
        }
    }

    private void drawBackPanel()
    {
        GUI.color = new Color(0.0f, 0.0f, 0.0f, 0.80f);
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
        GUI.color = Color.white;

        GUI.Label(
            new Rect(Screen.width / 2 - 250, 25, 500, 60),
            "SHOP",
            this.title_style
        );

        GUI.Label(
            new Rect(40, 35, 400, 45),
            "Gold : " + player_gold.ToString(),
            this.text_style
        );

        if (this.message != "")
        {
            GUI.Label(
                new Rect(Screen.width / 2 - 500, Screen.height - 95, 1000, 70),
                this.message,
                this.text_style
            );
        }
    }

    private Rect getCardRect(int index)
    {
        float x = Screen.width / 2.0f - CARD_WIDTH / 2.0f;
        float y = CARD_START_Y + index * CARD_INTERVAL;

        return new Rect(x, y, CARD_WIDTH, CARD_HEIGHT);
    }

    private void drawDebuffSelect()
    {
        GUI.Label(
            new Rect(Screen.width / 2 - 350, 105, 700, 45),
            "1. 디버프 선택",
            this.text_style
        );

        for (int i = 0; i < debuff_list.Count; i++)
        {
            DebuffData data = debuff_list[i];
            Rect box_rect = this.getCardRect(i);

            GUI.Box(box_rect, "");

            GUI.Label(
                new Rect(box_rect.x + 30, box_rect.y + 20, 680, 35),
                data.name,
                this.text_style
            );

            GUI.Label(
                new Rect(box_rect.x + 30, box_rect.y + 65, 680, 85),
                data.description + "\n획득 골드 : +" + data.reward_gold.ToString(),
                this.text_style
            );

            if (GUI.Button(
                new Rect(box_rect.x + 760, box_rect.y + 52, 170, 65),
                "선택",
                this.button_style))
            {
                this.selectDebuff(data);
            }
        }
    }

    private void drawJokerSelect()
    {
        GUI.Label(
            new Rect(Screen.width / 2 - 350, 105, 700, 45),
            "2. 조커 구매",
            this.text_style
        );

        for (int i = 0; i < joker_list.Count; i++)
        {
            JokerData data = joker_list[i];
            Rect box_rect = this.getCardRect(i);

            GUI.Box(box_rect, "");

            GUI.Label(
                new Rect(box_rect.x + 30, box_rect.y + 20, 680, 35),
                data.name,
                this.text_style
            );

            GUI.Label(
                new Rect(box_rect.x + 30, box_rect.y + 65, 680, 85),
                data.description + "\n가격 : -" + data.price.ToString() + " Gold",
                this.text_style
            );

            if (GUI.Button(
                new Rect(box_rect.x + 760, box_rect.y + 52, 170, 65),
                "구매",
                this.button_style))
            {
                this.selectJoker(data);
            }
        }

        if (GUI.Button(
            new Rect(Screen.width / 2 - 170, Screen.height - 175, 340, 65),
            "조커 구매 안 함",
            this.button_style))
        {
            this.selected_joker_name = "구매 안 함";
            pending_joker_name = "";

            this.step = STEP.ITEM_SELECT;
            this.message = "사용 아이템은 무료로 하나 선택합니다.";
        }
    }

    private void drawItemSelect()
    {
        GUI.Label(
            new Rect(Screen.width / 2 - 350, 105, 700, 45),
            "3. 사용 아이템 선택",
            this.text_style
        );

        for (int i = 0; i < item_list.Count; i++)
        {
            ItemData data = item_list[i];
            Rect box_rect = this.getCardRect(i);

            GUI.Box(box_rect, "");

            GUI.Label(
                new Rect(box_rect.x + 30, box_rect.y + 20, 680, 35),
                data.name,
                this.text_style
            );

            GUI.Label(
                new Rect(box_rect.x + 30, box_rect.y + 65, 680, 85),
                data.description + "\n가격 : 무료",
                this.text_style
            );

            if (GUI.Button(
                new Rect(box_rect.x + 760, box_rect.y + 52, 170, 65),
                "선택",
                this.button_style))
            {
                this.selectItem(data);
            }
        }
    }

    private void drawDone()
    {
        Rect box_rect = new Rect(Screen.width / 2 - 500, 160, 1000, 420);
        GUI.Box(box_rect, "");

        GUI.Label(
            new Rect(box_rect.x + 40, box_rect.y + 35, 920, 350),
            "상점 선택 완료\n\n" +
            "선택한 디버프 : " + selected_debuff_name + "\n" +
            "구매한 조커 : " + selected_joker_name + "\n" +
            "선택한 사용 아이템 : " + selected_item_name + "\n\n" +
            "현재 골드 : " + player_gold.ToString(),
            this.text_style
        );

        if (GUI.Button(
            new Rect(Screen.width / 2 - 180, Screen.height - 220, 360, 70),
            "다음 스테이지 시작",
            this.button_style))
        {
            this.step = STEP.CLOSED;

            // 아직 스테이지 시스템이 따로 없으므로 GameScene 재시작
            SceneManager.LoadScene("GameScene");
        }

        if (GUI.Button(
            new Rect(Screen.width / 2 - 180, Screen.height - 135, 360, 70),
            "타이틀로 돌아가기",
            this.button_style))
        {
            this.step = STEP.CLOSED;
            SceneManager.LoadScene("TitleScene");
        }
    }

    private void selectDebuff(DebuffData data)
    {
        this.selected_debuff_name = data.name;
        pending_debuff_name = data.name;

        player_gold += data.reward_gold;

        this.step = STEP.JOKER_SELECT;
        this.message = data.name + " 선택. +" + data.reward_gold.ToString() + " Gold 획득.";
    }

    private void selectJoker(JokerData data)
    {
        if (player_gold < data.price)
        {
            this.message = "골드가 부족합니다.";
            return;
        }

        this.selected_joker_name = data.name;
        pending_joker_name = data.name;

        player_gold -= data.price;

        this.step = STEP.ITEM_SELECT;
        this.message = data.name + " 구매 완료. 사용 아이템을 선택하세요.";
    }

    private void selectItem(ItemData data)
    {
        this.selected_item_name = data.name;
        pending_item_name = data.name;

        // 인게임에서 사용할 아이템으로 등록
        ItemRoot.SetItem(data.name);

        this.step = STEP.DONE;
        this.message = "상점 선택이 완료되었습니다.";
    }

    private void applyDebuffByName(string debuff_name)
    {
        if (this.block_root == null)
        {
            return;
        }

        if (debuff_name == "특정 구역 점수 무효화")
        {
            // 예시: 왼쪽 아래 3칸 점수 무효화
            HashSet<Vector2Int> positions = new HashSet<Vector2Int>();

            positions.Add(new Vector2Int(0, 0));
            positions.Add(new Vector2Int(1, 0));
            positions.Add(new Vector2Int(2, 0));

            this.block_root.SetNegativeBlockPositions(positions);
        }
        else if (debuff_name == "4개 매치 필요")
        {
            this.block_root.SetRequireBlocks(4);
        }
        else if (debuff_name == "불타는 시간 감소")
        {
            // 아직 LevelControl에 연소시간 변경 함수가 없으므로 나중에 연결
        }
    }

    private void applyJokerByName(string joker_name)
    {
        if (this.score_counter == null)
        {
            return;
        }

        if (this.score_counter.block_scores == null)
        {
            return;
        }

        if (joker_name == "파란색 블록 점수 증가")
        {
            this.score_counter.block_scores[(int)Block.COLOR.BLUE] = 100;
        }
        else if (joker_name == "마젠타 블록 점수 증가")
        {
            this.score_counter.block_scores[(int)Block.COLOR.MAGENTA] = 100;
        }
        else if (joker_name == "매치 요구 수 감소")
        {
            // 아직 구현 전
            // require_blocks를 2로 줄이면 초기 보드 정제 기능과 충돌할 수 있으므로 일단 적용하지 않음
            // this.block_root.SetRequireBlocks(2);
        }
    }

    private void applyItemByName(string item_name)
    {
        // 아직 사용 아이템 구현 전
        // 지금은 선택 이름만 static으로 저장해두는 상태

        if (item_name == "셔플")
        {
            // TODO: 셔플 아이템 구현
        }
        else if (item_name == "시간 정지")
        {
            // TODO: 시간 정지 아이템 구현
        }
        else if (item_name == "폭탄")
        {
            // TODO: 폭탄 아이템 구현
        }
    }
}