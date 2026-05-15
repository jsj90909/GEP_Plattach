using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 상점 아이템 식별을 위한 Enum 정의
public enum DebuffType { NONE, HEAT_TIME_DECREASE, SCORE_NULLIFY, REQUIRE_MATCH_4 }
public enum JokerType { NONE, BLUE_SCORE_UP, MAGENTA_SCORE_UP, REQUIRE_MATCH_2 }
public enum ItemType { NONE, REMOVE_PINK, TIME_STOP, PLUS_MOVES }

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
    private ItemRoot item_root = null;

    public int player_gold = 0;

    // Enum ID로 상태 저장
    private List<DebuffType> pending_debuff_ids = new List<DebuffType>();
    private List<JokerType> pending_joker_ids = new List<JokerType>();
    private ItemType pending_item_id = ItemType.NONE;

    private DebuffType selected_debuff_id = DebuffType.NONE;
    private JokerType selected_joker_id = JokerType.NONE;
    private ItemType selected_item_id = ItemType.NONE;

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
        public DebuffType id;
        public string name;
        public string description;
        public int reward_gold;

        public DebuffData(DebuffType id, string name, string description, int reward_gold)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.reward_gold = reward_gold;
        }
    }

    private class JokerData
    {
        public JokerType id;
        public string name;
        public string description;
        public int price;

        public JokerData(JokerType id, string name, string description, int price)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.price = price;
        }
    }

    private class ItemData
    {
        public ItemType id;
        public string name;
        public string description;

        public ItemData(ItemType id, string name, string description)
        {
            this.id = id;
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
        this.item_root = this.gameObject.GetComponent<ItemRoot>();

        this.createShopData();
    }

    // 다음 스테이지 보드 생성 전(SceneControl.Start) 호출될 효과 적용 메서드
    public void ApplyPendingEffects()
    {
        if (this.block_root == null) this.block_root = this.gameObject.GetComponent<BlockRoot>();
        if (this.score_counter == null) this.score_counter = this.gameObject.GetComponent<ScoreCounter>();
        if (this.item_root == null) this.item_root = this.gameObject.GetComponent<ItemRoot>();

        // 1. 누적된 디버프 모두 씌우기
        foreach (DebuffType debuff in this.pending_debuff_ids)
        {
            this.applyDebuffById(debuff);
        }

        // 2. 누적된 조커 모두 씌우기
        foreach (JokerType joker in this.pending_joker_ids)
        {
            this.applyJokerById(joker);
        }

        // 3. 아이템 씌우기 (1개)
        //this.applyItemById(this.pending_item_id);
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

        this.button_style = new GUIStyle(GUI.skin.button);
        this.button_style.fontSize = 24;
        this.button_style.alignment = TextAnchor.MiddleCenter;

        this.gui_style_initialized = true;
    }

    private void createShopData()
    {
        debuff_list.Add(new DebuffData(DebuffType.HEAT_TIME_DECREASE, "불타는 시간 감소", "다음 스테이지에서 블록이 더 빨리 사라집니다.", 100));
        debuff_list.Add(new DebuffData(DebuffType.SCORE_NULLIFY, "특정 구역 점수 무효화", "다음 스테이지에서 일부 구역의 블록 점수가 무효화됩니다.", 150));
        debuff_list.Add(new DebuffData(DebuffType.REQUIRE_MATCH_4, "4개 매치 필요", "다음 스테이지에서 4개 이상 연결해야 점수가 납니다.", 200));

        joker_list.Add(new JokerData(JokerType.BLUE_SCORE_UP, "파란색 블록 점수 증가", "파란색 블록 점수를 100점으로 변경합니다.", 100));
        joker_list.Add(new JokerData(JokerType.MAGENTA_SCORE_UP, "마젠타 블록 점수 증가", "마젠타 블록 점수를 100점으로 변경합니다.", 120));
        joker_list.Add(new JokerData(JokerType.REQUIRE_MATCH_2, "매치 요구 수 감소", "다음 스테이지에서 2개만 연결하면 점수가 납니다.", 150));

        item_list.Add(new ItemData(ItemType.REMOVE_PINK, "살구색 제거", "그리드의 살구색 블록을 제거합니다."));
        item_list.Add(new ItemData(ItemType.TIME_STOP, "시간 정지", "나중에 일정 시간 타이머를 멈추는 아이템으로 구현할 예정입니다."));
        item_list.Add(new ItemData(ItemType.PLUS_MOVES, "이동 횟수 증가", "현재 스테이지의 이동 횟수 제한을 증가시킵니다.."));
    }

    public void OpenShop()
    {
        this.step = STEP.DEBUFF_SELECT;

        this.selected_debuff_id = DebuffType.NONE;
        this.selected_joker_id = JokerType.NONE;
        this.selected_item_id = ItemType.NONE;

        this.message = "디버프를 선택하면 골드를 받습니다.";
    }

    public bool IsOpen()
    {
        return this.step != STEP.CLOSED;
    }

    // UI 표시용
    public string GetCurrentDebuffName()
    {
        if (this.pending_debuff_ids.Count == 0) return "없음";

        List<string> names = new List<string>();
        foreach (var id in this.pending_debuff_ids)
        {
            names.Add(debuff_list.Find(d => d.id == id)?.name ?? id.ToString());
        }
        return string.Join("\n", names);
    }

    public string GetCurrentJokerName()
    {
        if (this.pending_joker_ids.Count == 0) return "없음";

        List<string> names = new List<string>();
        foreach (var id in this.pending_joker_ids)
        {
            names.Add(joker_list.Find(j => j.id == id)?.name ?? id.ToString());
        }
        return string.Join("\n", names);
    }

    public string GetCurrentItemName()
    {
        if (this.pending_item_id == ItemType.NONE) return "없음";
        return item_list.Find(i => i.id == this.pending_item_id)?.name ?? this.pending_item_id.ToString();
    }

    public int GetGold()
    {
        return player_gold;
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
            this.selected_joker_id = JokerType.NONE;

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
            "선택한 디버프 : " + selected_debuff_id.ToString() + "\n" +
            "구매한 조커 : " + selected_joker_id.ToString() + "\n" +
            "선택한 사용 아이템 : " + selected_item_id.ToString() + "\n\n" +
            "현재 골드 : " + player_gold.ToString(),
            this.text_style
        );

        if (GUI.Button(
            new Rect(Screen.width / 2 - 180, Screen.height - 220, 360, 70),
            "다음 스테이지 시작",
            this.button_style))
        {
            StageManager.Instance.NextStage();
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
        this.selected_debuff_id = data.id; // 완료 창 표시용
        this.pending_debuff_ids.Add(data.id); // 실제 효과 리스트에 누적

        this.player_gold += data.reward_gold;

        this.step = STEP.JOKER_SELECT;
        this.message = data.name + " 선택. +" + data.reward_gold.ToString() + " Gold 획득.";
    }

    private void selectJoker(JokerData data)
    {
        if (this.player_gold < data.price)
        {
            this.message = "골드가 부족합니다.";
            return;
        }

        this.selected_joker_id = data.id; // 완료 창 표시용
        this.pending_joker_ids.Add(data.id); // 실제 효과 리스트에 누적

        this.player_gold -= data.price;

        this.step = STEP.ITEM_SELECT;
        this.message = data.name + " 구매 완료. 사용 아이템을 선택하세요.";
    }

    private void selectItem(ItemData data)
    {
        this.selected_item_id = data.id;
        pending_item_id = data.id;

        if (this.item_root == null)
        {
            this.item_root = this.gameObject.GetComponent<ItemRoot>();
        }

        if (this.item_root != null)
        {
            // ItemRoot는 아직 Name 기반을 쓰고 있으므로 그대로 유지
            this.item_root.SetItem(data.name);
        }

        this.step = STEP.DONE;
        this.message = "상점 선택이 완료되었습니다.";
    }

    private void applyDebuffById(DebuffType debuff_id)
    {
        if (this.block_root == null) return;

        switch (debuff_id)
        {
            case DebuffType.SCORE_NULLIFY:
                HashSet<Vector2Int> positions = new HashSet<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
                this.block_root.SetNegativeBlockPositions(positions);
                break;
            case DebuffType.REQUIRE_MATCH_4:
                this.block_root.SetRequireBlocks(4);
                break;
            case DebuffType.HEAT_TIME_DECREASE:
                this.block_root.SetHeatTime(1.5f);
                break;
        }
    }

    private void applyJokerById(JokerType joker_id)
    {
        if (this.score_counter == null || this.score_counter.block_scores == null) return;

        switch (joker_id)
        {
            case JokerType.BLUE_SCORE_UP:
                this.score_counter.block_scores[(int)Block.COLOR.BLUE] = 100;
                break;
            case JokerType.MAGENTA_SCORE_UP:
                this.score_counter.block_scores[(int)Block.COLOR.MAGENTA] = 100;
                break;
            case JokerType.REQUIRE_MATCH_2:
                if (this.block_root != null)
                {
                    this.block_root.SetRequireBlocks(2);
                }
                break;
        }
    }

    private void applyItemById(ItemType item_id)
    {
        if (item_id == ItemType.NONE) return;

        switch (item_id)
        {
            case ItemType.REMOVE_PINK:
                this.block_root.RemoveBlocksByColor(Block.COLOR.PINK);
                break;
            case ItemType.TIME_STOP:
                // TODO: 시간 정지 아이템 구현
                break;
            case ItemType.PLUS_MOVES:
                // TODO: 폭탄 아이템 구현
                break;
        }
    }

    public void ApplyItemEffect()
    {
        switch (this.pending_item_id)
        {
            case ItemType.REMOVE_PINK:
                this.block_root.RemoveBlocksByColor(Block.COLOR.PINK);
                break;
            case ItemType.TIME_STOP:
                // TODO: 시간 정지 아이템 구현
                break;
            case ItemType.PLUS_MOVES:
                StageManager.Instance.PlusCurrentMoves(50);
                break;
        }
    }
}