using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class BasisCircularMenuItem : MonoBehaviour
{
    public RectTransform Parent;
    public Image Icon;
    public TextMeshProUGUI Text;
    public BasisUGCMenuDescription Description = new BasisUGCMenuDescription();
}
