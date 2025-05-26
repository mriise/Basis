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
    public BasisCircularMenuItem[] TestItems;

    [SerializeField]
    public BasisUGCMenuDescription[] Descriptors;
    public void OnEnable()
    {
        RebuildMenu();
    }
    public void RebuildMenu()
    {
        ArrangeItemsInCircle(TestItems, Descriptors);
    }
    void ArrangeItemsInCircle(BasisCircularMenuItem[] BasisCircularMenuItem, BasisUGCMenuDescription[] Descriptors)
    {
        int childCount = BasisCircularMenuItem.Length;
        for (int Index = 0; Index < childCount; Index++)
        {
            BasisCircularMenuItem Menu = BasisCircularMenuItem[Index];
            BasisUGCMenuDescription Description = Descriptors[Index];
            float angle = startAngle + (360f / childCount) * Index * (clockwise ? -1 : 1);
            float angleRad = angle * Mathf.Deg2Rad;
            Vector2 pos = new Vector2(Mathf.Cos(angleRad) * radius,Mathf.Sin(angleRad) * radius);
            Menu.Parent.anchoredPosition = pos;
            Apply(Menu, Description);
        }
    }
    public void Apply(BasisCircularMenuItem MenuItem, BasisUGCMenuDescription Description)
    {
        MenuItem.Text.text = Description.MenuName;
        MenuItem.Icon.sprite = Description.Sprite != null ? Description.Sprite : DefaultIcon;
    }
    [System.Serializable]
    public class BasisCircularMenuItem
    {
        public RectTransform Parent;
        public Image Icon;
        public TextMeshProUGUI Text;
    }
}
