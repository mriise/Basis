using Basis.Scripts.BasisSdk;
using Basis.Scripts.BasisSdk.Helpers.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(BasisScene))]
public class BasisSceneSDKInspector : Editor
{
    public VisualTreeAsset visualTree;
    public BasisScene BasisScene;
    public VisualElement rootElement;
    public VisualElement uiElementsRoot;
    private Label resultLabel; // Store the result label for later clearing

    private List<BuildTarget> selectedTargets = new List<BuildTarget>();

    public void OnEnable()
    {
        visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BasisSDKConstants.SceneuxmlPath);
        BasisScene = (BasisScene)target;

        // Initialize selectedTargets with all available targets
        selectedTargets = new List<BuildTarget>(BasisSDKConstants.allowedTargets);
    }

    public override VisualElement CreateInspectorGUI()
    {
        BasisScene = (BasisScene)target;
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

            foreach (var target in BasisSDKConstants.allowedTargets)
            {
                Toggle toggle = new Toggle(BasisSDKConstants.targetDisplayNames[target])
                {
                    value = true // Set all toggles to true by default
                };

                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                        selectedTargets.Add(target);
                    else
                        selectedTargets.Remove(target);
                });

                buildTargetFoldout.Add(toggle);
            }

            // Build Button
            Button buildButton = BasisHelpersGizmo.Button(uiElementsRoot, BasisSDKConstants.BuildButton);
            buildButton.clicked += () => Build(buildButton, selectedTargets);
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

        Debug.Log($"Building Scene Bundles for: {string.Join(", ", targets.ConvertAll(t => BasisSDKConstants.targetDisplayNames[t]))}");

        // Call the build function and capture result
        (bool success, string message) = await BasisBundleBuild.SceneBundleBuild(BasisScene, targets);
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
