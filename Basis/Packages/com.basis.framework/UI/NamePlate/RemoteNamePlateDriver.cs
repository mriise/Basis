using Basis.Scripts.Device_Management;
using Basis.Scripts.Drivers;
using Unity.Mathematics;
using UnityEngine;

namespace Basis.Scripts.UI.NamePlate
{
    public class RemoteNamePlateDriver : MonoBehaviour
    {
        // Use an array for better performance
        private static BasisRemoteNamePlate[] RemoteNamePlates = new BasisRemoteNamePlate[0];
        private static int count = 0; // Track the number of active elements
        public static RemoteNamePlateDriver Instance;
        public Color NormalColor;
        public Color IsTalkingColor;
        public Color OutOfRangeColor;
        [SerializeField]
        public static float transitionDuration = 0.3f;
        [SerializeField]
        public static float returnDelay = 0.4f;
        public static float YHeightMultiplier = 1.25f;

        public static Color StaticNormalColor;
        public static Color StaticIsTalkingColor;
        public static Color StaticOutOfRangeColor;
        public static Vector3 dirToCamera;
        public static Vector3 cachedDirection;
        public static Quaternion cachedRotation;
        public void Awake()
        {
            Instance = this;
            if (BasisDeviceManagement.IsMobile())
            {
                NormalColor.a = 1;
                IsTalkingColor.a = 1;
                OutOfRangeColor.a = 1;
            }
            StaticNormalColor = NormalColor;
            StaticIsTalkingColor = IsTalkingColor;
            StaticOutOfRangeColor = OutOfRangeColor;
        }

        /// <summary>
        /// Adds a new BasisNamePlate to the array.
        /// </summary>
        public void AddNamePlate(BasisRemoteNamePlate newNamePlate)
        {
            if (newNamePlate == null)
            {
                return;
            }


            // Check if it already exists
            for (int i = 0; i < count; i++)
            {
                if (RemoteNamePlates[i] == newNamePlate)
                {
                    return;
                }
            }

            // Resize if necessary
            if (count >= RemoteNamePlates.Length)
            {
                ResizeArray(RemoteNamePlates.Length == 0 ? 4 : RemoteNamePlates.Length * 2);
            }

            // Add the new nameplate
            RemoteNamePlates[count++] = newNamePlate;
        }

        /// <summary>
        /// Removes an existing BasisNamePlate from the array.
        /// </summary>
        public void RemoveNamePlate(BasisRemoteNamePlate namePlateToRemove)
        {
            if (namePlateToRemove == null) return;

            for (int RemotePlayerIndex = 0; RemotePlayerIndex < count; RemotePlayerIndex++)
            {
                if (RemoteNamePlates[RemotePlayerIndex] == namePlateToRemove)
                {
                    // Shift elements down to remove the nameplate
                    for (int Index = RemotePlayerIndex; Index < count - 1; Index++)
                    {
                        RemoteNamePlates[Index] = RemoteNamePlates[Index + 1];
                    }

                    RemoteNamePlates[--count] = null; // Clear the last element
                    break;
                }
            }
        }

        /// <summary>
        /// Removes a BasisNamePlate by index.
        /// </summary>
        public void RemoveNamePlateAt(int index)
        {
            if (index < 0 || index >= count) return;

            // Shift elements down to remove the nameplate
            for (int i = index; i < count - 1; i++)
            {
                RemoteNamePlates[i] = RemoteNamePlates[i + 1];
            }

            RemoteNamePlates[--count] = null; // Clear the last element
        }

        /// <summary>
        /// Resizes the internal array.
        /// </summary>
        private void ResizeArray(int newSize)
        {
            BasisRemoteNamePlate[] newArray = new BasisRemoteNamePlate[newSize];
            for (int Index = 0; Index < count; Index++)
            {
                newArray[Index] = RemoteNamePlates[Index];
            }

            RemoteNamePlates = newArray;
        }
        public static float x;
        public static float z;
        public static void SimulateNamePlates()
        {
            Vector3 Position = BasisLocalCameraDriver.Position;
            for (int Index = 0; Index < count; Index++)
            {
                BasisRemoteNamePlate NamePlate = RemoteNamePlates[Index];
                if (NamePlate.IsVisible)
                {
                    cachedDirection = NamePlate.HipTarget.OutgoingWorldData.position;
                    cachedDirection.y += NamePlate.MouthTarget.TposeLocal.position.y / YHeightMultiplier;
                    dirToCamera = Position - cachedDirection;
                    cachedRotation = Quaternion.Euler(x, math.atan2(dirToCamera.x, dirToCamera.z) * Mathf.Rad2Deg, z);
                    NamePlate.Self.SetPositionAndRotation(cachedDirection, cachedRotation);
                }
            }
        }
    }
}
