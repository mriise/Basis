using Basis.Scripts.BasisSdk.Helpers.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(BasisProp))]
public class BasisPropSDKInspector : Editor
{
    public VisualTreeAsset visualTree;
    public BasisProp BasisProp;
    public VisualElement rootElement;
    public VisualElement uiElementsRoot;
    private Label resultLabel; // Store the result label for later clearing
    public BasisAssetBundleObject assetBundleObject;
    public void OnEnable()
    {
        visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BasisSDKConstants.PropuxmlPath);
        BasisProp = (BasisProp)target;
    }
    public override VisualElement CreateInspectorGUI()
    {
        BasisProp = (BasisProp)target;
        rootElement = new VisualElement();

        // Draw default inspector elements first
        InspectorElement.FillDefaultInspector(rootElement, serializedObject, this);

        if (visualTree != null)
        {
            uiElementsRoot = visualTree.CloneTree();
            rootElement.Add(uiElementsRoot);

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

            Button BuildButton = BasisHelpersGizmo.Button(uiElementsRoot, BasisSDKConstants.BuildButton);
            BuildButton.clicked += () => Build(BuildButton, assetBundleObject.selectedTargets);
        }
        else
        {
            Debug.LogError("VisualTree is null. Make sure the UXML file is assigned correctly.");
        }

        return rootElement;
    }

    private async void Build(Button buildButton, List<BuildTarget> targets)
    {
        if (targets == null || targets.Count == 0)
        {
            Debug.LogError("No build targets selected.");
            return;
        }

        Debug.Log($"Building Gameobject Bundles for: {string.Join(", ", targets.ConvertAll(t => BasisSDKConstants.targetDisplayNames[t]))}");
        (bool success, string message) = await BasisBundleBuild.GameObjectBundleBuild(BasisProp, targets);

        // Clear any previous result label
        ClearResultLabel();

        // Display new result in the UI
        resultLabel = new Label
        {
            style = { fontSize = 14 }
        };

        if (success)
        {
            resultLabel.text = "Build successful";
            resultLabel.style.backgroundColor = Color.green;
            resultLabel.style.color = Color.black; // Success message color
        }
        else
        {
            resultLabel.text = $"Build failed: {message}";
            resultLabel.style.backgroundColor = Color.red;
            resultLabel.style.color = Color.black; // Error message color
        }

        // Add the result label to the UI
        uiElementsRoot.Add(resultLabel);
    }

    // Method to clear the result label
    private void ClearResultLabel()
    {
        if (resultLabel != null)
        {
            uiElementsRoot.Remove(resultLabel);  // Remove the label from the UI
            resultLabel = null; // Optionally reset the reference to null
        }
    }
}
