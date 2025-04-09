using Basis.Scripts.BasisSdk;
using Basis.Scripts.BasisSdk.Helpers;
using Basis.Scripts.BasisSdk.Players;
using JigglePhysics;
using System.Collections.Generic;
using UnityEngine;
using static JigglePhysics.JiggleRigBuilder;
namespace Basis.Scripts.Drivers
{
    public class BasisAvatarStrainJiggleDriver : MonoBehaviour
    {
        public BasisPlayer player;
        public JiggleRigBuilder Jiggler;
        public JiggleRigRendererLOD JiggleRigRendererLOD;
        public bool OnCalibration()
        {
            if (Jiggler != null)
            {
                GameObject.Destroy(Jiggler);
            }
            if (player.BasisAvatar != null)
            {
                if (player.BasisAvatar.JiggleStrains != null && player.BasisAvatar.JiggleStrains.Length != 0)
                {
                    int Count = player.BasisAvatar.JiggleStrains.Length;
                    JiggleRigRendererLOD = BasisHelpers.GetOrAddComponent<JiggleRigRendererLOD>(player.BasisAvatar.Animator.gameObject);
                    JiggleRigRendererLOD.CameraPosition = BasisLocalCameraDriver.Position;
                    JiggleRigRendererLOD.Simulate();
                    JiggleRigRendererLOD.TargetPoint = player.Mouth.OutgoingWorldData.position;
                    JiggleRigRendererLOD.SetRenderers(player.BasisAvatar.Renders);
                    Jiggler = player.BasisAvatar.Animator.gameObject.AddComponent<JiggleRigBuilder>();
                    List<JiggleRig> Jiggles = new List<JiggleRig>();
                    for (int StrainIndex = 0; StrainIndex < Count; StrainIndex++)
                    {
                        BasisJiggleStrain Strain = player.BasisAvatar.JiggleStrains[StrainIndex];
                        if (Strain.RootTransform != null)
                        {
                            JiggleRig Rig = Conversion(Strain);
                            Jiggles.Add(Rig);
                        }
                        else
                        {
                            BasisDebug.LogError("Missing Root Transform of Jiggle Strain!");
                        }
                    }
                    Jiggler.jiggleRigs = Jiggles;
                    Jiggler.Initialize();
                    return true;
                }
            }
            return false;
        }
        public void Simulate()
        {
            JiggleRigRendererLOD.TargetPoint = player.Mouth.OutgoingWorldData.position;
            JiggleRigRendererLOD.Simulate();
        }
        public void LocalSimulate()
        {
            JiggleRigRendererLOD.CameraPosition = BasisLocalCameraDriver.Position;
        }
        public void PrepareTeleport()
        {
            if (Jiggler != null)
            {
                Jiggler.PrepareTeleport();
            }
        }
        public void FinishTeleport()
        {
            if (Jiggler != null)
            {
                Jiggler.FinishTeleport();
            }
        }
        public void SetWind(Vector3 Wind)
        {
            Jiggler.wind = Wind;
        }
        public JiggleRig Conversion(BasisJiggleStrain Strain)
        {
            JiggleSettings Base = new JiggleSettings();
            JiggleSettingsData Data = new JiggleSettingsData
            {
                gravityMultiplier = Strain.GravityMultiplier,
                friction = Strain.Friction,
                angleElasticity = Strain.AngleElasticity,
                blend = Strain.Blend,
                airDrag = Strain.AirDrag,
                lengthElasticity = Strain.LengthElasticity,
                elasticitySoften = Strain.ElasticitySoften,
                radiusMultiplier = Strain.RadiusMultiplier
            };
            Base.SetData(Data);
            JiggleRig JiggleRig = AssignUnComputedData(Strain.RootTransform, Base, Strain.IgnoredTransforms, Strain.Colliders);
            return JiggleRig;
        }
        public JiggleRig AssignUnComputedData(Transform rootTransform, JiggleSettingsBase jiggleSettings, Transform[] ignoredTransforms, Collider[] colliders)
        {
            JiggleRig JiggleRig = new JiggleRig(rootTransform, jiggleSettings, ignoredTransforms, colliders);

            return JiggleRig;
        }
    }
}
