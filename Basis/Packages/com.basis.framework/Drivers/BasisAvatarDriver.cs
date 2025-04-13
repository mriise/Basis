using Basis.Scripts.BasisSdk;
using Basis.Scripts.BasisSdk.Helpers;
using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Common;
using Basis.Scripts.Device_Management;
using Basis.Scripts.TransformBinders.BoneControl;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
namespace Basis.Scripts.Drivers
{
    public abstract class BasisAvatarDriver : MonoBehaviour
    {
        public float ActiveAvatarEyeHeight()
        {
            if (BasisLocalPlayer.Instance.BasisAvatar != null)
            {
                return BasisLocalPlayer.Instance.BasisAvatar.AvatarEyePosition.x;
            }
            else
            {
                return BasisLocalPlayer.FallbackSize;
            }
        }
        private static string TPose = "Assets/Animator/Animated TPose.controller";
        public static string BoneData = "Assets/ScriptableObjects/BoneData.asset";
        public Action CalibrationComplete;
        public Action TposeStateChange;
        public BasisTransformMapping References = new BasisTransformMapping();
        public RuntimeAnimatorController SavedruntimeAnimatorController;
        public SkinnedMeshRenderer[] SkinnedMeshRenderer;
        public BasisPlayer Player;
        public bool CurrentlyTposing = false;
        public bool HasEvents = false;
        public List<int> ActiveMatrixOverrides = new List<int>();
        public void TryActiveMatrixOverride(int InstanceID)
        {
            if (ActiveMatrixOverrides.Contains(InstanceID) == false)
            {
                ActiveMatrixOverrides.Add(InstanceID);
                SetAllMatrixRecalculation(true);
            }
        }
        public void RemoveActiveMatrixOverride(int InstanceID)
        {
            if (ActiveMatrixOverrides.Remove(InstanceID))
            {
                if (ActiveMatrixOverrides.Count == 0)
                {
                    SetAllMatrixRecalculation(false);
                }
            }
        }
        public void SetMatrixOverride()
        {
#if UNITY_EDITOR
            SetAllMatrixRecalculation(true);
#else
            if (ActiveMatrixOverrides.Count != 0)
            {
                SetAllMatrixRecalculation(true);
            }
            else
            {
                SetAllMatrixRecalculation(false);
           }
#endif
        }
        public void Calibration(BasisAvatar Avatar)
        {
            FindSkinnedMeshRenders();
            BasisTransformMapping.AutoDetectReferences(Player.BasisAvatar.Animator, Avatar.transform, out References);
            Player.FaceIsVisible = false;
            if (Avatar == null)
            {
                BasisDebug.LogError("Missing Avatar");
            }
            if (Avatar.FaceVisemeMesh == null)
            {
                BasisDebug.Log("Missing Face for " + Player.DisplayName, BasisDebug.LogTag.Avatar);
            }
            Player.UpdateFaceVisibility(Avatar.FaceVisemeMesh.isVisible);
            if (Player.FaceRenderer != null)
            {
                GameObject.Destroy(Player.FaceRenderer);
            }
            Player.FaceRenderer = BasisHelpers.GetOrAddComponent<BasisMeshRendererCheck>(Avatar.FaceVisemeMesh.gameObject);
            Player.FaceRenderer.Check += Player.UpdateFaceVisibility;

            if (BasisFacialBlinkDriver.MeetsRequirements(Avatar))
            {
                BasisFacialBlinkDriver FacialBlinkDriver = BasisHelpers.GetOrAddComponent<BasisFacialBlinkDriver>(Player.gameObject);
                FacialBlinkDriver.Initialize(Player,Avatar);
            }
        }
        public void PutAvatarIntoTPose()
        {
            BasisDebug.Log("PutAvatarIntoTPose", BasisDebug.LogTag.Avatar);
            CurrentlyTposing = true;
            if (SavedruntimeAnimatorController == null)
            {
                SavedruntimeAnimatorController = Player.BasisAvatar.Animator.runtimeAnimatorController;
            }
            UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<RuntimeAnimatorController> op = Addressables.LoadAssetAsync<RuntimeAnimatorController>(TPose);
            RuntimeAnimatorController RAC = op.WaitForCompletion();
            Player.BasisAvatar.Animator.runtimeAnimatorController = RAC;
            ForceUpdateAnimator(Player.BasisAvatar.Animator);
            BasisDeviceManagement.UnassignFBTrackers();
            TposeStateChange?.Invoke();
        }
        public void ResetAvatarAnimator()
        {
            BasisDebug.Log("ResetAvatarAnimator", BasisDebug.LogTag.Avatar);
            Player.BasisAvatar.Animator.runtimeAnimatorController = SavedruntimeAnimatorController;
            SavedruntimeAnimatorController = null;
            CurrentlyTposing = false;
            TposeStateChange?.Invoke();
        }
        public Bounds GetBounds(Transform animatorParent)
        {
            // Get all renderers in the parent GameObject
            Renderer[] renderers = animatorParent.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, new Vector3(0.3f, BasisLocalPlayer.FallbackSize, 0.3f));
            }
            Bounds bounds = renderers[0].bounds;
            for (int Index = 1; Index < renderers.Length; Index++)
            {
                bounds.Encapsulate(renderers[Index].bounds);
            }
            return bounds;
        }
        public static bool TryConvertToBoneTrackingRole(HumanBodyBones body, out BasisBoneTrackedRole result)
        {
            result = BasisBoneTrackedRole.Chest; // Set a default value or handle it based on your requirements

            if (Enum.TryParse(body.ToString(), out BasisBoneTrackedRole parsedRole))
            {
                result = parsedRole;
                return true; // Successfully parsed
            }

            return false; // Failed to parse
        }
        public static bool TryConvertToHumanoidRole(BasisBoneTrackedRole body, out HumanBodyBones result)
        {
            result = HumanBodyBones.Hips; // Set a default value or handle it based on your requirements

            if (Enum.TryParse(body.ToString(), out HumanBodyBones parsedRole))
            {
                result = parsedRole;
                return true; // Successfully parsed
            }

            return false; // Failed to parse
        }
        public static bool IsApartOfSpineVertical(BasisBoneTrackedRole Role)
        {
            if (Role == BasisBoneTrackedRole.Hips ||
                Role == BasisBoneTrackedRole.Chest ||
                Role == BasisBoneTrackedRole.Hips ||
                Role == BasisBoneTrackedRole.Spine ||
                Role == BasisBoneTrackedRole.CenterEye ||
                Role == BasisBoneTrackedRole.Mouth ||
                Role == BasisBoneTrackedRole.Head)
            {
                return true;
            }
            return false;
        }
        public void CalculateTransformPositions(Animator anim, BaseBoneDriver driver)
        {
            BasisDebug.Log("CalculateTransformPositions", BasisDebug.LogTag.Avatar);
            UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<BasisFallBackBoneData> BasisFallBackBoneDataAsync = Addressables.LoadAssetAsync<BasisFallBackBoneData>(BoneData);
            BasisFallBackBoneData FBBD = BasisFallBackBoneDataAsync.WaitForCompletion();
            for (int Index = 0; Index < driver.ControlsLength; Index++)
            {
                BasisBoneControl Control = driver.Controls[Index];
                if (driver.trackedRoles[Index] == BasisBoneTrackedRole.CenterEye)
                {
                    GetWorldSpaceRotAndPos(() => Player.BasisAvatar.AvatarEyePosition, out quaternion Rotation, out float3 TposeWorld);
                    SetInitialData(anim, Control, driver.trackedRoles[Index], TposeWorld);
                }
                else
                {
                    if (driver.trackedRoles[Index] == BasisBoneTrackedRole.Mouth)
                    {
                        GetWorldSpaceRotAndPos(() => Player.BasisAvatar.AvatarMouthPosition, out quaternion Rotation, out float3 TposeWorld);
                        SetInitialData(anim, Control, driver.trackedRoles[Index], TposeWorld);
                    }
                    else
                    {
                        if (FBBD.FindBone(out BasisFallBone FallBackBone, driver.trackedRoles[Index]))
                        {
                            if (TryConvertToHumanoidRole(driver.trackedRoles[Index], out HumanBodyBones HumanBones))
                            {
                                GetBoneRotAndPos(driver, anim, HumanBones, FallBackBone.PositionPercentage, out quaternion Rotation, out float3 TposeWorld, out bool UsedFallback);
                                SetInitialData(anim, Control, driver.trackedRoles[Index], TposeWorld);
                            }
                            else
                            {
                                BasisDebug.LogError("cant Convert to humanbodybone " + driver.trackedRoles[Index]);
                            }
                        }
                        else
                        {
                            BasisDebug.LogError("cant find Fallback Bone for " + driver.trackedRoles[Index]);
                        }
                    }
                }
            }
            Addressables.Release(BasisFallBackBoneDataAsync);
        }
        public void GetBoneRotAndPos(BaseBoneDriver driver, Animator anim, HumanBodyBones bone, Vector3 heightPercentage, out quaternion Rotation, out float3 Position, out bool UsedFallback)
        {
            if (anim.avatar != null && anim.avatar.isHuman)
            {
                Transform boneTransform = anim.GetBoneTransform(bone);
                if (boneTransform == null)
                {
                    Rotation = driver.transform.rotation;
                    if (BasisHelpers.TryGetFloor(anim, out Position))
                    {

                    }
                    // Position = new Vector3(0, Position.y, 0);
                    Position += CalculateFallbackOffset(bone, ActiveAvatarEyeHeight(), heightPercentage);
                    //Position = new Vector3(0, Position.y, 0);
                    UsedFallback = true;
                }
                else
                {
                    UsedFallback = false;
                    boneTransform.GetPositionAndRotation(out Vector3 VPosition, out Quaternion QRotation);
                    Position = VPosition;
                    Rotation = QRotation;
                }
            }
            else
            {
                Rotation = driver.transform.rotation;
                if (BasisHelpers.TryGetFloor(anim, out Position))
                {

                }
                Position = new Vector3(0, Position.y, 0);
                Position += CalculateFallbackOffset(bone, ActiveAvatarEyeHeight(), heightPercentage);
                Position = new Vector3(0, Position.y, 0);
                UsedFallback = true;
            }
        }
        public float3 CalculateFallbackOffset(HumanBodyBones bone, float fallbackHeight, float3 heightPercentage)
        {
            Vector3 height = fallbackHeight * heightPercentage;
            return bone == HumanBodyBones.Hips ? Multiply(height, -Vector3.up) : Multiply(height, Vector3.up);
        }
        public static Vector3 Multiply(Vector3 value, Vector3 scale)
        {
            return new Vector3(value.x * scale.x, value.y * scale.y, value.z * scale.z);
        }
        public void GetWorldSpaceRotAndPos(Func<Vector2> positionSelector, out quaternion rotation, out float3 position)
        {
            rotation = Quaternion.identity;
            position = Vector3.zero;
            if (BasisHelpers.TryGetFloor(Player.BasisAvatar.Animator, out float3 bottom))
            {
                Vector3 convertedToVector3 = BasisHelpers.AvatarPositionConversion(positionSelector());
                position = BasisHelpers.ConvertFromLocalSpace(convertedToVector3, bottom);
            }
            else
            {
                BasisDebug.LogError("Missing bottom");
            }
        }
        public void ForceUpdateAnimator(Animator Anim)
        {
            // Specify the time you want the Animator to update to (in seconds)
            float desiredTime = Time.deltaTime;

            // Call the Update method to force the Animator to update to the desired time
            Anim.Update(desiredTime);
        }
        public bool IsNull(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                BasisDebug.LogError("Missing Object during calibration");
                return true;
            }
            else
            {
                return false;
            }
        }
        public void SetInitialData(Animator animator, BasisBoneControl bone, BasisBoneTrackedRole Role,Vector3 WorldTpose)
        {
            bone.OutGoingData.position = BasisLocalBoneDriver.ConvertToAvatarSpaceInitial(animator, WorldTpose);//out Vector3 WorldSpaceFloor
            bone.TposeLocal.position = bone.OutGoingData.position;
            bone.TposeLocal.rotation = bone.OutGoingData.rotation;
            if (IsApartOfSpineVertical(Role))
            {
                bone.OutGoingData.position = new Vector3(0, bone.OutGoingData.position.y, bone.OutGoingData.position.z);
                bone.TposeLocal.position = bone.OutGoingData.position;
            }
            if (Role == BasisBoneTrackedRole.Hips)
            {
                bone.TposeLocal.rotation = quaternion.identity;
            }
        }
        public void SetAndCreateLock(BaseBoneDriver BaseBoneDriver, BasisBoneTrackedRole LockToBoneRole, BasisBoneTrackedRole AssignedTo, float PositionLerpAmount, float QuaternionLerpAmount, bool CreateLocks = true)
        {
            if (CreateLocks)
            {

                if (BaseBoneDriver.FindBone(out BasisBoneControl AssignedToAddToBone, AssignedTo) == false)
                {
                    BasisDebug.LogError("Cant Find Bone " + AssignedTo);
                }
                if (BaseBoneDriver.FindBone(out BasisBoneControl LockToBone, LockToBoneRole) == false)
                {
                    BasisDebug.LogError("Cant Find Bone " + LockToBoneRole);
                }
                BaseBoneDriver.CreateRotationalLock(AssignedToAddToBone, LockToBone, PositionLerpAmount, QuaternionLerpAmount);
            }
        }
        public int SkinnedMeshRendererLength;
        public void FindSkinnedMeshRenders()
        {
            SkinnedMeshRenderer = Player.BasisAvatar.Animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            SkinnedMeshRendererLength = SkinnedMeshRenderer.Length;
        }
        public void SetAllMatrixRecalculation(bool State)
        {
            for (int Index = 0; Index < SkinnedMeshRendererLength; Index++)
            {
                SkinnedMeshRenderer Render = SkinnedMeshRenderer[Index];
                Render.forceMatrixRecalculationPerRender = State;
            }
            BasisDebug.Log("Matrix ReCalculation State set to " + State);
        }
        public void updateWhenOffscreen(bool State)
        {
            for (int Index = 0; Index < SkinnedMeshRendererLength; Index++)
            {
                SkinnedMeshRenderer Render = SkinnedMeshRenderer[Index];
                Render.updateWhenOffscreen = State;
            }
        }
    }
}
