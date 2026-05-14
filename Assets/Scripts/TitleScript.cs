using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScript : MonoBehaviour
{
    public string creatorName = ""; // 제작자 이름은 Inspector에서 입력하거나 아래 문자열에 직접 입력

    private GUIStyle title_style;
    private GUIStyle label_style;
    private GUIStyle small_style;
    private GUIStyle box_style;

    private bool gui_style_initialized = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SceneManager.LoadScene("GameScene");
        }
    }

    void OnGUI()
    {
        this.createGUIStyle();

        // 배경 반투명 박스
        GUI.color = new Color(0.0f, 0.0f, 0.0f, 0.65f);
        GUI.Box(
            new Rect(Screen.width / 2.0f - 520.0f, 90.0f, 1040.0f, 760.0f),
            "",
            this.box_style
        );
        GUI.color = Color.white;

        // 제목
        GUI.Label(
            new Rect(Screen.width / 2.0f - 300.0f, 120.0f, 600.0f, 90.0f),
            "'Block'",
            this.title_style
        );

        float x = Screen.width / 2.0f - 440.0f;
        float y = 300.0f;
        float w = 880.0f;

        GUI.Label(
            new Rect(x, y, w, 170.0f),
            "목적 : 같은 색상의 블럭을 n개 (기본 3개) 이상 가로 또는 세로로 맞추어 " +
            "조커와 사용 아이템을 활용해 디버프를 뚫고 보스 스테이지 포함 3개의 스테이지를 클리어하는 것.",
            this.label_style
        );

        y += 150.0f;

        GUI.Label(
            new Rect(x, y, w, 50.0f),
            "조작법 : Click & Drag",
            this.label_style
        );

        y += 70.0f;

        GUI.Label(
            new Rect(x, y, w, 50.0f),
            "해상도 : 1920 x 1080",
            this.label_style
        );

        y += 70.0f;

        GUI.Label(
            new Rect(x, y, w, 50.0f),
            "제작자 : C077030 정효원, C177028 장승주" + this.creatorName,
            this.label_style
        );

        GUI.Label(
            new Rect(Screen.width / 2.0f - 300.0f, 780.0f, 600.0f, 50.0f),
            "화면을 클릭하면 게임이 시작됩니다.",
            this.small_style
        );
    }

    private void createGUIStyle()
    {
        if (this.gui_style_initialized)
        {
            return;
        }

        this.title_style = new GUIStyle();
        this.title_style.fontSize = 72;
        this.title_style.normal.textColor = Color.white;
        this.title_style.alignment = TextAnchor.MiddleCenter;
        this.title_style.fontStyle = FontStyle.Bold;

        this.label_style = new GUIStyle();
        this.label_style.fontSize = 30;
        this.label_style.normal.textColor = Color.white;
        this.label_style.wordWrap = true;
        this.label_style.alignment = TextAnchor.UpperLeft;

        this.small_style = new GUIStyle();
        this.small_style.fontSize = 28;
        this.small_style.normal.textColor = Color.yellow;
        this.small_style.alignment = TextAnchor.MiddleCenter;

        this.box_style = new GUIStyle(GUI.skin.box);

        this.gui_style_initialized = true;
    }
}