using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.TransformBinders.BoneControl;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Basis.Scripts.Device_Management.Devices.UnityInputSystem
{
    [Serializable]
    public class BasisUnityInputManagement : BasisBaseTypeManagement
    {
        public List<InputDevice> inputDevices = new List<InputDevice>();
        public Dictionary<string, InputDevice> TypicalDevices = new Dictionary<string, InputDevice>();
        public bool HasEvents = false;

        private void OnEnable()
        {
            InputSystem.onDeviceChange += OnDeviceChanged;
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChanged;
        }

        private void OnDeviceChanged(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                    UpdateDeviceList();
                    break;
                case InputDeviceChange.Removed:
                    UpdateDeviceList();
                    break;
                case InputDeviceChange.ConfigurationChanged:
                    Debug.Log($"Device Configuration Changed: {device.name}");
                    break;
            }
        }

        private void UpdateDeviceList()
        {
            inputDevices = InputSystem.devices.ToList();

            foreach (var device in inputDevices)
            {
                if (device != null)
                {
                    string id = GenerateID(device);
                    if (id.Contains("OpenXR"))
                    {
                        if (!TypicalDevices.ContainsKey(id))
                        {
                            CreatePhysicalTrackedDevice(device, id);
                            TypicalDevices[id] = device;
                        }
                    }
                }
            }

            var keysToRemove = TypicalDevices.Keys.Where(id => !inputDevices.Contains(TypicalDevices[id])).ToList();
            foreach (var key in keysToRemove)
            {
                DestroyPhysicalTrackedDevice(key);
                TypicalDevices.Remove(key);
            }
        }

        private string GenerateID(InputDevice device)
        {
            return $"{device.name}|{device.deviceId}";
        }

        private void CreatePhysicalTrackedDevice(InputDevice device, string uniqueID)
        {
            var gameObject = new GameObject(uniqueID)
            {
                transform =
                {
                    parent = BasisLocalPlayer.Instance.LocalBoneDriver.transform
                }
            };
            var basisXRInput = gameObject.AddComponent<BasisOpenXRControllerInput>();
            basisXRInput.ClassName = nameof(BasisOpenXRControllerInput);
            bool state = GetControllerOrHMD(device, out BasisBoneTrackedRole BasisBoneTrackedRole);
            basisXRInput.Initialize(device, uniqueID, device.name + BasisBoneTrackedRole.ToString(), nameof(BasisUnityInputManagement), state, BasisBoneTrackedRole);
            BasisDeviceManagement.Instance.TryAdd(basisXRInput);
        }

        private bool GetControllerOrHMD(InputDevice device, out BasisBoneTrackedRole BasisBoneTrackedRole)
        {
            BasisBoneTrackedRole = BasisBoneTrackedRole.CenterEye;
            if (device is UnityEngine.InputSystem.XR.XRController)
            {
                BasisBoneTrackedRole = device.description.manufacturer.Contains("Left") ? BasisBoneTrackedRole.LeftHand : BasisBoneTrackedRole.RightHand;
                return true;
            }
            else if (device is UnityEngine.InputSystem.XR.XRHMD)
            {
                BasisBoneTrackedRole = BasisBoneTrackedRole.CenterEye;
                return true;
            }
            return false;
        }

        public void DestroyPhysicalTrackedDevice(string id)
        {
            TypicalDevices.Remove(id);
            BasisDeviceManagement.Instance.RemoveDevicesFrom("BasisUnityInputManagement", id);
        }

        public override void StopSDK()
        {
            BasisDebug.Log("Stopping BasisUnityInputManagement");
            foreach (var device in TypicalDevices.Keys.ToList())
            {
                DestroyPhysicalTrackedDevice(device);
            }
            if (HasEvents)
            {
                InputSystem.onDeviceChange -= OnDeviceChanged;
                HasEvents = false;
            }
        }

        public override void BeginLoadSDK()
        {
        }

        public override void StartSDK()
        {
            BasisDeviceManagement.Instance.SetCameraRenderState(true);
            BasisDebug.Log("Starting BasisUnityInputManagement");
            if (!HasEvents)
            {
                InputSystem.onDeviceChange += OnDeviceChanged;
                HasEvents = true;
            }
            UpdateDeviceList();
        }

        public override string Type()
        {
            return "UnityInputLoader";
        }
    }
}
