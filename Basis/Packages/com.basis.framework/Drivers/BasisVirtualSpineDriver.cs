using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.TransformBinders.BoneControl;
using Unity.Mathematics;
using UnityEngine;
[System.Serializable]
public class BasisVirtualSpineDriver
{
    [SerializeField] public BasisBoneControl CenterEye;
    [SerializeField] public BasisBoneControl Head;
    [SerializeField] public BasisBoneControl Neck;
    [SerializeField] public BasisBoneControl Chest;
    [SerializeField] public BasisBoneControl Spine;
    [SerializeField] public BasisBoneControl Hips;
    [SerializeField] public BasisBoneControl RightShoulder;
    [SerializeField] public BasisBoneControl LeftShoulder;
    [SerializeField] public BasisBoneControl LeftLowerArm;
    [SerializeField] public BasisBoneControl RightLowerArm;
    [SerializeField] public BasisBoneControl LeftLowerLeg;
    [SerializeField] public BasisBoneControl RightLowerLeg;
    [SerializeField] public BasisBoneControl LeftHand;
    [SerializeField] public BasisBoneControl RightHand;
    [SerializeField] public BasisBoneControl LeftFoot;
    [SerializeField] public BasisBoneControl RightFoot;
    public float NeckRotationSpeed = 12;
    public float ChestRotationSpeed = 25;
    public float SpineRotationSpeed = 30;
    public float HipsRotationSpeed = 40;
    public float MaxNeckAngle = 0; // Limit the neck's rotation range to avoid extreme twisting
    public float MaxChestAngle = 0; // Limit the chest's rotation range
    public float MaxHipsAngle = 0; // Limit the hips' rotation range
    public float MaxSpineAngle = 0;
    public void Initialize()
    {
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out CenterEye, BasisBoneTrackedRole.CenterEye))
        {
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out Head, BasisBoneTrackedRole.Head))
        {
            Head.HasVirtualOverride = true;
        }

        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out Neck, BasisBoneTrackedRole.Neck))
        {
            Neck.HasVirtualOverride = true;
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out Chest, BasisBoneTrackedRole.Chest))
        {
            Chest.HasVirtualOverride = true;
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out Spine, BasisBoneTrackedRole.Spine))
        {
            Spine.HasVirtualOverride = true;
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out Hips, BasisBoneTrackedRole.Hips))
        {
            Hips.HasVirtualOverride = true;
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out LeftLowerArm, BasisBoneTrackedRole.LeftLowerArm))
        {
         //  LeftLowerArm.HasVirtualOverride = true;
        }

        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out RightLowerArm, BasisBoneTrackedRole.RightLowerArm))
        {
         //  RightLowerArm.HasVirtualOverride = true;
        }

        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out LeftLowerLeg, BasisBoneTrackedRole.LeftLowerLeg))
        {
         //  LeftLowerLeg.HasVirtualOverride = true;
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out RightLowerLeg, BasisBoneTrackedRole.RightLowerLeg))
        {
            //RightLowerLeg.HasVirtualOverride = true;
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out LeftHand, BasisBoneTrackedRole.LeftHand))
        {
            // LeftHand.HasVirtualOverride = true;
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out RightHand, BasisBoneTrackedRole.RightHand))
        {
            //   RightHand.HasVirtualOverride = true;
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out LeftFoot, BasisBoneTrackedRole.LeftFoot))
        {
            // LeftHand.HasVirtualOverride = true;
        }
        if (BasisLocalPlayer.Instance.LocalBoneDriver.FindBone(out RightFoot, BasisBoneTrackedRole.RightFoot))
        {
            //   RightHand.HasVirtualOverride = true;
        }
        BasisLocalPlayer.Instance.OnPreSimulateBones += OnSimulateHead;//instead of virtual run just run at the start
    }
    public void CalibrateOffsets()
    {

        // Calculate head offset from eyes in eyes' local space
        headOffset = Quaternion.Inverse(CenterEye.TposeLocal.rotation) * (Head.TposeLocal.position - CenterEye.TposeLocal.position);

        // Calculate neck offset from head in head's local space
        neckOffset = Quaternion.Inverse(Head.TposeLocal.rotation) * (Neck.TposeLocal.position - Head.TposeLocal.position);

        neckEyeOffset = Quaternion.Inverse(CenterEye.TposeLocal.rotation) * (Neck.TposeLocal.position - CenterEye.TposeLocal.position);
        // Store original neck Y position in root local space
        originalNeckY = Neck.TposeLocal.position.y;
    }
    public void DeInitialize()
    {
        if (Neck != null)
        {
            Neck.HasVirtualOverride = false;
        }
        if (Chest != null)
        {
            Chest.HasVirtualOverride = false;
        }
        if (Hips != null)
        {
            Hips.HasVirtualOverride = false;
        }
        if (Spine != null)
        {
            Spine.HasVirtualOverride = false;
        }
        BasisLocalPlayer.Instance.OnPreSimulateBones -= OnSimulateHead;
    }
    public void OnSimulateHead()
    {
        CalibrateOffsets();
        float deltaTime = Time.deltaTime;
        Quaternion Rotation = Neck.OutGoingData.rotation;
        Vector3 Forward = Rotation * Vector3.forward;

        CalculateBones(Forward, CenterEye.OutGoingData.position, CenterEye.OutGoingData.rotation,
        out Vector3 headWorldPos, out Vector3 finalNeckWorldPos, out Quaternion neckYawRot);

        // Process rotations with reduced influence (progressive damping)
        ApplyClampedRotation(Chest, Neck, deltaTime * ChestRotationSpeed, -MaxChestAngle, MaxChestAngle);
        ApplyClampedRotation(Spine, Chest, deltaTime * SpineRotationSpeed, -MaxSpineAngle, MaxSpineAngle);
        ApplyClampedRotation(Hips, Spine, deltaTime * HipsRotationSpeed, -MaxHipsAngle, MaxHipsAngle);

        Head.OutGoingData.position = headWorldPos;
        Head.OutGoingData.rotation = CenterEye.OutGoingData.rotation;

        Neck.OutGoingData.position = finalNeckWorldPos;
        Neck.OutGoingData.rotation = neckYawRot;
        // Apply positional constraints (if any)
        ApplyPositionControl(Chest);
        ApplyPositionControl(Spine);
        ApplyPositionControl(Hips);
    }

    private void ApplyClampedRotation(BasisBoneControl targetBone, BasisBoneControl sourceBone, float lerpFactor, float minPitch, float maxPitch)
    {
        Quaternion slerped = Quaternion.Slerp(targetBone.OutGoingData.rotation, sourceBone.OutGoingData.rotation, lerpFactor);
        Vector3 euler = slerped.eulerAngles;

        // Ensure the pitch is properly clamped even if wrapping around 360°
        float pitch = NormalizeAngle(euler.x);
        float clampedPitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        targetBone.OutGoingData.rotation = Quaternion.Euler(clampedPitch, euler.y, 0);
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    private void ApplyPositionControl(BasisBoneControl boneControl)
    {
        if (!boneControl.HasTarget)
        {
            return;
        }

        quaternion targetRotation = boneControl.Target.OutGoingData.rotation;

        // Extract yaw-only forward vector
        float3 forward = math.mul(targetRotation, new float3(0, 0, 1));
        forward.y = 0;
        forward = math.normalize(forward);

        quaternion yawRotation = quaternion.LookRotationSafe(forward, new float3(0, 1, 0));
        float3 offset = math.mul(yawRotation, boneControl.Offset);

        boneControl.OutGoingData.position = boneControl.Target.OutGoingData.position + offset;
    }

    public Vector3 headOffset;          // Eye-to-head in eye local space
    public Vector3 neckOffset;          // Head-to-neck in head local space
    public Vector3 neckEyeOffset;
    public float originalNeckY;        // Original neck Y position in root local space
    public void CalculateBones(Vector3 Forward, Vector3 eyeWorldPos, Quaternion eyeWorldRot, out Vector3 HeadPosition, out Vector3 NeckPosition, out Quaternion neckYawRot)
    {
        HeadPosition = eyeWorldPos + eyeWorldRot * headOffset;
        // === NECK FOLLOW (world space, with locked Y) ===
        Vector3 rawNeckWorldPos = HeadPosition + eyeWorldRot * neckOffset;
        // Lock Y to the original neck Y in root's local space
        Vector3 rawNeckLocalToRoot = rawNeckWorldPos;
        rawNeckLocalToRoot.y = originalNeckY;
        NeckPosition = rawNeckLocalToRoot;
        NeckPosition.y = eyeWorldPos.y + neckEyeOffset.y;
        // Calculate yaw-only rotation from eye forward
        Vector3 flatForward = eyeWorldRot * Vector3.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.001f)
        {
            flatForward = Forward;//neckTransform.forward;
        }
        neckYawRot = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
    }
}
