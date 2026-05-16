using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemRoot : MonoBehaviour
{
    private string current_item_name = "";
    private bool has_item = false;

    ShopRoot shop_root;

    void Start()
    {
        this.shop_root = this.gameObject.GetComponent<ShopRoot>();
        if(has_item) ClearItem();
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

    public void UseCurrentItem()
    {
        if (!has_item || current_item_name == "")
        {
            Debug.Log("[ItemRoot] 사용할 아이템이 없습니다.");
            return;
        }

        this.shop_root.ApplyItemEffect();

        Debug.Log("[ItemRoot] 사용 아이템 사용: " + current_item_name);

        current_item_name = "";
        has_item = false;
    }
}