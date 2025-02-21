using System.Collections.Generic;

namespace Basis.Scripts.UI
{
    /// </summary>
    sealed class RaycastHitComparer : IComparer<RaycastHitData>
    {
        public int Compare(RaycastHitData a, RaycastHitData b)
            => b.graphic.depth.CompareTo(a.graphic.depth);
    }
}
