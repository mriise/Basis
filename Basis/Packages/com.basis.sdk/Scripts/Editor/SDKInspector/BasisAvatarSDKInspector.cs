using System;
using Basis.Scripts.BasisSdk;
using Basis.Scripts.BasisSdk.Helpers.Editor;
using Basis.Scripts.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(BasisAvatar))]
public partial class BasisAvatarSDKInspector : Editor
{
    public static event Action<BasisAvatarSDKInspector> InspectorGuiCreated;
    public VisualTreeAsset visualTree;
    public BasisAvatar Avatar;
    public VisualElement uiElementsRoot;
    public bool AvatarEyePositionState = false;
    public bool AvatarMouthPositionState = false;
    public VisualElement rootElement;
    public AvatarSDKJiggleBonesView AvatarSDKJiggleBonesView = new AvatarSDKJiggleBonesView();
    public AvatarSDKVisemes AvatarSDKVisemes = new AvatarSDKVisemes();
    public Button EventCallbackAvatarBundleButton { get; private set; }
    public Texture2D Texture;
    private Label resultLabel; // Store the result label for later clearing
    public string Error;
    public BasisAssetBundleObject assetBundleObject;
    private void OnEnable()
    {
        visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BasisSDKConstants.AvataruxmlPath);
        Avatar = (BasisAvatar)target;
    }

    public override VisualElement CreateInspectorGUI()
    {
        Avatar = (BasisAvatar)target;
        rootElement = new VisualElement();
        if (visualTree != null)
        {
            uiElementsRoot = visualTree.CloneTree();
            rootElement.Add(uiElementsRoot);

            BasisAutomaticSetupAvatarEditor.TryToAutomatic(this);
            SetupItems();
            AvatarSDKJiggleBonesView.Initialize(this);
            AvatarSDKVisemes.Initialize(this);

            InspectorGuiCreated?.Invoke(this);
        }
        else
        {
            Debug.LogError("VisualTree is null. Make sure the UXML file is assigned correctly.");
        }
        return rootElement;
    }
    public void AutomaticallyFindVisemes()
    {
        SkinnedMeshRenderer Renderer = Avatar.FaceVisemeMesh;
        Undo.RecordObject(Avatar, "Automatically Find Visemes");
        Avatar.FaceVisemeMovement = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        List<string> Names = AvatarHelper.FindAllNames(Renderer);
        foreach (KeyValuePair<string, int> Value in AvatarHelper.SearchForVisemeIndex)
        {
            if (AvatarHelper.GetBlendShapes(Names, Value.Key, out int OnMeshIndex))
            {
                Avatar.FaceVisemeMovement[Value.Value] = OnMeshIndex;
            }
        }
        EditorUtility.SetDirty(Avatar);
        AssetDatabase.Refresh();
        AvatarSDKVisemes.Initialize(this);
    }

    public void AutomaticallyFindBlinking()
    {
        SkinnedMeshRenderer Renderer = Avatar.FaceBlinkMesh;
        Undo.RecordObject(Avatar, "Automatically Find Blinking");
        Avatar.BlinkViseme = new int[] { };
        List<string> Names = AvatarHelper.FindAllNames(Renderer);
        int[] Ints = new int[] { -1 };
        foreach (string Name in AvatarHelper.SearchForBlinkIndex)
        {
            if (AvatarHelper.GetBlendShapes(Names, Name, out int BlendShapeIndex))
            {
                Ints[0] = BlendShapeIndex;
                break;
            }
        }
        Avatar.BlinkViseme = Ints;
        EditorUtility.SetDirty(Avatar);
        AssetDatabase.Refresh();
        AvatarSDKVisemes.Initialize(this);
    }

    public void ClickedAvatarEyePositionButton(Button Button)
    {
        Undo.RecordObject(Avatar, "Toggle Eye Position Gizmo");
        AvatarEyePositionState = !AvatarEyePositionState;
        Button.text = "Eye Position Gizmo " + AvatarHelper.BoolToText(AvatarEyePositionState);
        EditorUtility.SetDirty(Avatar);
    }

    public void ClickedAvatarMouthPositionButton(Button Button)
    {
        Undo.RecordObject(Avatar, "Toggle Mouth Position Gizmo");
        AvatarMouthPositionState = !AvatarMouthPositionState;
        Button.text = "Mouth Position Gizmo " + AvatarHelper.BoolToText(AvatarMouthPositionState);
        EditorUtility.SetDirty(Avatar);
    }

    public void EventCallbackAnimator(ChangeEvent<UnityEngine.Object> evt, ref Animator Renderer)
    {
        //  Debug.Log(nameof(EventCallbackAnimator));
        Undo.RecordObject(Avatar, "Change Animator");
        Renderer = (Animator)evt.newValue;
        // Check if the Avatar is part of a prefab
        if (PrefabUtility.IsPartOfPrefabInstance(Avatar))
        {
            // Record the prefab modification
            PrefabUtility.RecordPrefabInstancePropertyModifications(Avatar);
        }
        EditorUtility.SetDirty(Avatar);
    }

    public void EventCallbackFaceVisemeMesh(ChangeEvent<UnityEngine.Object> evt, ref SkinnedMeshRenderer Renderer)
    {
        // Debug.Log(nameof(EventCallbackFaceVisemeMesh));
        Undo.RecordObject(Avatar, "Change Face Viseme Mesh");
        Renderer = (SkinnedMeshRenderer)evt.newValue;

        // Check if the Avatar is part of a prefab
        if (PrefabUtility.IsPartOfPrefabInstance(Avatar))
        {
            // Record the prefab modification
            PrefabUtility.RecordPrefabInstancePropertyModifications(Avatar);
        }
        EditorUtility.SetDirty(Avatar);
    }

    private void OnMouthHeightValueChanged(ChangeEvent<Vector2> evt)
    {
        Undo.RecordObject(Avatar, "Change Mouth Height");
        Avatar.AvatarMouthPosition = new Vector3(evt.newValue.x, evt.newValue.y, 0);
        EditorUtility.SetDirty(Avatar);
    }

    private void OnEyeHeightValueChanged(ChangeEvent<Vector2> evt)
    {
        Undo.RecordObject(Avatar, "Change Eye Height");
        Avatar.AvatarEyePosition = new Vector3(evt.newValue.x, evt.newValue.y,0);
        EditorUtility.SetDirty(Avatar);
    }

    private void OnSceneGUI()
    {
        BasisAvatar avatar = (BasisAvatar)target;
        BasisAvatarGizmoEditor.UpdateGizmos(this, avatar);
    }
    public void SetupItems()
    {
        // Initialize Buttons
        Button avatarEyePositionClick = BasisHelpersGizmo.Button(uiElementsRoot, BasisSDKConstants.avatarEyePositionButton);
        Button avatarMouthPositionClick = BasisHelpersGizmo.Button(uiElementsRoot, BasisSDKConstants.avatarMouthPositionButton);
        Button avatarBundleButton = BasisHelpersGizmo.Button(uiElementsRoot, BasisSDKConstants.AvatarBundleButton);
        Button avatarAutomaticVisemeDetectionClick = BasisHelpersGizmo.Button(uiElementsRoot, BasisSDKConstants.AvatarAutomaticVisemeDetection);
        Button avatarAutomaticBlinkDetectionClick = BasisHelpersGizmo.Button(uiElementsRoot, BasisSDKConstants.AvatarAutomaticBlinkDetection);

        // Initialize Event Callbacks for Vector2 fields (for Avatar Eye and Mouth Position)
        BasisHelpersGizmo.CallBackVector2Field(uiElementsRoot, BasisSDKConstants.avatarEyePositionField, Avatar.AvatarEyePosition, OnEyeHeightValueChanged);
        BasisHelpersGizmo.CallBackVector2Field(uiElementsRoot, BasisSDKConstants.avatarMouthPositionField, Avatar.AvatarMouthPosition, OnMouthHeightValueChanged);

        // Initialize ObjectFields and assign references
        ObjectField animatorField = uiElementsRoot.Q<ObjectField>(BasisSDKConstants.animatorField);
        ObjectField faceBlinkMeshField = uiElementsRoot.Q<ObjectField>(BasisSDKConstants.FaceBlinkMeshField);
        ObjectField faceVisemeMeshField = uiElementsRoot.Q<ObjectField>(BasisSDKConstants.FaceVisemeMeshField);

        TextField AvatarNameField = uiElementsRoot.Q<TextField>(BasisSDKConstants.AvatarName);
        TextField AvatarDescriptionField = uiElementsRoot.Q<TextField>(BasisSDKConstants.AvatarDescription);

        TextField AvatarpasswordField = uiElementsRoot.Q<TextField>(BasisSDKConstants.Avatarpassword);

        ObjectField AvatarIconField = uiElementsRoot.Q<ObjectField>(BasisSDKConstants.AvatarIcon);

        Label ErrorMessage = uiElementsRoot.Q<Label>(BasisSDKConstants.ErrorMessage);

        animatorField.allowSceneObjects = true;
        faceBlinkMeshField.allowSceneObjects = true;
        faceVisemeMeshField.allowSceneObjects = true;
        AvatarIconField.allowSceneObjects = true;

        AvatarIconField.value = null;
        animatorField.value = Avatar.Animator;
        faceBlinkMeshField.value = Avatar.FaceBlinkMesh;
        faceVisemeMeshField.value = Avatar.FaceVisemeMesh;

        AvatarNameField.value = Avatar.BasisBundleDescription.AssetBundleName;
        AvatarDescriptionField.value = Avatar.BasisBundleDescription.AssetBundleDescription;

        AvatarNameField.RegisterCallback<ChangeEvent<string>>(AvatarName);
        AvatarDescriptionField.RegisterCallback<ChangeEvent<string>>(AvatarDescription);

        AvatarIconField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(OnAssignTexture2D);

        // Button click events
        avatarEyePositionClick.clicked += () => ClickedAvatarEyePositionButton(avatarEyePositionClick);
        avatarMouthPositionClick.clicked += () => ClickedAvatarMouthPositionButton(avatarMouthPositionClick);
        avatarAutomaticVisemeDetectionClick.clicked += AutomaticallyFindVisemes;
        avatarAutomaticBlinkDetectionClick.clicked += AutomaticallyFindBlinking;

        // Multi-select dropdown (Foldout with Toggles)
        Foldout buildTargetFoldout = new Foldout { text = "Select Build Targets", value = true }; // Expanded by default
        uiElementsRoot.Add(buildTargetFoldout);
        if (assetBundleObject == null)
        {
            assetBundleObject = AssetDatabase.LoadAssetAtPath<BasisAssetBundleObject>(BasisAssetBundleObject.AssetBundleObject);

        }

        foreach (var target in BasisSDKConstants.allowedTargets)
        {
            // Check if the target is already selected
            bool isSelected = assetBundleObject.selectedTargets.Contains(target);

            Toggle toggle = new Toggle(BasisSDKConstants.targetDisplayNames[target])
            {
                value = isSelected // Set the toggle based on whether the target is in the selected list
            };

            toggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    assetBundleObject.selectedTargets.Add(target);
                else
                    assetBundleObject.selectedTargets.Remove(target);
            });

            buildTargetFoldout.Add(toggle);
        }
        avatarBundleButton.clicked += () => EventCallbackAvatarBundle(assetBundleObject.selectedTargets);

        // Register Animator field change event
        animatorField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(evt => EventCallbackAnimator(evt, ref Avatar.Animator));

        // Register Blink and Viseme Mesh field change events
        faceBlinkMeshField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(evt => EventCallbackFaceVisemeMesh(evt, ref Avatar.FaceBlinkMesh));
        faceVisemeMeshField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(evt => EventCallbackFaceVisemeMesh(evt, ref Avatar.FaceVisemeMesh));

        // Update Button Text
        avatarEyePositionClick.text = "Eye Position Gizmo " + AvatarHelper.BoolToText(AvatarEyePositionState);
        avatarMouthPositionClick.text = "Mouth Position Gizmo " + AvatarHelper.BoolToText(AvatarMouthPositionState);

        ErrorMessage.visible = false;
        ErrorMessage.text = "";
    }
    private async void EventCallbackAvatarBundle(List<BuildTarget> targets)
    {
        if (targets == null || targets.Count == 0)
        {
            Debug.LogError("No build targets selected.");
            return;
        }
        if (ValidateAvatar())
        {
            Debug.Log($"Building Gameobject Bundles for: {string.Join(", ", targets.ConvertAll(t => BasisSDKConstants.targetDisplayNames[t]))}");
            (bool success, string message) = await BasisBundleBuild.GameObjectBundleBuild(Avatar, targets);
            // Clear any previous result label
            ClearResultLabel();

            // Display new result in the UI
            resultLabel = new Label
            {
                style = { fontSize = 14 }
            };
            resultLabel.style.color = Color.black; // Error message color
            if (success)
            {
                resultLabel.text = "Build successful";
                resultLabel.style.backgroundColor = Color.green;
            }
            else
            {
                resultLabel.text = $"Build failed: {message}";
                resultLabel.style.backgroundColor = Color.red;
            }

            // Add the result label to the UI
            uiElementsRoot.Add(resultLabel);
        }
        else
        {
            EditorUtility.DisplayDialog("Avatar Error", "The Avatar has a issue check the console for a error", "will do");
        }
    }
    private void ClearResultLabel()
    {
        if (resultLabel != null)
        {
            uiElementsRoot.Remove(resultLabel);  // Remove the label from the UI
            resultLabel = null; // Optionally reset the reference to null
        }
    }
    public bool ValidateAvatar()
    {
        if (Avatar == null)
        {
            Debug.LogError("Missing Avatar");
            return false;
        }
        if (Avatar.Animator == null)
        {
            Debug.LogError("Missing Animator");
            return false;
        }
        if (Avatar.BlinkViseme == null || Avatar.BlinkViseme.Length == 0)
        {
            Debug.LogError("BlinkViseme Meta Data was null or empty this should never occur");
            return false;
        }
        if (Avatar.FaceVisemeMovement == null || Avatar.FaceVisemeMovement.Length == 0)
        {
            Debug.LogError("FaceVisemeMovement Meta Data was null or empty this should never occur");
            return false;
        }
        if (Avatar.FaceBlinkMesh == null)
        {
            Debug.LogError("FaceBlinkMesh was null, please assign a skinned mesh does not need to be the face blink mesh just a mesh");
            return false;
        }
        if (Avatar.FaceVisemeMesh == null)
        {
            Debug.LogError("FaceVisemeMesh was null, please assign a skinned mesh does not need to be the face blink mesh just a mesh");
            return false;
        }
        if (Avatar.AvatarEyePosition == Vector2.zero)
        {
            Debug.LogError("Avatar Eye Position was empty");
            return false;
        }
        if (Avatar.AvatarMouthPosition == Vector2.zero)
        {
            Debug.LogError("Avatar Mouth Position was empty");
            return false;
        }
        return true;
    }
    public void OnAssignTexture2D(ChangeEvent<UnityEngine.Object> Texture2D)
    {
        Texture = (Texture2D)Texture2D.newValue;
    }
    public void AvatarDescription(ChangeEvent<string> evt)
    {
        Avatar.BasisBundleDescription.AssetBundleDescription = evt.newValue;
        EditorUtility.SetDirty(Avatar);
        AssetDatabase.Refresh();
    }
    public void AvatarName(ChangeEvent<string> evt)
    {
        Avatar.BasisBundleDescription.AssetBundleName = evt.newValue;
        EditorUtility.SetDirty(Avatar);
        AssetDatabase.Refresh();
    }
}
