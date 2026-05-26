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

        if (Input.GetKeyDown(KeyCode.R))
        {
            this.SpawnRainbowBlock();
        }

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

        bool rainbow0 = block0.is_rainbow;
        bool rainbow1 = block1.is_rainbow;

        Vector3 scale0 = block0.transform.localScale;
        Vector3 scale1 = block1.transform.localScale;

        float vanish_timer0 = block0.vanish_timer;
        float vanish_timer1 = block1.vanish_timer;

        Vector3 offset0 = BlockRoot.getDirVector(dir);
        Vector3 offset1 = BlockRoot.getDirVector(BlockRoot.getOppositDir(dir));

        block0.setColor(color1);
        block1.setColor(color0);

        block0.SetRainbow(rainbow1);
        block1.SetRainbow(rainbow0);

        block0.transform.localScale = scale1;
        block1.transform.localScale = scale0;

        block0.vanish_timer = vanish_timer1;
        block1.vanish_timer = vanish_timer0;

        block0.beginSlide(offset0);
        block1.beginSlide(offset1);
    }

    // 두 블록이 같은 색으로 연결 가능한지 확인한다.
    // 무지개 블록은 어떤 색과도 연결되는 와일드카드로 취급한다.
    private bool IsSameColorOrRainbow(BlockControl a, BlockControl b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        if (a.is_rainbow || b.is_rainbow)
        {
            return true;
        }

        return a.color == b.color;
    }

    // 인수로 받은 블록이 세 개의 블록 안에 들어가는 지 파악하는 메서드
    public bool checkConnection(BlockControl start)
    {
        // 무지개 블록은 다른 색을 이어주는 역할만 한다.
        // 무지개 블록 자체를 기준색으로 검사하면 서로 다른 색까지 한 줄로 이어질 수 있으므로 제외한다.
        if (start.is_rainbow)
        {
            return false;
        }

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

            if (!IsSameColorOrRainbow(next_block, start)) { break; }
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

            if (!IsSameColorOrRainbow(next_block, start)) { break; }
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

            if (!IsSameColorOrRainbow(next_block, start)) { break; }
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

            if (!IsSameColorOrRainbow(next_block, start)) { break; }
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

        bool rainbow0 = block0.is_rainbow;
        bool rainbow1 = block1.is_rainbow;

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

        block0.SetRainbow(rainbow1);
        block1.SetRainbow(rainbow0);

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

    // 조커 효과: 현재 보드 위의 일반 블록 하나를 무지개 블록으로 만든다.
    public void SpawnRainbowBlock()
    {
        if (this.blocks == null)
        {
            return;
        }

        List<BlockControl> candidates = new List<BlockControl>();

        foreach (BlockControl block in this.blocks)
        {
            if (block == null) continue;
            if (!block.isIdle()) continue;
            if (block.isVacant()) continue;
            if (block.isVanishing()) continue;
            if (block.is_rainbow) continue;

            candidates.Add(block);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("[BlockRoot] 무지개 블록으로 만들 수 있는 블록이 없습니다.");
            return;
        }

        BlockControl target = candidates[Random.Range(0, candidates.Count)];
        target.SetRainbow(true);

        Debug.Log("[BlockRoot] 무지개 블록 생성: (" + target.i_pos.x + ", " + target.i_pos.y + ")");
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

    public void SetEqualProbabilitiesKeepZeros()
    {
        LevelData level_data = this.level_control.getCurrentLevelData();
        int nonZeroCount = 0;

        for (int i = 0; i < level_data.probability.Length; i++)
        {
            if (level_data.probability[i] > 0.0f)
            {
                nonZeroCount++;
            }
        }

        if (nonZeroCount == 0)
        {
            Debug.LogWarning("No non-zero probabilities to distribute.");
            return;
        }

        float equalProb = 1.0f / nonZeroCount;

        for (int i = 0; i < level_data.probability.Length; i++)
        {
            if (level_data.probability[i] > 0.0f)
            {
                level_data.probability[i] = equalProb;
            }
        }

        level_data.normalize();

        Debug.Log("Non-zero probabilities set equally: " + string.Join(", ", level_data.probability));
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

    //public void SetBlockScore(Block.COLOR color, int score)
    //{
    //    this.score_counter.block_scores[(int)color] = score;
    //}

    public void RemoveBlocksByColor(Block.COLOR color)
    {
        bool has_match = false;

        // 1. 지정된 색상의 블록을 연소(Vanishing) 상태로 만듭니다.
        foreach (BlockControl block in this.blocks)
        {
            // 조건 강화: VACANT, VANISHING 상태도 엄격히 검사
            if (block.color == color && !block.isVacant() && !block.isVanishing() && block.isIdle())
            {
                block.toVanishing();
                has_match = true;
                Debug.Log($"[RemoveBlocksByColor] Block at ({block.i_pos.x}, {block.i_pos.y}) marked for removal. Color: {color}");
            }
        }

        Debug.Log($"[RemoveBlocksByColor] Total blocks to remove: {CountVanishingBlocksByColor(color)}. Has match: {has_match}");

        if (has_match)
        {
            // 연속 점화(콤보)가 아니라면 리셋
            if (!this.is_vanishing_prev)
            {
                this.score_counter.clearIgniteCount();
            }

            // 연소 중인 블록의 색상 및 위치 수집
            int[] vanishingblockcolors = GetVanishinBlockColor();
            HashSet<Vector2Int> vanishingblockpositions_set = GetVanishingBlockPosition();

            // 디버프(네거티브 블록) 감점 로직 적용
            if (this.negative_block_positions != null)
            {
                foreach (Vector2Int temp_set in vanishingblockpositions_set)
                {
                    if (this.negative_block_positions.Contains(temp_set))
                    {
                        Block.COLOR temp_color = this.blocks[temp_set.x, temp_set.y].color;
                        vanishingblockcolors[(int)temp_color]--;
                    }
                }
            }

            // 아이템 사용을 1회의 매치로 취급
            this.score_counter.addIgniteCount2(1, vanishingblockcolors);
            this.score_counter.updateTotalScore();

            // 타는 중인 모든 블록의 연소 타이머 재시작
            foreach (BlockControl block in this.blocks)
            {
                if (block.isVanishing())
                {
                    block.rewindVanishTimer();
                }
            }

            // 연소가 끝날 때까지 블록 낙하 로직이 실행되지 않도록 강제 플래그 설정
            this.is_vanishing_prev = true;

            Debug.Log($"[RemoveBlocksByColor] Item effect applied successfully for color: {color}");
        }
        else
        {
            Debug.LogWarning($"[RemoveBlocksByColor] No blocks found to remove for color: {color}");
        }
    }

    // 색상별 연소 중인 블록 개수 카운트 (디버깅용)
    private int CountVanishingBlocksByColor(Block.COLOR color)
    {
        int count = 0;

        foreach (BlockControl block in this.blocks)
        {
            if (block.color == color && block.isVanishing())
            {
                count++;
            }
        }

        return count;
    }

    public void SetProbabilityKeepZeros(Block.COLOR targetColor, float targetProbability)
    {
        // 목표 확률을 0.0 ~ 1.0 사이로 제한
        targetProbability = Mathf.Clamp(targetProbability, 0.0f, 1.0f);
        LevelData level_data = this.level_control.getCurrentLevelData();

        int nonZeroCount = 0;
        float sumOthers = 0.0f;

        // 1. 타겟 색상이 아니며, 현재 확률이 0이 아닌 블록들의 개수와 합을 파악
        for (int i = 0; i < level_data.probability.Length; i++)
        {
            if (i != (int)targetColor && level_data.probability[i] > 0.0f)
            {
                nonZeroCount++;
                sumOthers += level_data.probability[i];
            }
        }

        // 2. 다른 출현 가능한 블록이 없거나, 타겟 확률을 1.0(100%)으로 설정한 경우
        if (targetProbability >= 1.0f || nonZeroCount == 0)
        {
            for (int i = 0; i < level_data.probability.Length; i++)
            {
                level_data.probability[i] = (i == (int)targetColor) ? 1.0f : 0.0f;
            }

            level_data.normalize();
            return;
        }

        // 3. 기존에 출현하던 블록들(0이 아닌 블록)에게만 남은 확률을 기존 비율대로 분배
        float remainingProbability = 1.0f - targetProbability;

        for (int i = 0; i < level_data.probability.Length; i++)
        {
            if (i == (int)targetColor)
            {
                // 타겟 색상은 요청한 확률로 고정
                level_data.probability[i] = targetProbability;
            }
            else if (level_data.probability[i] > 0.0f)
            {
                // 출현 중인 블록은 기존 비율(ratio)을 반영하여 나머지 확률을 나누어 가짐
                float ratio = level_data.probability[i] / sumOthers;
                level_data.probability[i] = remainingProbability * ratio;
            }
            else
            {
                // 출현 확률이 0이었던 블록은 확실하게 0으로 유지
                level_data.probability[i] = 0.0f;
            }
        }

        // 최종적으로 합계가 1.0이 되도록 정규화 진행
        level_data.normalize();

        Debug.Log($"Target {targetColor} set to {targetProbability} (Zeros kept). Current Probabilities: " + string.Join(", ", level_data.probability));
    }

    public void IncreaseBlockProbability(Block.COLOR targetColor, float increaseAmount)
    {
        LevelData level_data = this.level_control.getCurrentLevelData();

        // 지정된 블록의 현재 확률을 가져옵니다.
        float currentProbability = level_data.probability[(int)targetColor];

        // 현재 확률에 요청받은 증가치를 더하고, 0.0 ~ 1.0 사이로 제한합니다.
        float targetProbability = Mathf.Clamp(currentProbability + increaseAmount, 0.0f, 1.0f);

        // 0을 보존하는 확률 설정 함수를 호출하여 안전하게 확률을 재분배합니다.
        this.SetProbabilityKeepZeros(targetColor, targetProbability);

        Debug.Log("Increased " + targetColor.ToString() + " by " + increaseAmount.ToString() + ". Target probability: " + targetProbability.ToString());
    }

    public void DecreaseBlockProbability(Block.COLOR targetColor, float decreaseAmount)
    {
        LevelData level_data = this.level_control.getCurrentLevelData();

        // 지정된 블록의 현재 확률을 가져옵니다.
        float currentProbability = level_data.probability[(int)targetColor];

        // 현재 확률에서 요청받은 감소치를 빼고, 0.0 ~ 1.0 사이로 제한합니다.
        float targetProbability = Mathf.Clamp(currentProbability - decreaseAmount, 0.0f, 1.0f);

        // 0을 보존하는 확률 설정 함수를 호출하여 안전하게 확률을 재분배합니다.
        this.SetProbabilityKeepZeros(targetColor, targetProbability);

        Debug.Log("Decreased " + targetColor.ToString() + " by " + decreaseAmount.ToString() + ". Target probability: " + targetProbability.ToString());
    }

    public void MultiplyBlockProbability(Block.COLOR targetColor, float multiplier)
    {
        LevelData level_data = this.level_control.getCurrentLevelData();

        // 지정된 블록의 현재 확률을 가져옵니다.
        float currentProbability = level_data.probability[(int)targetColor];

        // 현재 확률에 요청받은 배율을 곱하고, 0.0 ~ 1.0 사이로 제한합니다.
        float targetProbability = Mathf.Clamp(currentProbability * multiplier, 0.0f, 1.0f);

        // 0을 보존하는 확률 설정 함수를 호출하여 안전하게 확률을 재분배합니다.
        this.SetProbabilityKeepZeros(targetColor, targetProbability);

        Debug.Log("Multiplied " + targetColor.ToString() + " by " + multiplier.ToString() + ". Target probability: " + targetProbability.ToString());
    }
}