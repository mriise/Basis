using UnityEngine;

namespace JigglePhysics
{
    public class JiggleRigSimpleLOD : JiggleRigLOD
    {

        [Tooltip("Distance to disable the jiggle rig.")]
        [SerializeField] float distance = 20f;
        [Tooltip("Distance past distance from which it blends out rather than instantly disabling.")]
        [SerializeField] float blend = 5f;

        public static Vector3 CameraPosition;
        public Vector3 TargetPoint;
        protected override bool CheckActive()
        {
            float cameraDistance = Vector3.Distance(CameraPosition, TargetPoint);
            float currentBlend = (cameraDistance - distance + blend) / blend;
            currentBlend = Mathf.Clamp01(1f - currentBlend);
            for (int Index = 0; Index < JiggleCount; Index++)
            {
                jiggles[Index].blend = currentBlend;
            }
            return cameraDistance < distance;
        }

    }
}
