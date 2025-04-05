using Basis.Scripts.Device_Management;
using Basis.Scripts.Drivers;
using UnityEngine;

namespace Basis.Scripts.UI.NamePlate
{
    public class RemoteNamePlateDriver : MonoBehaviour
    {
        // Use an array for better performance
        private BasisNamePlate[] basisRemotePlayers = new BasisNamePlate[0];
        private int count = 0; // Track the number of active elements
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
        public void AddNamePlate(BasisNamePlate newNamePlate)
        {
            if (newNamePlate == null) return;

            // Check if it already exists
            for (int i = 0; i < count; i++)
            {
                if (basisRemotePlayers[i] == newNamePlate) return;
            }

            // Resize if necessary
            if (count >= basisRemotePlayers.Length)
            {
                ResizeArray(basisRemotePlayers.Length == 0 ? 4 : basisRemotePlayers.Length * 2);
            }

            // Add the new nameplate
            basisRemotePlayers[count++] = newNamePlate;
        }

        /// <summary>
        /// Removes an existing BasisNamePlate from the array.
        /// </summary>
        public void RemoveNamePlate(BasisNamePlate namePlateToRemove)
        {
            if (namePlateToRemove == null) return;

            for (int i = 0; i < count; i++)
            {
                if (basisRemotePlayers[i] == namePlateToRemove)
                {
                    // Shift elements down to remove the nameplate
                    for (int j = i; j < count - 1; j++)
                    {
                        basisRemotePlayers[j] = basisRemotePlayers[j + 1];
                    }

                    basisRemotePlayers[--count] = null; // Clear the last element
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
                basisRemotePlayers[i] = basisRemotePlayers[i + 1];
            }

            basisRemotePlayers[--count] = null; // Clear the last element
        }

        /// <summary>
        /// Resizes the internal array.
        /// </summary>
        private void ResizeArray(int newSize)
        {
            BasisNamePlate[] newArray = new BasisNamePlate[newSize];
            for (int i = 0; i < count; i++)
            {
                newArray[i] = basisRemotePlayers[i];
            }

            basisRemotePlayers = newArray;
        }
        public float x;
        public float z;
        public void LateUpdate()
        {
            Vector3 Position = BasisLocalCameraDriver.Position;
            for (int Index = 0; Index < count; Index++)
            {
                BasisNamePlate NamePlate = basisRemotePlayers[Index];
                if (NamePlate.IsVisible)
                {
                    cachedDirection = NamePlate.HipTarget.OutgoingWorldData.position;
                    cachedDirection.y += NamePlate.MouthTarget.TposeLocal.position.y / YHeightMultiplier;
                    dirToCamera = Position - cachedDirection;
                    cachedRotation = Quaternion.Euler(x, Mathf.Atan2(dirToCamera.x, dirToCamera.z) * Mathf.Rad2Deg, z);
                    NamePlate.transform.SetPositionAndRotation(cachedDirection, cachedRotation);
                }
            }
        }
    }
}
