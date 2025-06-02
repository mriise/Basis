using System.Collections.Generic;
using UnityEngine;
public class BasisUICircularMenu : MonoBehaviour
{
    public RectTransform MiddlePoint;
    public RectTransform Tophat;
    [Header("Settings")]
    public bool clockwise = true;
    public float startAngle = 0f;
    public Sprite DefaultIcon;

    [SerializeField]
    public List<BasisCircularMenuItem> Items;

    public BasisCircularMenuItem CopyFrom;
    public float YPointDistance = 20;
    public float XPointDistance = 20;

    public int CreateCount = 4;
    public void OnEnable()
    {
        RebuildMenu();
    }
    public void RebuildMenu()
    {
        BasisUGCMenuDescription[] Description = new BasisUGCMenuDescription[CreateCount];

        for (int Index = 0; Index < Description.Length; Index++)
        {
            BasisUGCMenuDescription description = Description[Index];
            description.MenuName = $"test  {Index}";
            Description[Index] = description;
        }
        ArrangeItemsInCircle(Description);
    }
    // The full circular image this is based on (e.g., 0 to 360 degrees)
    public float TotalDegrees = 360f;

    public void ArrangeItemsInCircle(BasisUGCMenuDescription[] BasisCircularMenuItem)
    {
        // Clear old items
        foreach (BasisCircularMenuItem item in Items)
        {
            if (item != null)
            {
                GameObject.Destroy(item.gameObject);
            }
        }
        Items.Clear();

        int childCount = BasisCircularMenuItem.Length;
        if (childCount == 0) return;

        float angleStep = TotalDegrees / childCount;

        for (int i = 0; i < childCount; i++)
        {
            // Calculate the angle in degrees
            float angle = startAngle + (clockwise ? -1 : 1) * i * angleStep;
            float angleRad = angle * Mathf.Deg2Rad;

            // Instantiate the UI item
            GameObject copy = Instantiate(CopyFrom.gameObject, MiddlePoint);
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;

            if (copy.TryGetComponent(out BasisCircularMenuItem menu))
            {
                // Rotate background fill to match the slice
                menu.Background.fillAmount = angleStep / 360f;
                menu.Background.rectTransform.localRotation = Quaternion.Euler(0, 0, -angle);

                // Apply description data
                Apply(menu, BasisCircularMenuItem[i]);

                // Position the Point on the circle
                if (menu.Point != null)
                {
                    RectTransform PointRect = menu.Point;

                    PointRect.localPosition = Quaternion.Euler(0, 0, angleStep / 360f) * new Vector3(-XPointDistance, -YPointDistance, 0f); // local Y
                    PointRect.localRotation = Quaternion.Euler(0, 0, angle); // Optional: rotate to face outward
                }

                Items.Add(menu);
            }

            copy.SetActive(true);
        }

        Tophat.SetAsLastSibling(); // Keep this on top
    }
    public void Apply(BasisCircularMenuItem MenuItem, BasisUGCMenuDescription Description)
    {
        MenuItem.Description = Description;
        MenuItem.Text.text = Description.MenuName;
        MenuItem.Icon.sprite = Description.Sprite != null ? Description.Sprite : DefaultIcon;
    }
}
