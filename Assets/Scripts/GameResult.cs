using UnityEngine;
using UnityEngine.SceneManagement;

public class GameResult : MonoBehaviour
{
    private GUIStyle title_style;
    private GUIStyle text_style;
    private GUIStyle box_style;

    private bool gui_style_initialized = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SceneManager.LoadScene("TitleScene");
        }
    }

    void OnGUI()
    {
        this.createGUIStyle();

        string scene_name = SceneManager.GetActiveScene().name;
        string result_text = "";

        if (scene_name == "GameClearScene")
        {
            result_text = "게임 클리어!";
        }
        else if (scene_name == "GameOverScene")
        {
            result_text = "게임 오버";
        }
        else
        {
            result_text = "결과 화면";
        }

        this.drawResult(result_text);
    }

    private void drawResult(string result_text)
    {
        GUI.color = new Color(0.0f, 0.0f, 0.0f, 0.70f);
        GUI.Box(
            new Rect(
                Screen.width / 2.0f - 500.0f,
                Screen.height / 2.0f - 220.0f,
                1000.0f,
                440.0f
            ),
            "",
            this.box_style
        );
        GUI.color = Color.white;

        GUI.Label(
            new Rect(
                Screen.width / 2.0f - 350.0f,
                Screen.height / 2.0f - 130.0f,
                700.0f,
                90.0f
            ),
            result_text,
            this.title_style
        );

        GUI.Label(
            new Rect(
                Screen.width / 2.0f - 400.0f,
                Screen.height / 2.0f + 40.0f,
                800.0f,
                60.0f
            ),
            "화면을 클릭하면 타이틀로 돌아갑니다.",
            this.text_style
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

        this.text_style = new GUIStyle();
        this.text_style.fontSize = 30;
        this.text_style.normal.textColor = Color.yellow;
        this.text_style.alignment = TextAnchor.MiddleCenter;

        this.box_style = new GUIStyle(GUI.skin.box);

        this.gui_style_initialized = true;
    }
}