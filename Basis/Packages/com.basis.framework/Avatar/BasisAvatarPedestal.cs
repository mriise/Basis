using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Device_Management.Devices;
using Basis.Scripts.TransformBinders.BoneControl;
using UnityEngine;

public class BasisAvatarPedestal : InteractableObject
{
    public Transform Avatar;
    public string UniqueID;
    public void Start()
    {
        Initalize();
    }
    public void Initalize()
    {
        if (Avatar == null)
        {
            BasisDebug.LogError("Avatar is not assigned.");
            return;
        }

        Renderer[] renderers = Avatar.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            BasisDebug.LogWarning("No renderers found on Avatar.");
            return;
        }

        // Calculate total bounds
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        // Height is the y size of the bounding box
        float height = bounds.size.y;
        float radius = Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f;

        // Add or get a CapsuleCollider
        if (TryGetComponent<CapsuleCollider>(out CapsuleCollider capsule))
        {
        }
        else
        {
            capsule = gameObject.AddComponent<CapsuleCollider>();
        }

        capsule.center = Avatar.InverseTransformPoint(bounds.center);
        capsule.height = height;
        capsule.radius = radius;
        capsule.direction = 1; // Y axis

        BasisDebug.Log($"CapsuleCollider added: Height={height}, Radius={radius}, Center={capsule.center}");
        UniqueID =  BasisGenerateUniqueID.GenerateUniqueID();
    }

    public override bool CanHover(BasisInput input)
    {
        return !DisableInfluence &&
            !IsPuppeted &&
            Inputs.IsInputAdded(input) &&
            input.TryGetRole(out BasisBoneTrackedRole role) &&
            Inputs.TryGetByRole(role, out BasisInputWrapper found) &&
            found.GetState() == InteractInputState.Ignored &&
            IsWithinRange(found.BoneControl.OutgoingWorldData.position);
    }
    public override bool CanInteract(BasisInput input)
    {
        return !DisableInfluence &&
            !IsPuppeted &&
            Inputs.IsInputAdded(input) &&
            input.TryGetRole(out BasisBoneTrackedRole role) &&
            Inputs.TryGetByRole(role, out BasisInputWrapper found) &&
            found.GetState() == InteractInputState.Hovering &&
            IsWithinRange(found.BoneControl.OutgoingWorldData.position);
    }

    public override void OnHoverStart(BasisInput input)
    {
        var found = Inputs.FindExcludeExtras(input);
        if (found != null && found.Value.GetState() != InteractInputState.Ignored)
            BasisDebug.LogWarning(nameof(PickupInteractable) + " input state is not ignored OnHoverStart, this shouldn't happen");
        var added = Inputs.ChangeStateByRole(found.Value.Role, InteractInputState.Hovering);
        if (!added)
            BasisDebug.LogWarning(nameof(PickupInteractable) + " did not find role for input on hover");

        OnHoverStartEvent?.Invoke(input);
        HighlightObject(true);
    }

    public override void OnHoverEnd(BasisInput input, bool willInteract)
    {
        if (input.TryGetRole(out BasisBoneTrackedRole role) && Inputs.TryGetByRole(role, out _))
        {
            if (!willInteract)
            {
                if (!Inputs.ChangeStateByRole(role, InteractInputState.Ignored))
                {
                    BasisDebug.LogWarning(nameof(PickupInteractable) + " found input by role but could not remove by it, this is a bug.");
                }
            }
            OnHoverEndEvent?.Invoke(input, willInteract);
            HighlightObject(false);
        }
    }
    public override void OnInteractStart(BasisInput input)
    {
        if (input.TryGetRole(out BasisBoneTrackedRole role) && Inputs.TryGetByRole(role, out BasisInputWrapper wrapper))
        {
            // same input that was highlighting previously
            if (wrapper.GetState() == InteractInputState.Hovering)
            {
                WasPressed();
                OnInteractStartEvent?.Invoke(input);
            }
            else
            {
                Debug.LogWarning("Input source interacted with ReparentInteractable without highlighting first.");
            }
        }
        else
        {
            BasisDebug.LogWarning(nameof(PickupInteractable) + " did not find role for input on Interact start");
        }
    }

    public override void OnInteractEnd(BasisInput input)
    {
        if (input.TryGetRole(out BasisBoneTrackedRole role) && Inputs.TryGetByRole(role, out BasisInputWrapper wrapper))
        {
            if (wrapper.GetState() == InteractInputState.Interacting)
            {
                Inputs.ChangeStateByRole(wrapper.Role, InteractInputState.Ignored);

                WasPressed();
                OnInteractEndEvent?.Invoke(input);
            }
        }
    }
    public void HighlightObject(bool IsHighlighted)
    {

    }
    public bool WasJustPressed = false;
    public async void WasPressed()
    {
        if (Avatar != null && WasJustPressed == false && UniqueID != BasisLocalPlayer.Instance.AvatarMetaData.BasisRemoteBundleEncrypted.CombinedURL)
        {
            WasJustPressed = true;
            BasisLoadableBundle Bundle = new BasisLoadableBundle
            {
                LoadableGameobject = new BasisLoadableBundle.BasisLoadableGameobject()
            };
            Avatar.parent = null;
            GameObject AvatarCopy = GameObject.Instantiate(Avatar.gameObject);
            Bundle.LoadableGameobject.InSceneItem = AvatarCopy;
            Bundle.BasisRemoteBundleEncrypted = new BasisRemoteEncyptedBundle
            {
                CombinedURL = UniqueID
            };
            await BasisLocalPlayer.Instance.CreateAvatar(2, Bundle);
            WasJustPressed = false;
        }
    }
    public override bool IsInteractingWith(BasisInput input)
    {
        var found = Inputs.FindExcludeExtras(input);
        return found.HasValue && found.Value.GetState() == InteractInputState.Interacting;
    }

    public override bool IsHoveredBy(BasisInput input)
    {
        var found = Inputs.FindExcludeExtras(input);
        return found.HasValue && found.Value.GetState() == InteractInputState.Hovering;
    }

    public override void InputUpdate()
    {
    }

    public override bool IsInteractTriggered(BasisInput input)
    {
        // click or mostly triggered
        return input.InputState.Trigger >= 0.9;
    }
}
