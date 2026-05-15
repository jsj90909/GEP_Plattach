using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BlockRoot : MonoBehaviour
{ // 블록을 가로세로 바둑판(grid) 모양으로 관리
    public GameObject BlockPrefab = null; // 만들어낼 블록의 프리팹
    public BlockControl[,] blocks; // 그리드

    private GameObject main_camera = null; // 메인 카메라
    private BlockControl grabbed_block = null; // 잡은 블록

    private ScoreCounter score_counter = null; // 점수 카운터 ScoreCounter
    protected bool is_vanishing_prev = false; // 앞에서 발화했는가

    public TextAsset levelData = null; // 레벨 데이터의 텍스트를 저장
    public LevelControl level_control; // LevelControl를 저장

    [SerializeField] private int require_blocks = 3;

    private HashSet<Vector2Int> negative_block_positions = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> move_lock_positions = new HashSet<Vector2Int>();

    public bool preventAutoMatchOnStart = true;

    private JokerRoot joker_root = null;
    private DebuffRoot debuff_root = null;

    void Awake()
    {
        this.main_camera = GameObject.FindGameObjectWithTag("MainCamera"); // 카메라로부터 마우스 커서를 통과하는 광선을 쏘기 위해서 필요
        this.score_counter = this.gameObject.GetComponent<ScoreCounter>();
        this.joker_root = this.gameObject.GetComponent<JokerRoot>();
        this.debuff_root = this.gameObject.GetComponent<DebuffRoot>();
    }

    void Update()
    { // 마우스 좌표와 겹치는지 체크, 잡을 수 있는 상태의 블록을 잡음
        Vector3 mouse_position; // 마우스 위치
        this.unprojectMousePosition(out mouse_position, Input.mousePosition); // 마우스 위치를 가져옴
        Vector2 mouse_position_xy = new Vector2(mouse_position.x, mouse_position.y); // 가져온 마우스 위치를 하나의 Vector2로 모음

        if (this.grabbed_block == null)
        { // 잡은 블록이 비었으면
            if (!this.is_has_falling_block())
            {
                if (Input.GetMouseButtonDown(0))
                { // 마우스 버튼이 눌렸으면
                    foreach (BlockControl block in this.blocks)
                    { // blocks 배열의 모든 요소를 차례로 처리
                        if (!block.isGrabbable())
                        { // 블록을 잡을 수 없다면
                            continue;
                        }

                        if (this.IsMoveLockPosition(new Vector2Int(block.i_pos.x, block.i_pos.y)))
                        { // 블록이 이동 잠금 위치에 있다면
                            continue;
                        }

                        if (!block.isContainedPosition(mouse_position_xy))
                        { // 마우스 위치가 블록 영역 안이 아니면
                            continue;
                        }

                        this.grabbed_block = block; // 처리 중인 블록을 grabbed_block에 등록
                        this.grabbed_block.beginGrab();
                        break;
                    }
                }
            }
        }
        else
        { // 잡은 블록이 비어있지 않으면
            do
            {
                BlockControl swap_target = this.getNextBlock(grabbed_block, grabbed_block.slide_dir); // 슬라이드할 곳의 블록을 가져옴

                if (swap_target == null)
                { // 슬라이드할 곳 블록이 비어 있으면
                    break;
                }

                if (!swap_target.isGrabbable())
                { // 슬라이드할 곳의 블록이 잡을 수 있는 상태가 아니라면
                    break;
                }

                // 현재 위치에서 슬라이드 위치까지의 거리를 얻음
                float offset = this.grabbed_block.calcDirOffset(mouse_position_xy, this.grabbed_block.slide_dir);

                if (offset < Block.COLLISION_SIZE / 2.0f)
                { // 거리가 블록 크기의 절반보다 작다면
                    break;
                }

                this.swapBlock(grabbed_block, grabbed_block.slide_dir, swap_target); // 블록을 교체

                // 이동 횟수 제한 미션일 경우, 실제 교체 성공 시에만 이동 횟수 감소
                if (StageManager.Instance != null)
                {
                    StageManager.Instance.UseMove();
                }

                this.grabbed_block = null; // 지금은 블록을 잡고 있지 않음
            } while (false);

            if (!Input.GetMouseButton(0))
            { // 마우스 버튼이 눌려져 있지 않으면
                this.grabbed_block.endGrab(); // 블록을 놨을 때의 처리를 실행
                this.grabbed_block = null;
            }
        }

        // 낙하 중 또는 슬라이드 중이면
        if (this.is_has_falling_block() || this.is_has_sliding_block())
        {
            // 아무것도 하지 않는다
        }
        else
        {
            int ignite_count = 0; // 불붙은 개수

            // 그리드 안의 모든 블록에 대해서 처리
            foreach (BlockControl block in this.blocks)
            {
                if (!block.isIdle())
                { // 대기 중이 아니면 다음 블록을 처리
                    continue;
                }

                // 세로 또는 가로에 같은 색 블록이 세 개 이상 나열했다면
                if (this.checkConnection(block))
                {
                    ignite_count++; // 불붙은 개수를 증가
                }
            }

            if (ignite_count > 0)
            { // 불붙은 개수가 0보다 크면 = 한 군데라도 맞춰진 곳이 있음
                if (!this.is_vanishing_prev)
                {
                    this.score_counter.clearIgniteCount(); // 연속 점화가 아니라면, 점화 횟수를 리셋
                }

                int[] vanishingblockcolors = GetVanishinBlockColor(); // 연소 중인 블록의 색을 가져옴
                HashSet<Vector2Int> vanishingblockpositions_set = GetVanishingBlockPosition(); // 연소 중인 블록의 위치

                foreach (Vector2Int temp_set in vanishingblockpositions_set)
                {
                    if (negative_block_positions != null)
                    {
                        if (negative_block_positions.Contains(temp_set))
                        {
                            Block.COLOR temp_color = this.blocks[temp_set.x, temp_set.y].color; // 연소 중인 블록의 색
                            vanishingblockcolors[(int)temp_color]--; // 연소 중인 블록의 색의 점수 무효화
                        }
                    }
                }

                this.score_counter.addIgniteCount2(ignite_count, vanishingblockcolors); // 점화 횟수를 증가. 연소 중인 블록의 색도 함께 전달
                this.score_counter.updateTotalScore(); // 합계 점수 갱신

                int block_count = 0; // 불붙는 중인 블록 수

                // 그리드 내의 모든 블록에 대해서 처리
                foreach (BlockControl block in this.blocks)
                {
                    if (block.isVanishing())
                    { // 타는 중이면
                        block.rewindVanishTimer(); // 다시 점화
                        block_count++; // 발화 중인 블록 개수를 증가
                    }
                }
            }
        }

        bool is_vanishing = this.is_has_vanishing_block(); // 하나라도 연소 중인 블록이 있는가?

        do
        {
            if (is_vanishing)
            {
                break;
            }

            if (this.is_has_sliding_block())
            {
                break;
            }

            for (int x = 0; x < Block.BLOCK_NUM_X; x++)
            {
                if (this.is_has_sliding_block_in_column(x))
                { // 열에 교체 중인 블록이 있다면 그 열은 처리하지 않고 다음 열로 진행
                    continue;
                }

                for (int y = 0; y < Block.BLOCK_NUM_Y - 1; y++)
                { // 그 열에 있는 블록을 위에서부터 검사
                    if (!this.blocks[x, y].isVacant())
                    { // 지정 블록이 비표시가 아니라면 다음 블록으로
                        continue;
                    }

                    for (int y1 = y + 1; y1 < Block.BLOCK_NUM_Y; y1++)
                    { // 지정 블록 아래에 있는 블록을 검사
                        if (this.blocks[x, y1].isVacant())
                        {
                            continue;
                        }

                        this.fallBlock(this.blocks[x, y], Block.DIR4.UP, this.blocks[x, y1]); // 블록을 교체한다
                        break;
                    }
                }
            }

            for (int x = 0; x < Block.BLOCK_NUM_X; x++)
            { // 보충처리
                int fall_start_y = Block.BLOCK_NUM_Y;

                for (int y = 0; y < Block.BLOCK_NUM_Y; y++)
                {
                    if (!this.blocks[x, y].isVacant())
                    {
                        continue;
                    }

                    this.blocks[x, y].beginRespawn(fall_start_y); // 블록 부활
                    fall_start_y++;
                }
            }
        } while (false);

        this.is_vanishing_prev = is_vanishing;
    }

    // 블록을 만들어 내고 가로 9칸, 세로 9칸에 배치
    public void initialSetUp()
    {
        this.blocks = new BlockControl[Block.BLOCK_NUM_X, Block.BLOCK_NUM_Y]; // 그리드의 크기를 9×9로

        int color_index = 0; // 블록의 색 번호
        Block.COLOR color = Block.COLOR.FIRST; // 나열할 초기 배치 블록도 선택된 레벨의 출현 패턴을 따르게 하는 수정

        for (int y = 0; y < Block.BLOCK_NUM_Y; ++y)
        {
            for (int x = 0; x < Block.BLOCK_NUM_X; ++x)
            {
                GameObject game_object = Instantiate(this.BlockPrefab) as GameObject;
                BlockControl block = game_object.GetComponent<BlockControl>();

                this.blocks[x, y] = block;

                block.i_pos.x = x;
                block.i_pos.y = y;
                block.block_root = this;

                Vector3 position = BlockRoot.calcBlockPosition(block.i_pos);
                block.transform.position = position;

                // 현재 출현 확률을 바탕으로 색을 결정
                color = this.selectBlockColor();
                block.setColor(color);

                block.name = "block(" + block.i_pos.x.ToString() + "," + block.i_pos.y.ToString() + ")";

                color_index = Random.Range(0, (int)Block.COLOR.NORMAL_COLOR_NUM);
            }
        }

        if (this.preventAutoMatchOnStart)
        {
            this.RemoveInitialMatches();
        }
    }

    // 지정된 그리드 좌표로 씬에서의 좌표를 구함
    public static Vector3 calcBlockPosition(Block.iPosition i_pos)
    {
        Vector3 position = new Vector3(
            -(Block.BLOCK_NUM_X / 2.0f - 0.5f),
            -(Block.BLOCK_NUM_Y / 2.0f - 0.5f),
            0.0f
        );

        position.x += (float)i_pos.x * Block.COLLISION_SIZE;
        position.y += (float)i_pos.y * Block.COLLISION_SIZE;

        return position;
    }

    public bool unprojectMousePosition(out Vector3 world_position, Vector3 mouse_position)
    {
        bool ret;

        Plane plane = new Plane(Vector3.back, new Vector3(0.0f, 0.0f, -Block.COLLISION_SIZE / 2.0f));
        Ray ray = this.main_camera.GetComponent<Camera>().ScreenPointToRay(mouse_position);

        float depth;

        if (plane.Raycast(ray, out depth))
        {
            world_position = ray.origin + ray.direction * depth;
            ret = true;
        }
        else
        {
            world_position = Vector3.zero;
            ret = false;
        }

        return ret;
    }

    public BlockControl getNextBlock(BlockControl block, Block.DIR4 dir)
    {
        BlockControl next_block = null;

        switch (dir)
        {
            case Block.DIR4.RIGHT:
                if (block.i_pos.x < Block.BLOCK_NUM_X - 1)
                {
                    next_block = this.blocks[block.i_pos.x + 1, block.i_pos.y];
                }
                break;

            case Block.DIR4.LEFT:
                if (block.i_pos.x > 0)
                {
                    next_block = this.blocks[block.i_pos.x - 1, block.i_pos.y];
                }
                break;

            case Block.DIR4.UP:
                if (block.i_pos.y < Block.BLOCK_NUM_Y - 1)
                {
                    next_block = this.blocks[block.i_pos.x, block.i_pos.y + 1];
                }
                break;

            case Block.DIR4.DOWN:
                if (block.i_pos.y > 0)
                {
                    next_block = this.blocks[block.i_pos.x, block.i_pos.y - 1];
                }
                break;
        }

        return next_block;
    }

    public static Vector3 getDirVector(Block.DIR4 dir)
    {
        Vector3 v = Vector3.zero;

        switch (dir)
        {
            case Block.DIR4.RIGHT:
                v = Vector3.right;
                break;

            case Block.DIR4.LEFT:
                v = Vector3.left;
                break;

            case Block.DIR4.UP:
                v = Vector3.up;
                break;

            case Block.DIR4.DOWN:
                v = Vector3.down;
                break;
        }

        v *= Block.COLLISION_SIZE;

        return v;
    }

    public static Block.DIR4 getOppositDir(Block.DIR4 dir)
    {
        Block.DIR4 opposit = dir;

        switch (dir)
        {
            case Block.DIR4.RIGHT:
                opposit = Block.DIR4.LEFT;
                break;

            case Block.DIR4.LEFT:
                opposit = Block.DIR4.RIGHT;
                break;

            case Block.DIR4.UP:
                opposit = Block.DIR4.DOWN;
                break;

            case Block.DIR4.DOWN:
                opposit = Block.DIR4.UP;
                break;
        }

        return opposit;
    }

    public void swapBlock(BlockControl block0, Block.DIR4 dir, BlockControl block1)
    {
        Block.COLOR color0 = block0.color;
        Block.COLOR color1 = block1.color;

        Vector3 scale0 = block0.transform.localScale;
        Vector3 scale1 = block1.transform.localScale;

        float vanish_timer0 = block0.vanish_timer;
        float vanish_timer1 = block1.vanish_timer;

        Vector3 offset0 = BlockRoot.getDirVector(dir);
        Vector3 offset1 = BlockRoot.getDirVector(BlockRoot.getOppositDir(dir));

        block0.setColor(color1);
        block1.setColor(color0);

        block0.transform.localScale = scale1;
        block1.transform.localScale = scale0;

        block0.vanish_timer = vanish_timer1;
        block1.vanish_timer = vanish_timer0;

        block0.beginSlide(offset0);
        block1.beginSlide(offset1);
    }

    // 인수로 받은 블록이 세 개의 블록 안에 들어가는 지 파악하는 메서드
    public bool checkConnection(BlockControl start)
    {
        bool ret = false;
        int normal_block_num = 0;

        if (!start.isVanishing())
        {
            normal_block_num = 1;
        }

        int rx = start.i_pos.x;
        int lx = start.i_pos.x;

        for (int x = lx - 1; x >= 0; x--)
        {
            BlockControl next_block = this.blocks[x, start.i_pos.y];

            if (next_block.color != start.color) { break; }
            if (next_block.step == Block.STEP.FALL || next_block.next_step == Block.STEP.FALL) { break; }
            if (next_block.step == Block.STEP.SLIDE || next_block.next_step == Block.STEP.SLIDE) { break; }

            if (!next_block.isVanishing())
            {
                normal_block_num++;
            }

            lx = x;
        }

        for (int x = rx + 1; x < Block.BLOCK_NUM_X; x++)
        {
            BlockControl next_block = this.blocks[x, start.i_pos.y];

            if (next_block.color != start.color) { break; }
            if (next_block.step == Block.STEP.FALL || next_block.next_step == Block.STEP.FALL) { break; }
            if (next_block.step == Block.STEP.SLIDE || next_block.next_step == Block.STEP.SLIDE) { break; }

            if (!next_block.isVanishing())
            {
                normal_block_num++;
            }

            rx = x;
        }

        do
        {
            if (rx - lx + 1 < require_blocks) { break; }
            if (normal_block_num == 0) { break; }

            for (int x = lx; x < rx + 1; x++)
            {
                this.blocks[x, start.i_pos.y].toVanishing();
                ret = true;
            }
        } while (false);

        normal_block_num = 0;

        if (!start.isVanishing())
        {
            normal_block_num = 1;
        }

        int uy = start.i_pos.y;
        int dy = start.i_pos.y;

        for (int y = dy - 1; y >= 0; y--)
        {
            BlockControl next_block = this.blocks[start.i_pos.x, y];

            if (next_block.color != start.color) { break; }
            if (next_block.step == Block.STEP.FALL || next_block.next_step == Block.STEP.FALL) { break; }
            if (next_block.step == Block.STEP.SLIDE || next_block.next_step == Block.STEP.SLIDE) { break; }

            if (!next_block.isVanishing())
            {
                normal_block_num++;
            }

            dy = y;
        }

        for (int y = uy + 1; y < Block.BLOCK_NUM_Y; y++)
        {
            BlockControl next_block = this.blocks[start.i_pos.x, y];

            if (next_block.color != start.color) { break; }
            if (next_block.step == Block.STEP.FALL || next_block.next_step == Block.STEP.FALL) { break; }
            if (next_block.step == Block.STEP.SLIDE || next_block.next_step == Block.STEP.SLIDE) { break; }

            if (!next_block.isVanishing())
            {
                normal_block_num++;
            }

            uy = y;
        }

        do
        {
            if (uy - dy + 1 < require_blocks) { break; }
            if (normal_block_num == 0) { break; }

            for (int y = dy; y < uy + 1; y++)
            {
                this.blocks[start.i_pos.x, y].toVanishing();
                ret = true;
            }
        } while (false);

        return ret;
    }

    private bool is_has_vanishing_block()
    {
        bool ret = false;

        foreach (BlockControl block in this.blocks)
        {
            if (block.vanish_timer > 0.0f)
            {
                ret = true;
                break;
            }
        }

        return ret;
    }

    private (bool, Block.COLOR) is_has_vanishing_block(bool _)
    {
        bool ret = false;
        Block.COLOR color = Block.COLOR.GRAY;

        foreach (BlockControl block in this.blocks)
        {
            if (block.vanish_timer > 0.0f)
            {
                color = block.color;
                ret = true;
                break;
            }
        }

        return (ret, color);
    }

    private bool is_has_sliding_block()
    {
        bool ret = false;

        foreach (BlockControl block in this.blocks)
        {
            if (block.step == Block.STEP.SLIDE)
            {
                ret = true;
                break;
            }
        }

        return ret;
    }

    private bool is_has_falling_block()
    {
        bool ret = false;

        foreach (BlockControl block in this.blocks)
        {
            if (block.step == Block.STEP.FALL)
            {
                ret = true;
                break;
            }
        }

        return ret;
    }

    public void fallBlock(BlockControl block0, Block.DIR4 dir, BlockControl block1)
    {
        Block.COLOR color0 = block0.color;
        Block.COLOR color1 = block1.color;

        Vector3 scale0 = block0.transform.localScale;
        Vector3 scale1 = block1.transform.localScale;

        float vanish_timer0 = block0.vanish_timer;
        float vanish_timer1 = block1.vanish_timer;

        bool visible0 = block0.isVisible();
        bool visible1 = block1.isVisible();

        Block.STEP step0 = block0.step;
        Block.STEP step1 = block1.step;

        block0.setColor(color1);
        block1.setColor(color0);

        block0.transform.localScale = scale1;
        block1.transform.localScale = scale0;

        block0.vanish_timer = vanish_timer1;
        block1.vanish_timer = vanish_timer0;

        block0.setVisible(visible1);
        block1.setVisible(visible0);

        block0.step = step1;
        block1.step = step0;

        block0.beginFall(block1);
    }

    private bool is_has_sliding_block_in_column(int x)
    {
        bool ret = false;

        for (int y = 0; y < Block.BLOCK_NUM_Y; y++)
        {
            if (this.blocks[x, y].isSliding())
            {
                ret = true;
                break;
            }
        }

        return ret;
    }

    public void create()
    {
        this.level_control = new LevelControl();
        this.level_control.initialize();
        this.level_control.loadLevelData(this.levelData);
        this.level_control.selectLevel();
    }

    public Block.COLOR selectBlockColor()
    {
        Block.COLOR color = Block.COLOR.FIRST;

        LevelData level_data = this.level_control.getCurrentLevelData();
        float rand = Random.Range(0.0f, 1.0f);
        float sum = 0.0f;
        int i = 0;

        for (i = 0; i < level_data.probability.Length - 1; i++)
        {
            if (level_data.probability[i] == 0.0f)
            {
                continue;
            }

            sum += level_data.probability[i];

            if (rand < sum)
            {
                break;
            }
        }

        color = (Block.COLOR)i;

        return color;
    }

    public int[] GetVanishinBlockColor()
    {
        int[] countBlockColors = new int[(int)Block.COLOR.NUM];
        countBlockColors.Initialize();

        foreach (BlockControl block in this.blocks)
        {
            if (block.vanish_timer > 0.0f)
            {
                countBlockColors[(int)block.color]++;
            }
        }

        return countBlockColors;
    }

    public HashSet<Vector2Int> GetVanishingBlockPosition()
    {
        HashSet<Vector2Int> positionSet = new HashSet<Vector2Int>();

        foreach (BlockControl block in this.blocks)
        {
            if (block.vanish_timer > 0.0f)
            {
                positionSet.Add(new Vector2Int(block.i_pos.x, block.i_pos.y));
            }
        }

        return positionSet;
    }

    public void SetRequireBlocks(int num)
    {
        this.require_blocks = num;
    }

    public void SetNegativeBlockPositions(HashSet<Vector2Int> positions)
    {
        this.negative_block_positions = positions;
        this.debuff_root.InstantiateDebuff("NegativeBlock", positions);
    }

    public void AddNegativeBlockPosition(Vector2Int position)
    {
        this.negative_block_positions.Add(position);
        this.debuff_root.InstantiateDebuff("NegativeBlock", new HashSet<Vector2Int> { position });
    }

    public bool IsNegativeBlockPosition(Vector2Int position)
    {
        return this.negative_block_positions.Contains(position);
    }

    public void ClearNegativeBlockPositions()
    {
        this.negative_block_positions.Clear();
    }

    public void RemoveNegativeBlockPosition(Vector2Int position)
    {
        this.negative_block_positions.Remove(position);
    }

    public void SetMoveLockPositions(HashSet<Vector2Int> positions)
    {
        this.move_lock_positions = positions;
        this.debuff_root.InstantiateDebuff("MoveLock", positions);
    }

    public void AddMoveLockPosition(Vector2Int position)
    {
        if (this.move_lock_positions == null)
        {
            this.move_lock_positions = new HashSet<Vector2Int>();
        }

        this.move_lock_positions.Add(position);
        this.debuff_root.InstantiateDebuff("MoveLock", new HashSet<Vector2Int> { position });
    }

    public bool IsMoveLockPosition(Vector2Int position)
    {
        if (this.move_lock_positions == null)
        {
            return false;
        }

        return this.move_lock_positions.Contains(position);
    }

    public void ClearMoveLockPositions()
    {
        if (this.move_lock_positions != null)
        {
            this.move_lock_positions.Clear();
        }
    }

    public void RemoveMoveLockPosition(Vector2Int position)
    {
        if (this.move_lock_positions != null)
        {
            this.move_lock_positions.Remove(position);
        }
    }

    public void SetHeatTime(float time)
    {
        this.level_control.setVanishTime(time);
    }

    public void SetProbability(Block.COLOR color, float probability)
    {
        this.level_control.setProbability(color, probability);
    }

    private void RemoveInitialMatches()
    {
        float[] probabilities = this.level_control.getCurrentLevelData().probability;

        for (int y = 0; y < Block.BLOCK_NUM_Y; y++)
        {
            for (int x = 0; x < Block.BLOCK_NUM_X; x++)
            {
                BlockControl block = this.blocks[x, y];

                if (CheckMatchAt(x, y, block.color))
                {
                    List<Block.COLOR> safeColors = new List<Block.COLOR>();
                    float safeProbabilitySum = 0.0f;

                    for (int c = 0; c < (int)Block.COLOR.NORMAL_COLOR_NUM; c++)
                    {
                        Block.COLOR testColor = (Block.COLOR)c;

                        if (!CheckMatchAt(x, y, testColor) && probabilities[c] > 0.0f)
                        {
                            safeColors.Add(testColor);
                            safeProbabilitySum += probabilities[c];
                        }
                    }

                    if (safeColors.Count > 0)
                    {
                        float rand = Random.Range(0.0f, safeProbabilitySum);
                        float currentSum = 0.0f;
                        Block.COLOR selectedSafeColor = safeColors[0];

                        foreach (Block.COLOR color in safeColors)
                        {
                            currentSum += probabilities[(int)color];

                            if (rand <= currentSum)
                            {
                                selectedSafeColor = color;
                                break;
                            }
                        }

                        block.setColor(selectedSafeColor);
                    }
                    else
                    {
                        block.setColor(this.selectBlockColor());
                    }
                }
            }
        }
    }

    private bool CheckMatchAt(int x, int y, Block.COLOR colorToCheck)
    {
        if (x >= (require_blocks - 1))
        {
            bool match_x = true;

            for (int i = 1; i < require_blocks; i++)
            {
                if (this.blocks[x - i, y].color != colorToCheck)
                {
                    match_x = false;
                    break;
                }
            }

            if (match_x)
            {
                return true;
            }
        }

        if (y >= (require_blocks - 1))
        {
            bool match_y = true;

            for (int i = 1; i < require_blocks; i++)
            {
                if (this.blocks[x, y - i].color != colorToCheck)
                {
                    match_y = false;
                    break;
                }
            }

            if (match_y)
            {
                return true;
            }
        }

        return false;
    }

    public void ClearBoard()
    {
        if (this.blocks != null)
        {
            for (int y = 0; y < Block.BLOCK_NUM_Y; ++y)
            {
                for (int x = 0; x < Block.BLOCK_NUM_X; ++x)
                {
                    if (this.blocks[x, y] != null)
                    {
                        Destroy(this.blocks[x, y].gameObject);
                    }
                }
            }

            this.blocks = null;
        }
    }

    public void SetExactProbability(Block.COLOR color, float targetRatio)
    {
        targetRatio = Mathf.Clamp(targetRatio, 0.0f, 1.0f);

        LevelData level_data = this.level_control.getCurrentLevelData();

        float sumOthers = 0.0f;

        for (int i = 0; i < level_data.probability.Length; i++)
        {
            if (i != (int)color)
            {
                sumOthers += level_data.probability[i];
            }
        }

        if (targetRatio >= 1.0f)
        {
            for (int i = 0; i < level_data.probability.Length; i++)
            {
                level_data.probability[i] = (i == (int)color) ? 1.0f : 0.0f;
            }

            this.SetProbability(color, 1.0f);
            return;
        }

        if (sumOthers <= 0.0f)
        {
            float distribute = 1.0f / (level_data.probability.Length - 1);

            for (int i = 0; i < level_data.probability.Length; i++)
            {
                if (i != (int)color)
                {
                    level_data.probability[i] = distribute;
                    sumOthers += distribute;
                }
            }
        }

        float requiredRawProbability = (targetRatio * sumOthers) / (1.0f - targetRatio);

        this.SetProbability(color, requiredRawProbability);
    }

    public void SetEqualProbabilities()
    {
        int colorCount = (int)Block.COLOR.NORMAL_COLOR_NUM;
        float equalProb = 1.0f / colorCount;

        LevelData level_data = this.level_control.getCurrentLevelData();

        for (int i = 0; i < colorCount; i++)
        {
            level_data.probability[i] = equalProb;
        }

        level_data.normalize();

        Debug.Log("All probabilities set equally: " + string.Join(", ", level_data.probability));
    }

    public void SetProbabilityAndDistributeEqually(Block.COLOR targetColor, float targetProbability)
    {
        targetProbability = Mathf.Clamp(targetProbability, 0.0f, 1.0f);

        int colorCount = (int)Block.COLOR.NORMAL_COLOR_NUM;
        float remainingProbability = 1.0f - targetProbability;
        float distributeProbability = remainingProbability / (colorCount - 1);

        LevelData level_data = this.level_control.getCurrentLevelData();

        for (int i = 0; i < colorCount; i++)
        {
            if (i == (int)targetColor)
            {
                level_data.probability[i] = targetProbability;
            }
            else
            {
                level_data.probability[i] = distributeProbability;
            }
        }

        level_data.normalize();

        Debug.Log($"Target {targetColor} set to {targetProbability}. Others set to {distributeProbability}.");
        Debug.Log("Current Probabilities: " + string.Join(", ", level_data.probability));
    }
}