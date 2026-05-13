using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemRoot : MonoBehaviour
{
    // 씬이 다시 로드되어도 선택한 사용 아이템이 유지되도록 static 사용
    private static string current_item_name = "";
    private static bool has_item = false;

    private ShopRoot shop_root = null;

    public GUIStyle item_button_style;
    public GUIStyle item_text_style;

    private bool gui_style_initialized = false;

    void Start()
    {
        this.shop_root = this.gameObject.GetComponent<ShopRoot>();
    }

    void OnGUI()
    {
        this.createGUIStyle();

        // 상점이 열려 있는 동안에는 인게임 아이템 버튼을 표시하지 않음
        if (this.shop_root != null && this.shop_root.IsOpen())
        {
            return;
        }

        this.drawItemButton();
    }

    private void createGUIStyle()
    {
        if (this.gui_style_initialized)
        {
            return;
        }

        // GUI.skin은 OnGUI 안에서만 접근해야 하므로 여기서 생성
        this.item_button_style = new GUIStyle(GUI.skin.button);
        this.item_button_style.fontSize = 24;
        this.item_button_style.alignment = TextAnchor.MiddleCenter;

        this.item_text_style = new GUIStyle();
        this.item_text_style.fontSize = 22;
        this.item_text_style.normal.textColor = Color.white;
        this.item_text_style.alignment = TextAnchor.MiddleCenter;

        this.gui_style_initialized = true;
    }

    private void drawItemButton()
    {
        float button_width = 220.0f;
        float button_height = 70.0f;

        // 화면 우측 중앙
        float x = Screen.width - button_width - 40.0f;
        float y = Screen.height / 2.0f - button_height / 2.0f;

        if (!has_item || current_item_name == "")
        {
            GUI.Label(
                new Rect(x, y - 40.0f, button_width, 30.0f),
                "사용 아이템 없음",
                this.item_text_style
            );

            return;
        }

        GUI.Label(
            new Rect(x, y - 45.0f, button_width, 35.0f),
            "사용 아이템",
            this.item_text_style
        );

        if (GUI.Button(
            new Rect(x, y, button_width, button_height),
            current_item_name,
            this.item_button_style))
        {
            this.useItem();
        }
    }

    private void useItem()
    {
        if (!has_item || current_item_name == "")
        {
            return;
        }

        Debug.Log("[ItemRoot] 사용 아이템 사용: " + current_item_name);

        // 아직 실제 효과는 구현하지 않았으므로 로그만 출력
        // 1회 사용 후 아이템 제거
        current_item_name = "";
        has_item = false;
    }

    public void SetItem(string item_name)
    {
        current_item_name = item_name;
        has_item = true;

        Debug.Log("[ItemRoot] 사용 아이템 획득: " + item_name);
    }

    public void ClearItem()
    {
        current_item_name = "";
        has_item = false;
    }

    public bool HasItem()
    {
        return has_item;
    }

    public string GetCurrentItemName()
    {
        return current_item_name;
    }
}