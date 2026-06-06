using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 상점 아이템 식별을 위한 Enum 정의
public enum DebuffType { NONE, HEAT_TIME_DECREASE, SCORE_NULLIFY, REQUIRE_MATCH_4, MOVE_LOCK, MAGENTA_PROBABILITY_UP }
public enum JokerType
{
    NONE,
    BLUE_SCORE_UP,
    BLUE_PROBABILITY_UP,
    REQUIRE_MATCH_2,
    ALL_SCORE_UP,
    GREEN_PROBABILITY_ZERO,
    HEAT_TIME_INCREASE,
    ORANGE_HEAT_GROWTH
}
public enum ItemType { NONE, REMOVE_PINK, SCORE_MULTIPLIER, PLUS_MOVES }

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
    private SceneControl scene_control = null;
    private DebuffRoot debuff_root = null;

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
    public GUIStyle gold_style;

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

    private List<DebuffData> current_displayed_debuffs = new List<DebuffData>();
    private List<JokerData> current_displayed_jokers = new List<JokerData>();

    private HashSet<DebuffType> applied_debuffs = new HashSet<DebuffType>();
    private HashSet<JokerType> applied_jokers = new HashSet<JokerType>();

    void Start()
    {
        this.score_counter = this.gameObject.GetComponent<ScoreCounter>();
        this.block_root = this.gameObject.GetComponent<BlockRoot>();
        this.item_root = this.gameObject.GetComponent<ItemRoot>();
        this.scene_control = this.gameObject.GetComponent<SceneControl>();
        this.debuff_root = this.gameObject.GetComponent<DebuffRoot>();

        this.createShopData();
    }

    // 다음 스테이지 보드 생성 전(SceneControl.Start) 호출될 효과 적용 메서드
    public void ApplyPendingEffects()
    {
        if (this.block_root == null) this.block_root = this.gameObject.GetComponent<BlockRoot>();
        if (this.score_counter == null) this.score_counter = this.gameObject.GetComponent<ScoreCounter>();
        if (this.item_root == null) this.item_root = this.gameObject.GetComponent<ItemRoot>();
        if (this.debuff_root == null) this.debuff_root = this.gameObject.GetComponent<DebuffRoot>();

        // 1. 누적된 디버프 모두 씌우기
        foreach (DebuffType debuff in this.pending_debuff_ids)
        {
            bool is_first_apply = !applied_debuffs.Contains(debuff);
            this.applyDebuffById(debuff, is_first_apply);

            if (is_first_apply) applied_debuffs.Add(debuff);
        }

        // 2. 누적된 조커 모두 씌우기
        foreach (JokerType joker in this.pending_joker_ids)
        {
            bool is_first_apply = !applied_jokers.Contains(joker);
            this.applyJokerById(joker, is_first_apply);

            if (is_first_apply) applied_jokers.Add(joker);
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
        this.text_style.fontSize = 28;
        this.text_style.normal.textColor = Color.white;
        this.text_style.wordWrap = true;

        this.button_style = new GUIStyle(GUI.skin.button);
        this.button_style.fontSize = 24;
        this.button_style.alignment = TextAnchor.MiddleCenter;

        this.gold_style = new GUIStyle();
        this.gold_style.fontSize = 40;
        this.gold_style.normal.textColor = Color.yellow;

        this.gui_style_initialized = true;
    }

    private void createShopData()
    {
        debuff_list.Add(new DebuffData(DebuffType.HEAT_TIME_DECREASE, "불타는 시간 감소", "다음 스테이지부터 블록이 더 빨리 사라집니다.", 400));
        debuff_list.Add(new DebuffData(DebuffType.SCORE_NULLIFY, "특정 구역 점수 무효화", "다음 스테이지부터 일부 구역의 블록 점수가 무효화됩니다.", 150));
        debuff_list.Add(new DebuffData(DebuffType.REQUIRE_MATCH_4, "4개 매치 필요", "다음 스테이지부터 4개 이상 연결해야 점수가 납니다.", 500));
        debuff_list.Add(new DebuffData(DebuffType.MOVE_LOCK, "이동 불가 구역 생성", "다음 스테이지부터 이동 불가 구역이 생성됩니다.", 150));
        debuff_list.Add(new DebuffData(DebuffType.MAGENTA_PROBABILITY_UP, "마젠타 블록 확률 생성", "다음 스테이지부터 마젠타 블록 등장 확률이 생성됩니다.", 300));

        joker_list.Add(new JokerData(JokerType.BLUE_SCORE_UP, "파란색 블록 점수 증가", "파란색 블록 점수를 1000점 증가시킵니다.", 100));
        joker_list.Add(new JokerData(JokerType.BLUE_PROBABILITY_UP, "파란색 블록 확률 증가", "파란색 블록 등장 확률을 증가시킵니다.", 150));
        joker_list.Add(new JokerData(JokerType.REQUIRE_MATCH_2, "매치 요구 수 감소", "다음 스테이지부터 2개만 연결하면 점수가 납니다.(4개 요구 무효화)", 450));
        joker_list.Add(new JokerData(JokerType.ALL_SCORE_UP, "전체 블록 점수 증가", "모든 색깔 블록의 점수를 500점 증가시킵니다.", 200));
        joker_list.Add(new JokerData(JokerType.GREEN_PROBABILITY_ZERO, "초록색 블록 제거", "다음 스테이지부터 초록색 블록이 등장하지 않습니다.", 250));
        joker_list.Add(new JokerData(JokerType.HEAT_TIME_INCREASE, "불타는 시간 증가", "다음 스테이지부터 블록이 더 느리게 사라집니다.", 150));
        joker_list.Add(new JokerData(JokerType.ORANGE_HEAT_GROWTH, "불타는 시간 누적형 증가", "주황색 블록이 연쇄될 때마다 현재 스테이지의 연소 시간이 0.05초 증가합니다.", 100));

        item_list.Add(new ItemData(ItemType.REMOVE_PINK, "살구색 제거", "그리드의 살구색 블록을 제거합니다."));
        item_list.Add(new ItemData(ItemType.SCORE_MULTIPLIER, "점수 2배", "사용 시 일정 시간 동안 모든 블록의 획득 점수가 2배가 됩니다."));
        item_list.Add(new ItemData(ItemType.PLUS_MOVES, "이동 횟수 증가", "현재 스테이지의 이동 횟수 제한을 증가시킵니다."));
    }

    public void OpenShop()
    {
        this.step = STEP.DEBUFF_SELECT;

        SoundManager.Instance.SetShopVolume(true);

        this.selected_debuff_id = DebuffType.NONE;
        this.selected_joker_id = JokerType.NONE;
        this.selected_item_id = ItemType.NONE;

        this.GenerateRandomDebuffs();

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
            this.gold_style
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

        // debuff_list 대신 current_displayed_debuffs 사용
        for (int i = 0; i < current_displayed_debuffs.Count; i++)
        {
            DebuffData data = current_displayed_debuffs[i];
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

        for (int i = 0; i < current_displayed_jokers.Count; i++)
        {
            JokerData data = current_displayed_jokers[i];
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

            // 이미 구매한 조커인지 확인하여 품절 표시
            if (this.pending_joker_ids.Contains(data.id))
            {
                GUIStyle soldout_style = new GUIStyle(this.text_style);
                soldout_style.normal.textColor = Color.gray;
                soldout_style.alignment = TextAnchor.MiddleCenter;

                GUI.Label(
                    new Rect(box_rect.x + 760, box_rect.y + 52, 170, 65),
                    "구매 완료",
                    soldout_style
                );
            }
            else
            {
                if (GUI.Button(
                    new Rect(box_rect.x + 760, box_rect.y + 52, 170, 65),
                    "구매",
                    this.button_style))
                {
                    this.selectJoker(data);
                }
            }
        }

        // 수동으로 아이템 선택으로 넘어가는 버튼
        if (GUI.Button(
            new Rect(Screen.width / 2 - 170, Screen.height - 175, 340, 65),
            "아이템 선택으로 넘어가기",
            this.button_style))
        {
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

        // 누적 보유 중인 조커 목록을 한 줄 문자열로 생성
        string purchasedJokers = "없음";
        if (this.pending_joker_ids.Count > 0)
        {
            List<string> names = new List<string>();
            foreach (var id in this.pending_joker_ids)
            {
                names.Add(joker_list.Find(j => j.id == id)?.name ?? id.ToString());
            }
            purchasedJokers = string.Join(", ", names);
        }

        GUI.Label(
        new Rect(box_rect.x + 40, box_rect.y + 35, 920, 350),
        $"상점 선택 완료\n\n" +
        $"선택한 디버프 : {debuff_list.Find(d => d.id == this.selected_debuff_id)?.name ?? this.selected_debuff_id.ToString()}\n" +
        $"보유 중인 조커 : {purchasedJokers}\n" +
        $"선택한 사용 아이템 : {item_list.Find(i => i.id == this.selected_item_id)?.name ?? this.selected_item_id.ToString()}\n\n" +
        $"현재 골드 : {player_gold}",
        this.text_style
        );

        if (GUI.Button(
            new Rect(Screen.width / 2 - 180, Screen.height - 220, 360, 70),
            "다음 스테이지 시작",
            this.button_style))
        {
            SoundManager.Instance.SetShopVolume(false);
            StageManager.Instance.NextStage();
        }

        if (GUI.Button(
            new Rect(Screen.width / 2 - 180, Screen.height - 135, 360, 70),
            "타이틀로 돌아가기",
            this.button_style))
        {
            SoundManager.Instance.SetShopVolume(false);
            this.step = STEP.CLOSED;
            SceneManager.LoadScene("TitleScene");
        }
    }

    private void selectDebuff(DebuffData data)
    {
        this.selected_debuff_id = data.id; // 완료 창 표시용
        this.pending_debuff_ids.Add(data.id); // 실제 효과 리스트에 누적

        this.player_gold += data.reward_gold;

        this.GenerateRandomJokers();

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

        this.selected_joker_id = data.id;
        this.pending_joker_ids.Add(data.id);

        this.player_gold -= data.price;
        this.message = data.name + " 구매 완료!";

        // 구매 직후, 더 살 수 있는 조커가 남아있는지 검사 (자동 전환 로직)
        bool canBuyMore = false;
        foreach (JokerData joker in current_displayed_jokers)
        {
            if (!this.pending_joker_ids.Contains(joker.id) && this.player_gold >= joker.price)
            {
                canBuyMore = true;
                break;
            }
        }

        if (!canBuyMore)
        {
            this.step = STEP.ITEM_SELECT;
            this.message = "더 이상 구매할 수 있는 조커가 없습니다. 사용 아이템을 선택하세요.";
        }
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

    private void applyDebuffById(DebuffType debuff_id, bool is_first_apply)
    {
        if (this.block_root == null) return;

        switch (debuff_id)
        {
            case DebuffType.SCORE_NULLIFY:
                HashSet<Vector2Int> positions = new HashSet<Vector2Int>();
                for (int i = 0; i < Block.BLOCK_NUM_X / 2; ++i)
                {
                    for (int j = 0; j < Block.BLOCK_NUM_Y / 2; ++j)
                    {
                        positions.Add(new Vector2Int(i, j));
                    }
                }
                this.block_root.SetNegativeBlockPositions(positions);
                break;
            case DebuffType.REQUIRE_MATCH_4:
                this.block_root.SetRequireBlocks(4);
                break;
            case DebuffType.HEAT_TIME_DECREASE:
                if (is_first_apply) this.block_root.PlusHeatTime(-0.5f);
                break;
            case DebuffType.MOVE_LOCK:
                HashSet<Vector2Int> moveLockPositions = new HashSet<Vector2Int>();
                for (int i = 0; i < Block.BLOCK_NUM_X; ++i)
                {
                    Vector2Int pos = new Vector2Int(i, 4);
                    moveLockPositions.Add(pos);
                }
                this.debuff_root.CreateMoveLock(moveLockPositions);
                this.block_root.SetMoveLockPositions(moveLockPositions);
                break;
            case DebuffType.MAGENTA_PROBABILITY_UP:
                if (is_first_apply) this.block_root.IncreaseBlockProbability(Block.COLOR.MAGENTA, 0.15f);
                break;
        }
    }

    private void applyJokerById(JokerType joker_id, bool is_first_apply)
    {
        if (this.score_counter == null)
        {
            this.score_counter = this.gameObject.GetComponent<ScoreCounter>();
        }

        if (this.block_root == null)
        {
            this.block_root = this.gameObject.GetComponent<BlockRoot>();
        }

        if (this.score_counter == null) return;

        switch (joker_id)
        {
            case JokerType.BLUE_SCORE_UP:
                this.score_counter.joker_score_overrides[(int)Block.COLOR.BLUE] = 1000;
                break;

            case JokerType.BLUE_PROBABILITY_UP:
                if (this.block_root != null && is_first_apply)
                {
                    this.block_root.IncreaseBlockProbability(Block.COLOR.BLUE, 0.2f);
                }
                break;

            case JokerType.REQUIRE_MATCH_2:
                if (this.block_root != null)
                {
                    this.block_root.SetRequireBlocks(2);
                }
                break;

            case JokerType.ALL_SCORE_UP:
                for (int i = 0; i < (int)Block.COLOR.NUM; i++)
                {
                    this.score_counter.joker_score_overrides[i] = 500;
                }
                break;

            case JokerType.GREEN_PROBABILITY_ZERO:
                if (this.block_root != null)
                {
                    this.block_root.SetBlockProbability(Block.COLOR.GREEN, 0.0f);
                }
                break;

            case JokerType.HEAT_TIME_INCREASE:
                if (this.block_root != null && is_first_apply)
                {
                    this.block_root.PlusHeatTime(1.0f);
                }
                break;

            case JokerType.ORANGE_HEAT_GROWTH:
                if (this.block_root != null)
                {
                    this.block_root.EnableOrangeHeatGrowthJoker();
                }
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
            case ItemType.SCORE_MULTIPLIER:
                if (this.score_counter != null)
                {
                    // 2배수 적용, 20초 동안 지속
                    this.score_counter.ActivateScoreMultiplier(2, 20.0f);
                }
                break;
            case ItemType.PLUS_MOVES:
                StageManager.Instance.PlusCurrentMoves(20);
                break;
        }
    }

    // 리스트를 무작위로 섞어주는 헬퍼 함수
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private bool ContainsDisplayedJoker(JokerType joker_id)
    {
        foreach (JokerData joker in current_displayed_jokers)
        {
            if (joker.id == joker_id)
            {
                return true;
            }
        }

        return false;
    }

    // 1. 디버프 랜덤 추출 함수
    private void GenerateRandomDebuffs()
    {
        List<DebuffData> available_debuffs = new List<DebuffData>();
        foreach (var d in debuff_list)
        {
            if (!pending_debuff_ids.Contains(d.id))
            {
                available_debuffs.Add(d);
            }
        }
        ShuffleList(available_debuffs);
        current_displayed_debuffs = available_debuffs.GetRange(0, Mathf.Min(3, available_debuffs.Count));
    }

    // 2. 조커 랜덤 추출 함수 (현재 골드 기반)
    private void GenerateRandomJokers()
    {
        List<JokerData> available_jokers = new List<JokerData>();
        foreach (var j in joker_list)
        {
            if (!pending_joker_ids.Contains(j.id))
            {
                // 2개 매치 조커이면서 현재 플레이어 골드(디버프 획득 골드 포함)가 가격보다 적으면 제외
                if (j.id == JokerType.REQUIRE_MATCH_2 && this.player_gold < j.price)
                {
                    continue;
                }

                available_jokers.Add(j);
            }
        }

        ShuffleList(available_jokers);

        current_displayed_jokers.Clear();

        foreach (JokerData joker in available_jokers)
        {
            bool is_heat_time_joker_pair =
                (joker.id == JokerType.HEAT_TIME_INCREASE && ContainsDisplayedJoker(JokerType.ORANGE_HEAT_GROWTH)) ||
                (joker.id == JokerType.ORANGE_HEAT_GROWTH && ContainsDisplayedJoker(JokerType.HEAT_TIME_INCREASE));

            if (is_heat_time_joker_pair)
            {
                continue;
            }

            current_displayed_jokers.Add(joker);

            if (current_displayed_jokers.Count >= 3)
            {
                break;
            }
        }
    }
}