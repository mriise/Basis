using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEditor.UIElements;
public class BuildReportViewerWindow : EditorWindow
{
    [MenuItem("Basis/Build Report Viewer")]
    public static void ShowWindow()
    {
        GenerateWindow();
    }
    public void OnEnable()
    {
        GenerateWindow();
    }
    public static void GenerateWindow()
    {
        BuildReportViewerWindow wnd = EditorWindow.GetWindow<BuildReportViewerWindow>("Basis Bundle Report");
        wnd.titleContent = new GUIContent("Basis Build Report Viewer");
        wnd.minSize = new Vector2(600, 400);
        wnd.GenerateReportUI();
    }
    public void GenerateReportUI()
    {
        rootVisualElement.Clear();

        BuildReport report = BuildReport.GetLatestReport();
        if (report == null)
        {
            var label = new Label("AssetBundle build failed or no report found.");
            label.style.color = Color.red;
            rootVisualElement.Add(label);
            return;
        }

        var toolbar = new Toolbar();
        var tabContent = new VisualElement();
        tabContent.style.flexGrow = 1;
        tabContent.style.marginTop = 5;
        void SwitchTab(System.Action contentBuilder)
        {
            tabContent.Clear();
            contentBuilder();
        }
        var summaryButton = new ToolbarButton(() => SwitchTab(() => tabContent.Add(BuildSummaryTab(report)))) { text = "Summary" };
        var packedAssetsButton = new ToolbarButton(() => SwitchTab(() => tabContent.Add(PackedAssetsTab(report)))) { text = "Packed Assets" };
        var scenesUsingAssetsButton = new ToolbarButton(() => SwitchTab(() => tabContent.Add(ScenesUsingAssetsTab(report)))) { text = "Scenes Using Assets" };
        var advancedButton = new ToolbarButton(() => SwitchTab(() => tabContent.Add(AdvancedTab(report)))) { text = "Advanced" };
        toolbar.Add(summaryButton);
        toolbar.Add(packedAssetsButton);
        toolbar.Add(scenesUsingAssetsButton);
        toolbar.Add(advancedButton);
        rootVisualElement.Add(toolbar);
        rootVisualElement.Add(tabContent);
        // Load first tab
        SwitchTab(() => tabContent.Add(BuildSummaryTab(report)));
    }
    private VisualElement BuildSummaryTab(BuildReport report)
    {
        var scrollView = new ScrollView();

        var summaryBox = new VisualElement();
        summaryBox.style.paddingLeft = 10;
        summaryBox.style.paddingTop = 10;

        var title = new Label("Build Summary");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.fontSize = 14;
        summaryBox.Add(title);

        Color statusColor = report.summary.result switch
        {
            BuildResult.Succeeded => Color.green,
            BuildResult.Failed => Color.red,
            BuildResult.Cancelled => new Color(1f, 0.65f, 0f), // Orange
            _ => Color.gray
        };

        void AddLine(string label, string value, Color? color = null)
        {
            var line = new Label($"{label}: {value}");
            if (color.HasValue)
                line.style.color = color.Value;
            summaryBox.Add(line);
        }
        AddLine("Result", report.summary.result.ToString(), statusColor);
        AddLine("Total Size", FormatSize(report.summary.totalSize));
        AddLine("Total Time", report.summary.totalTime.ToString("g"));
        AddLine("Total Errors", report.summary.totalErrors.ToString());
        AddLine("Total Warnings", report.summary.totalWarnings.ToString());
        AddLine("Platform", report.summary.platform.ToString());
        AddLine("platformGroup", report.summary.platformGroup.ToString());

        // Display summarized errors if any
        var errorText = report.SummarizeErrors();
        if (!string.IsNullOrWhiteSpace(errorText))
        {
            var errorLabel = new Label("\nErrors Summary:\n" + errorText);
            errorLabel.style.whiteSpace = WhiteSpace.Normal;
            errorLabel.style.color = Color.red;
            errorLabel.style.marginTop = 5;
            summaryBox.Add(errorLabel);
        }

        scrollView.Add(summaryBox);
        return scrollView;
    }
    private VisualElement PackedAssetsTab(BuildReport report)
    {
        var container = new VisualElement();
        var searchField = new ToolbarSearchField();
        var scrollView = new ScrollView();

        container.Add(searchField);
        container.Add(scrollView);

        void Refresh(string search = "")
        {
            scrollView.Clear();

            foreach (PackedAssets packedAsset in report.packedAssets)
            {
                var bundleFoldout = new Foldout { text = $"Bundle: {packedAsset.shortPath}", value = false };
                var assetList = packedAsset.contents
                    .Where(info => string.IsNullOrEmpty(search) || info.sourceAssetPath.ToLower().Contains(search.ToLower()))
                    .OrderByDescending(info => info.packedSize)
                    .ToList();

                foreach (var info in assetList)
                {
                    Texture icon = EditorGUIUtility.ObjectContent(null, info.type).image;

                    var itemContainer = new VisualElement();
                    itemContainer.style.flexDirection = FlexDirection.Row;
                    itemContainer.style.alignItems = Align.Center;
                    itemContainer.style.marginBottom = 2;
                    itemContainer.style.marginLeft = 2;
                    itemContainer.style.cursor = new StyleCursor( StyleKeyword.Auto); // Show it's clickable

                    if (icon != null && icon is Texture2D texture2D)
                    {
                        var iconElement = new VisualElement();
                        iconElement.style.width = 16;
                        iconElement.style.height = 16;
                        iconElement.style.backgroundImage = new StyleBackground(texture2D);
                        iconElement.style.unityBackgroundImageTintColor = Color.white;
                        iconElement.style.marginRight = 4;

                        itemContainer.Add(iconElement);
                    }

                    var label = new Label($"{info.sourceAssetPath} ({info.type.Name}) - {FormatSize(info.packedSize)}");
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;

                    itemContainer.Add(label);

                    // Make the whole container clickable
                    itemContainer.RegisterCallback<ClickEvent>(_ =>
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(info.sourceAssetPath);
                        if (asset != null)
                            EditorGUIUtility.PingObject(asset);
                        else
                            Debug.LogWarning($"Asset not found at path: {info.sourceAssetPath}");
                    });

                    bundleFoldout.Add(itemContainer);
                }

                if (assetList.Count > 0)
                    scrollView.Add(bundleFoldout);
            }
        }

        searchField.RegisterValueChangedCallback(evt => Refresh(evt.newValue));
        Refresh();

        return container;
    }
    private VisualElement ScenesUsingAssetsTab(BuildReport report)
    {
        var container = new VisualElement();
        var searchField = new ToolbarSearchField();
        var scrollView = new ScrollView();

        container.Add(searchField);
        container.Add(scrollView);

        void Refresh(string search = "")
        {
            scrollView.Clear();

            foreach (ScenesUsingAssets scenesUsingAsset in report.scenesUsingAssets)
            {
                foreach (ScenesUsingAsset usage in scenesUsingAsset.list)
                {
                    if (!string.IsNullOrEmpty(search) && !usage.assetPath.ToLower().Contains(search.ToLower()))
                        continue;

                    var foldout = new Foldout { text = $"Asset: {usage.assetPath}", value = false };

                    foreach (string scenePath in usage.scenePaths.OrderBy(p => p))
                    {
                        foldout.Add(new Label($"Used in: {scenePath}"));
                    }

                    scrollView.Add(foldout);
                }
            }
        }

        searchField.RegisterValueChangedCallback(evt => Refresh(evt.newValue));
        Refresh();

        return container;
    }
    private VisualElement AdvancedTab(BuildReport report)
    {
        var scrollView = new ScrollView();

        // Build Steps
        var stepsFoldout = new Foldout { text = "Build Steps", value = false };

        foreach (var step in report.steps)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 2;
            row.style.paddingLeft = step.depth * 12; // Indentation by depth

            var stepLabel = new Label($"- {step.name}");
            stepLabel.style.flexGrow = 1;
            stepLabel.style.whiteSpace = WhiteSpace.Normal;

            var durationLabel = new Label($"{step.duration.TotalSeconds:F2} s");
            durationLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            durationLabel.style.color = Color.gray;
            durationLabel.style.minWidth = 60;

            row.Add(stepLabel);
            row.Add(durationLabel);
            stepsFoldout.Add(row);
        }

        // Build Messages
        var messagesFoldout = new Foldout { text = "Build Messages", value = false };

        var errors = new VisualElement();
        var warnings = new VisualElement();
        var infos = new VisualElement();

        foreach (var step in report.steps)
        {
            foreach (var message in step.messages)
            {
                var label = new Label($"[{step.name}] {message.content}");
                label.style.whiteSpace = WhiteSpace.Normal;

                switch (message.type)
                {
                    case LogType.Error:
                    case LogType.Exception:
                        label.style.color = Color.red;
                        errors.Add(label);
                        break;
                    case LogType.Warning:
                        label.style.color = new Color(1f, 0.65f, 0f); // Orange
                        warnings.Add(label);
                        break;
                    default:
                        label.style.color = Color.gray;
                        infos.Add(label);
                        break;
                }
            }
        }

        if (errors.childCount > 0)
        {
            var errorFoldout = new Foldout { text = "Errors", value = false };
            errorFoldout.style.unityFontStyleAndWeight = FontStyle.Bold;
            errorFoldout.Add(errors);
            messagesFoldout.Add(errorFoldout);
        }

        if (warnings.childCount > 0)
        {
            var warningFoldout = new Foldout { text = "Warnings", value = false };
            warningFoldout.Add(warnings);
            messagesFoldout.Add(warningFoldout);
        }

        if (infos.childCount > 0)
        {
            var infoFoldout = new Foldout { text = "Info Logs", value = false };
            infoFoldout.Add(infos);
            messagesFoldout.Add(infoFoldout);
        }

        if (messagesFoldout.childCount == 0)
        {
            messagesFoldout.Add(new Label("No build messages found."));
        }

        scrollView.Add(stepsFoldout);
        scrollView.Add(messagesFoldout);

        return scrollView;
    }
    private static string FormatSize(ulong bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
