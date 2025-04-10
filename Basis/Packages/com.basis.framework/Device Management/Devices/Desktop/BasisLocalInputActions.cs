using Basis.Scripts.BasisSdk.Helpers;
using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Drivers;
using Basis.Scripts.UI.UI_Panels;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Basis.Scripts.Device_Management.Devices.Desktop
{
    [DefaultExecutionOrder(15003)]
    public class BasisLocalInputActions : MonoBehaviour
    {
        public InputActionReference MoveAction;
        public InputActionReference LookAction;
        public InputActionReference JumpAction;
        public InputActionReference CrouchAction;
        public InputActionReference RunButton;
        public InputActionReference Escape;
        public InputActionReference PrimaryButtonGetState;

        public InputActionReference DesktopSwitch;
        public InputActionReference XRSwitch;

        public InputActionReference LeftMousePressed;
        public InputActionReference RightMousePressed;

        public InputActionReference MiddleMouseScroll;
        public InputActionReference MiddleMouseScrollClick;

        [SerializeField] public static bool Crouching;
        [SerializeField] public static Vector2 LookDirection;
        public static BasisAvatarEyeInput CharacterEyeInput;
        public static BasisLocalInputActions Instance;
        public BasisLocalPlayer basisLocalPlayer;
        public PlayerInput Input;
        public static string InputActions = "InputActions";
        public static bool IgnoreCrouchToggle = false;
        public static Action AfterAvatarChanges;
        [SerializeField]
        public BasisInputState InputState = new BasisInputState();

        public void OnEnable()
        {
            if (BasisHelpers.CheckInstance(Instance))
            {
                Instance = this;
            }
            InputSystem.settings.SetInternalFeatureFlag("USE_OPTIMIZED_CONTROLS", true);
            InputSystem.settings.SetInternalFeatureFlag("USE_READ_VALUE_CACHING", true);
            BasisLocalCameraDriver.InstanceExists += SetupCamera;
            if (BasisDeviceManagement.IsMobile() == false)
            {
                EnableActions();
                AddCallbacks();
            }
        }

        public void OnDisable()
        {
            BasisLocalCameraDriver.InstanceExists -= SetupCamera;
            if (BasisDeviceManagement.IsMobile() == false)
            {
                RemoveCallbacks();
                DisableActions();
            }
        }
        public void SetupCamera()
        {
            Input.camera = BasisLocalCameraDriver.Instance.Camera;
        }
        public void Initialize(BasisLocalPlayer localPlayer)
        {
            basisLocalPlayer = localPlayer;
            this.gameObject.SetActive(true);
        }

        private void EnableActions()
        {
            DesktopSwitch.action.Enable();
            XRSwitch.action.Enable();
            MoveAction.action.Enable();
            LookAction.action.Enable();
            JumpAction.action.Enable();
            CrouchAction.action.Enable();
            RunButton.action.Enable();
            Escape.action.Enable();
            PrimaryButtonGetState.action.Enable();
            LeftMousePressed.action.Enable();
            RightMousePressed.action.Enable();
            MiddleMouseScroll.action.Enable();
            MiddleMouseScrollClick.action.Enable();
        }

        private void DisableActions()
        {
            DesktopSwitch.action.Disable();
            XRSwitch.action.Disable();
            MoveAction.action.Disable();
            LookAction.action.Disable();
            JumpAction.action.Disable();
            CrouchAction.action.Disable();
            RunButton.action.Disable();
            Escape.action.Disable();
            PrimaryButtonGetState.action.Disable();
            LeftMousePressed.action.Disable();
            RightMousePressed.action.Disable();
            MiddleMouseScroll.action.Disable();
            MiddleMouseScrollClick.action.Disable();
        }

        private void AddCallbacks()
        {
            CrouchAction.action.performed += OnCrouchStarted;
            DesktopSwitch.action.performed += OnSwitchDesktop;
            Escape.action.performed += OnEscapePerformed;
            JumpAction.action.performed += OnJumpActionPerformed;
            LeftMousePressed.action.performed += OnLeftMouse;
            MiddleMouseScroll.action.performed += OnMouseScroll;
            MiddleMouseScrollClick.action.performed += OnMouseScrollClick;
            MoveAction.action.performed += OnMoveActionStarted;
            PrimaryButtonGetState.action.performed += OnPrimaryGet;
            RightMousePressed.action.performed += OnRightMouse;
            RunButton.action.performed += OnRunStarted;
            LookAction.action.performed += OnLookActionStarted;
            XRSwitch.action.performed += OnSwitchOpenXR;

            CrouchAction.action.canceled += OnCrouchCancelled;
            DesktopSwitch.action.canceled += OnSwitchDesktop;
            Escape.action.canceled += OnEscapeCancelled;
            JumpAction.action.canceled += OnJumpActionCancelled;
            LeftMousePressed.action.canceled += OnLeftMouse;
            MiddleMouseScroll.action.canceled += OnMouseScroll;
            MiddleMouseScrollClick.action.canceled += OnMouseScrollClick;
            MoveAction.action.canceled += OnMoveActionCancelled;
            PrimaryButtonGetState.action.canceled += OnCancelPrimaryGet;
            RightMousePressed.action.canceled += OnRightMouse;
            RunButton.action.canceled += OnRunCancelled;
            LookAction.action.canceled += OnLookActionCancelled;
        }
        private void RemoveCallbacks()
        {
            CrouchAction.action.performed -= OnCrouchStarted;
            DesktopSwitch.action.performed -= OnSwitchDesktop;
            Escape.action.performed -= OnEscapePerformed;
            JumpAction.action.performed -= OnJumpActionPerformed;
            LeftMousePressed.action.performed -= OnLeftMouse;
            MiddleMouseScroll.action.performed -= OnMouseScroll;
            MiddleMouseScrollClick.action.performed -= OnMouseScrollClick;
            MoveAction.action.performed -= OnMoveActionStarted;
            PrimaryButtonGetState.action.performed -= OnPrimaryGet;
            RightMousePressed.action.performed -= OnRightMouse;
            RunButton.action.performed -= OnRunStarted;
            LookAction.action.performed -= OnLookActionStarted;
            XRSwitch.action.performed -= OnSwitchOpenXR;

            CrouchAction.action.canceled -= OnCrouchCancelled;
            DesktopSwitch.action.canceled -= OnSwitchDesktop;
            Escape.action.canceled -= OnEscapeCancelled;
            JumpAction.action.canceled -= OnJumpActionCancelled;
            LeftMousePressed.action.canceled -= OnLeftMouse;
            MiddleMouseScroll.action.canceled -= OnMouseScroll;
            MiddleMouseScrollClick.action.canceled -= OnMouseScrollClick;
            MoveAction.action.canceled -= OnMoveActionCancelled;
            PrimaryButtonGetState.action.canceled -= OnCancelPrimaryGet;
            RightMousePressed.action.canceled -= OnRightMouse;
            RunButton.action.canceled -= OnRunCancelled;
            LookAction.action.canceled -= OnLookActionCancelled;
        }
        // Input action methods
        private  void OnMoveActionStarted(InputAction.CallbackContext ctx)
        {
            basisLocalPlayer.LocalMoveDriver.MovementVector = ctx.ReadValue<Vector2>();
        }

        private static void OnMoveActionCancelled(InputAction.CallbackContext ctx)
        {
            BasisLocalInputActions.Instance.basisLocalPlayer.LocalMoveDriver.MovementVector = Vector2.zero;
        }

        private static void OnLookActionStarted(InputAction.CallbackContext ctx)
        {
            LookDirection = ctx.ReadValue<Vector2>();
            if (BasisLocalInputActions.CharacterEyeInput != null)
            {
                BasisLocalInputActions.CharacterEyeInput.HandleMouseRotation(LookDirection);
            }
        }

        private static void OnLookActionCancelled(InputAction.CallbackContext ctx)
        {
            LookDirection = Vector2.zero;
            if (BasisLocalInputActions.CharacterEyeInput != null)
            {
                BasisLocalInputActions.CharacterEyeInput.HandleMouseRotation(LookDirection);
            }
        }

        private static void OnJumpActionPerformed(InputAction.CallbackContext ctx)
        {
            BasisLocalInputActions.Instance.basisLocalPlayer.LocalMoveDriver.HandleJump();
        }

        private static void OnJumpActionCancelled(InputAction.CallbackContext ctx)
        {
            // Logic for when jump is cancelled (if needed)
        }

        private static void OnCrouchStarted(InputAction.CallbackContext ctx)
        {
            CrouchToggle(ctx);
        }

        private static void OnCrouchCancelled(InputAction.CallbackContext ctx)
        {
            CrouchToggle(ctx);
        }

        private static void CrouchToggle(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                if (!IgnoreCrouchToggle)
                    Crouching = !Crouching;

                if (BasisLocalInputActions.CharacterEyeInput != null)
                {
                    BasisLocalInputActions.CharacterEyeInput.HandleMouseRotation(LookDirection);
                }

                BasisLocalInputActions.Instance.basisLocalPlayer.LocalMoveDriver.SpeedMultiplier = Crouching ? 0 : 0.5f;
            }
        }

        private static void OnRunStarted(InputAction.CallbackContext ctx)
        {
            BasisLocalInputActions.Instance.basisLocalPlayer.LocalMoveDriver.SpeedMultiplier = Crouching ? 0 : 1;
        }

        private static void OnRunCancelled(InputAction.CallbackContext ctx)
        {
            BasisLocalInputActions.Instance.basisLocalPlayer.LocalMoveDriver.SpeedMultiplier = Crouching ? 0 : 0.5f;
        }

        private static void OnEscapePerformed(InputAction.CallbackContext ctx)
        {
            if (BasisHamburgerMenu.Instance == null)
            {
                BasisHamburgerMenu.OpenHamburgerMenuNow();
            }
            else
            {
                BasisHamburgerMenu.Instance.CloseThisMenu();
                BasisHamburgerMenu.Instance = null;
            }
        }

        private static void OnEscapeCancelled(InputAction.CallbackContext ctx)
        {
            // Logic for escape action cancellation (if needed)
        }

        private static void OnPrimaryGet(InputAction.CallbackContext ctx)
        {
            BasisLocalInputActions.Instance.InputState.PrimaryButtonGetState = true;
        }

        private static void OnCancelPrimaryGet(InputAction.CallbackContext ctx)
        {
            BasisLocalInputActions.Instance.InputState.PrimaryButtonGetState = false;
        }

        private static void OnSwitchDesktop(InputAction.CallbackContext ctx)
        {
            BasisDeviceManagement.ForceSetDesktop();
        }

        private static void OnSwitchOpenXR(InputAction.CallbackContext ctx)
        {
            BasisDeviceManagement.ForceLoadXR();
        }

        private static void OnLeftMouse(InputAction.CallbackContext ctx)
        {
         BasisLocalInputActions.Instance.InputState.Trigger = ctx.ReadValue<float>();
        }

        private static void OnRightMouse(InputAction.CallbackContext ctx)
        {
            // Handle right mouse press logic here if needed
        }

        private  static void OnMouseScroll(InputAction.CallbackContext ctx)
        {
            BasisLocalInputActions.Instance.InputState.Secondary2DAxis = ctx.ReadValue<Vector2>();
        }

        private static void OnMouseScrollClick(InputAction.CallbackContext ctx)
        {
            BasisLocalInputActions.Instance.InputState.Secondary2DAxisClick = ctx.ReadValue<float>() == 1;
        }
    }
}
