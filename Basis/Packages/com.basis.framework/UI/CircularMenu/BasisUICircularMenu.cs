using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class BasisUICircularMenu : MonoBehaviour
{
    public RectTransform MiddlePoint;
    [Header("Settings")]
    public float radius = 100f;
    public bool clockwise = true;
    public float startAngle = 0f;
    public Sprite DefaultIcon;

    [SerializeField]
    public BasisCircularMenuItem[] Items;
    public void OnEnable()
    {
        RebuildMenu();
    }
    public void RebuildMenu()
    {
        ArrangeItemsInCircle(Items);
    }
    void ArrangeItemsInCircle(BasisCircularMenuItem[] BasisCircularMenuItem)
    {
        int childCount = BasisCircularMenuItem.Length;
        for (int Index = 0; Index < childCount; Index++)
        {
            BasisCircularMenuItem Menu = BasisCircularMenuItem[Index];
            float angle = startAngle + (360f / childCount) * Index * (clockwise ? -1 : 1);
            float angleRad = angle * Mathf.Deg2Rad;
            Vector2 pos = new Vector2(Mathf.Cos(angleRad) * radius,Mathf.Sin(angleRad) * radius);
            Menu.Parent.anchoredPosition = pos;
            Apply(Menu, Menu.Description);
        }
    }
    public void Apply(BasisCircularMenuItem MenuItem, BasisUGCMenuDescription Description)
    {
        MenuItem.Text.text = Description.MenuName;
        MenuItem.Icon.sprite = Description.Sprite != null ? Description.Sprite : DefaultIcon;
    }
}
