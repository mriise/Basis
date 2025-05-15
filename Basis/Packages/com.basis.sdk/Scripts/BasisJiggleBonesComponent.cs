using Basis.Scripts.BasisSdk;
using UnityEngine;

public class BasisJiggleBonesComponent : MonoBehaviour
{
    [HeaderAttribute("this component can only exist next to the BasisAvatar Script and one time")]
    [SerializeField]
    public BasisJiggleStrain[] JiggleStrains;
}
