/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;
using Oculus.VR.Editor;

#if USING_XR_SDK_OPENXR
using UnityEngine.XR.OpenXR;
using Meta.XR;
using UnityEditor.XR.Management;
#endif

public class OVRManifestPreprocessor
{
    private static readonly string ManifestFileName = "AndroidManifest.xml";
    private static readonly string ManifestFolderName = "Plugins/Android";

    private static readonly string ManifestFolderPathAbsolute =
        Path.Combine(Application.dataPath, ManifestFolderName);

    private static readonly string BuildManifestFolderPathRelative =
        Path.Combine("Assets", ManifestFolderName);

    private static readonly string BuildManifestFilePathAbsolute =
        Path.Combine(ManifestFolderPathAbsolute, ManifestFileName);

    private static readonly string BuildManifestFilePathRelative =
        Path.Combine(BuildManifestFolderPathRelative, ManifestFileName);

    [MenuItem("Meta/Tools/Create store-compatible AndroidManifest.xml", false, 100000)]
    public static void GenerateManifestForSubmission()
    {
        var so = ScriptableObject.CreateInstance(typeof(OVRPluginInfo));
        var script = MonoScript.FromScriptableObject(so);
        string assetPath = AssetDatabase.GetAssetPath(script);
        string editorDir = Directory.GetParent(assetPath).FullName;
        string srcFile = editorDir + "/AndroidManifest.OVRSubmission.xml";

        if (!File.Exists(srcFile))
        {
            Debug.LogError("Cannot find Android manifest template for submission." +
                           " Please delete the OVR folder and reimport the Oculus Utilities.");
            return;
        }

        if (DoesAndroidManifestExist())
        {
            if (!EditorUtility.DisplayDialog("AndroidManifest.xml Already Exists!",
                    "Would you like to replace the existing manifest with a new one? All modifications will be lost.",
                    "Replace", "Cancel"))
            {
                return;
            }
        }

        // IO methods use absolute paths
        if (!Directory.Exists(ManifestFolderPathAbsolute))
            Directory.CreateDirectory(ManifestFolderPathAbsolute);

        PatchAndroidManifest(srcFile, BuildManifestFilePathAbsolute, false);
        AssetDatabase.Refresh();
        ShowAndroidManifestInProject();
    }

    public static bool DoesAndroidManifestExist()
    {
        // IO methods use absolute paths
        return File.Exists(BuildManifestFilePathAbsolute);
    }

    [MenuItem("Meta/Tools/Update AndroidManifest.xml", false, 100000)]
    public static void UpdateAndroidManifest()
    {
        if (!DoesAndroidManifestExist())
        {
            Debug.LogError(
                "Unable to update manifest because it does not exist! Run \"Create store-compatible AndroidManifest.xml\" first");
            return;
        }

        if (!EditorUtility.DisplayDialog("Update AndroidManifest.xml",
                "This will overwrite all Oculus specific AndroidManifest Settings. Continue?", "Overwrite", "Cancel"))
        {
            return;
        }

        PatchAndroidManifest(BuildManifestFilePathAbsolute, skipExistingAttributes: false);
        AssetDatabase.Refresh();
        ShowAndroidManifestInProject();
    }

    [MenuItem("Meta/Tools/Remove AndroidManifest.xml", false, 100000)]
    public static void RemoveAndroidManifest()
    {
        // AssetDatabase functions uses relative paths
        AssetDatabase.DeleteAsset(BuildManifestFilePathRelative);
        AssetDatabase.Refresh();
    }

    private static void ShowAndroidManifestInProject()
    {
        if (!DoesAndroidManifestExist())
            return;

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(BuildManifestFilePathRelative);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    private static void AddOrRemoveTag(XmlDocument doc, string @namespace, string path, string elementName, string name,
        bool required, bool modifyIfFound, params string[] attrs) // name, value pairs
    {
        var nodes = doc.SelectNodes(path + "/" + elementName);
        XmlElement element = null;
        foreach (XmlElement e in nodes)
        {
            if (name == null || name == e.GetAttribute("name", @namespace))
            {
                element = e;
                break;
            }
        }

        if (required)
        {
            if (element == null)
            {
                var parent = doc.SelectSingleNode(path);
                element = doc.CreateElement(elementName);
                element.SetAttribute("name", @namespace, name);
                parent.AppendChild(element);
            }

            for (int i = 0; i < attrs.Length; i += 2)
            {
                if (modifyIfFound || string.IsNullOrEmpty(element.GetAttribute(attrs[i], @namespace)))
                {
                    if (attrs[i + 1] != null)
                    {
                        element.SetAttribute(attrs[i], @namespace, attrs[i + 1]);
                    }
                    else
                    {
                        element.RemoveAttribute(attrs[i], @namespace);
                    }
                }
            }
        }
        else
        {
            if (element != null && modifyIfFound)
            {
                element.ParentNode.RemoveChild(element);
            }
        }
    }

    private static void AddReplaceValueTag(XmlDocument doc, string @namespace, string path, string elementName, string name)
    {
        XmlElement element = (XmlElement)doc.SelectSingleNode("/manifest");
        if (element == null)
        {
            UnityEngine.Debug.LogError("Could not find manifest tag in android manifest.");
            return;
        }

        string toolsNamespace = element.GetAttribute("xmlns:tools");
        if (string.IsNullOrEmpty(toolsNamespace))
        {
            toolsNamespace = "http://schemas.android.com/tools";
            element.SetAttribute("xmlns:tools", toolsNamespace);
        }

        var nodes = doc.SelectNodes(path + "/" + elementName);
        foreach (XmlElement e in nodes)
        {
            if (name == null || name == e.GetAttribute("name", @namespace))
            {
                element = e;
                break;
            }
        }

        element.SetAttribute("replace", toolsNamespace, "android:value");
    }

    public static void PatchAndroidManifest(string sourceFile, string destinationFile = null,
        bool skipExistingAttributes = true, bool enableSecurity = false)
    {
        if (destinationFile == null)
        {
            destinationFile = sourceFile;
        }

        bool modifyIfFound = !skipExistingAttributes;

        try
        {
            // Load android manifest file
            var doc = new XmlDocument();
            doc.Load(sourceFile);

            string androidNamespaceURI;
            XmlElement element = (XmlElement)doc.SelectSingleNode("/manifest");
            if (element == null)
            {
                UnityEngine.Debug.LogError("Could not find manifest tag in android manifest.");
                return;
            }

            // Get android namespace URI from the manifest
            androidNamespaceURI = element.GetAttribute("xmlns:android");
            if (string.IsNullOrEmpty(androidNamespaceURI))
            {
                UnityEngine.Debug.LogError("Could not find Android Namespace in manifest.");
                return;
            }

#if UNITY_2023_2_OR_NEWER
            // replace UnityPlayerActivity to UnityPlayerGameActivity
            XmlElement activityNode = doc.SelectSingleNode("/manifest/application/activity") as XmlElement;
            string activityName = activityNode.GetAttribute("name", androidNamespaceURI);
            if (activityName == "com.unity3d.player.UnityPlayerActivity")
            {
                activityNode.SetAttribute("name", androidNamespaceURI, "com.unity3d.player.UnityPlayerGameActivity");
            }

            // use a Theme.AppCompat theme for the compatbility with UnityPlayerGameActivity
            string activityTheme = activityNode.GetAttribute("theme", androidNamespaceURI);
            if (activityTheme == "@android:style/Theme.Black.NoTitleBar.Fullscreen")
            {
                activityNode.SetAttribute("theme", androidNamespaceURI, "@style/Theme.AppCompat.DayNight.NoActionBar");
            }
#endif

            ApplyRequiredManifestTags(doc, androidNamespaceURI, modifyIfFound, enableSecurity);
            ApplyFeatureManifestTags(doc, androidNamespaceURI, modifyIfFound);

            // The following manifest entries are all handled through Oculus XR SDK Plugin
#if !PRIORITIZE_OCULUS_XR_SETTINGS
            ApplyOculusXRManifestTags(doc, androidNamespaceURI, modifyIfFound);
            ApplyTargetDevicesManifestTags(doc, androidNamespaceURI, true /*modifyIfFound*/);
#endif


            var settings = new XmlWriterSettings
            {
                NewLineChars = GetSuggestedLineEnding(sourceFile),
                Indent = true,
            };
            using var writer = XmlWriter.Create(destinationFile, settings);
            doc.Save(writer);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogException(e);
        }

        // see if the current file uses \r\n or \n by scanning the first line
        // the first line in the xml file is ~50 chars
        string GetSuggestedLineEnding(string filepath)
        {
            using var reader = File.OpenRead(filepath);
            var prevChar = 'a';
            for (var i = 0; i < 256; i++)
            {
                var cur = reader.ReadByte();
                if (cur == -1) break;
                var curChar = (char)cur;
                if (curChar == '\n')
                    return prevChar == '\r' ? "\r\n" : "\n";
                prevChar = curChar;
            }
            return System.Environment.NewLine;
        }
    }



    private static void ApplyRequiredManifestTags(XmlDocument doc, string androidNamespaceURI, bool modifyIfFound,
        bool enableSecurity)
    {
        OVRProjectConfig projectConfig = OVRProjectConfig.CachedProjectConfig;

        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest/application/activity/intent-filter",
            "category",
            "android.intent.category.LEANBACK_LAUNCHER",
            required: false,
            modifyIfFound: true); // always remove leanback launcher

        // First add or remove headtracking flag if targeting Quest
        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-feature",
            "android.hardware.vr.headtracking",
            OVRDeviceSelector.isTargetDeviceQuestFamily,
            true,
            "version", "1",
            "required", OVRProjectConfig.CachedProjectConfig.allowOptional3DofHeadTracking ? "false" : "true");

        // make sure android label and icon are set in the manifest
        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "application",
            null,
            true,
            modifyIfFound,
            "label", "@string/app_name",
            "icon", "@mipmap/app_icon",
            // Disable allowBackup in manifest and add Android NSC XML file
            "allowBackup", projectConfig.disableBackups ? "false" : "true",
            "networkSecurityConfig", projectConfig.enableNSCConfig && enableSecurity ? "@xml/network_sec_config" : null
        );
    }

    private static void ApplyFeatureManifestTags(XmlDocument doc, string androidNamespaceURI, bool modifyIfFound)
    {
        OVRProjectConfig projectConfig = OVRProjectConfig.CachedProjectConfig;
        OVRRuntimeSettings runtimeSettings = OVRRuntimeSettings.GetRuntimeSettings();

        //============================================================================
        // Hand Tracking
        // If Quest is the target device, add the handtracking manifest tags if needed
        // Mapping of project setting to manifest setting:
        // OVRProjectConfig.HandTrackingSupport.ControllersOnly => manifest entry not present
        // OVRProjectConfig.HandTrackingSupport.ControllersAndHands => manifest entry present and required=false
        // OVRProjectConfig.HandTrackingSupport.HandsOnly => manifest entry present and required=true
        OVRProjectConfig.HandTrackingSupport targetHandTrackingSupport =
            OVRProjectConfig.CachedProjectConfig.handTrackingSupport;
        OVRProjectConfig.HandTrackingVersion targetHandTrackingVersion =
            OVRProjectConfig.CachedProjectConfig.handTrackingVersion;
        bool handTrackingEntryNeeded = OVRDeviceSelector.isTargetDeviceQuestFamily &&
                                       (targetHandTrackingSupport !=
                                        OVRProjectConfig.HandTrackingSupport.ControllersOnly);
        bool handTrackingVersionEntryNeeded = handTrackingEntryNeeded &&
                                              (targetHandTrackingVersion !=
                                               OVRProjectConfig.HandTrackingVersion.Default);
        string handTrackingVersionValue =
            (targetHandTrackingVersion == OVRProjectConfig.HandTrackingVersion.V2) ? "V2.0" : "V1.0";

        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-feature",
            "oculus.software.handtracking",
            handTrackingEntryNeeded,
            modifyIfFound,
            "required",
            (targetHandTrackingSupport == OVRProjectConfig.HandTrackingSupport.HandsOnly) ? "true" : "false");
        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-permission",
            "com.oculus.permission.HAND_TRACKING",
            handTrackingEntryNeeded,
            modifyIfFound);

        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest/application",
            "meta-data",
            "com.oculus.handtracking.frequency",
            handTrackingEntryNeeded,
            modifyIfFound,
            "value", projectConfig.handTrackingFrequency.ToString());

        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest/application",
            "meta-data",
            "com.oculus.handtracking.version",
            handTrackingVersionEntryNeeded,
            modifyIfFound,
            "value", handTrackingVersionValue);

        //============================================================================
        // System Keyboard
        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-feature",
            "oculus.software.overlay_keyboard",
            projectConfig.requiresSystemKeyboard,
            modifyIfFound,
            "required", "false");

        //============================================================================
        // Experimental Features
        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-feature",
            "com.oculus.experimental.enabled",
            projectConfig.experimentalFeaturesEnabled,
            modifyIfFound,
            "required", "true");

        //============================================================================
        // Anchor
        OVRProjectConfig.AnchorSupport targetAnchorSupport = OVRProjectConfig.CachedProjectConfig.anchorSupport;
        var sceneSupport = OVRProjectConfig.CachedProjectConfig.sceneSupport;
        bool anchorEntryNeeded = OVRDeviceSelector.isTargetDeviceQuestFamily &&
                                 (targetAnchorSupport == OVRProjectConfig.AnchorSupport.Enabled ||
                                  sceneSupport != OVRProjectConfig.FeatureSupport.None);

        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-permission",
            "com.oculus.permission.USE_ANCHOR_API",
            anchorEntryNeeded,
            modifyIfFound);

        var targetSharedAnchorSupport = OVRProjectConfig.CachedProjectConfig.sharedAnchorSupport;
        bool sharedAnchorEntryNeeded = OVRDeviceSelector.isTargetDeviceQuestFamily &&
                                       targetSharedAnchorSupport != OVRProjectConfig.FeatureSupport.None;

        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-permission",
            "com.oculus.permission.IMPORT_EXPORT_IOT_MAP_DATA",
            sharedAnchorEntryNeeded,
            modifyIfFound);



        //============================================================================
        // Passthrough
        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-feature",
            "com.oculus.feature.PASSTHROUGH",
            projectConfig.insightPassthroughSupport != OVRProjectConfig.FeatureSupport.None,
            modifyIfFound,
            "required", projectConfig.insightPassthroughSupport.ToRequiredAttributeValue());

        //============================================================================
        // System Splash Screen
        if (projectConfig.systemSplashScreen != null)
        {
            AddOrRemoveTag(doc,
                androidNamespaceURI,
                "/manifest/application",
                "meta-data",
                "com.oculus.ossplash",
                true,
                true /*modifyIfFound*/,
                "value",
                "true");
            AddOrRemoveTag(doc,
                androidNamespaceURI,
                "/manifest/application",
                "meta-data",
                "com.oculus.ossplash.type",
                true,
                true /*modifyIfFound*/,
                "value",
                projectConfig.systemSplashScreenType.ToManifestTag());
            AddOrRemoveTag(doc,
                androidNamespaceURI,
                "/manifest/application",
                "meta-data",
                "com.oculus.ossplash.colorspace",
                true,
                true /*modifyIfFound*/,
                "value",
                ColorSpaceToManifestTag(runtimeSettings.colorSpace));
        }

        // Contextual Passthrough
        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest/application",
            "meta-data",
            "com.oculus.ossplash.background",
            required: true,
            true /*modifyIfFound*/,
            "value",
            projectConfig.systemLoadingScreenBackground == OVRProjectConfig.SystemLoadingScreenBackground.ContextualPassthrough
                ? "passthrough-contextual"
                : "black");

        //============================================================================
        // Render Model
        OVRProjectConfig.RenderModelSupport renderModelSupport = OVRProjectConfig.CachedProjectConfig.renderModelSupport;
        bool renderModelEntryNeeded = OVRDeviceSelector.isTargetDeviceQuestFamily &&
                                      (renderModelSupport == OVRProjectConfig.RenderModelSupport.Enabled);

        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-feature",
            "com.oculus.feature.RENDER_MODEL",
            renderModelEntryNeeded,
            modifyIfFound);
        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-permission",
            "com.oculus.permission.RENDER_MODEL",
            renderModelEntryNeeded,
            modifyIfFound);

        //============================================================================
        // Tracked Keyboard
        // If Quest is the target device, add the tracked keyboard manifest tags if needed
        // Mapping of project setting to manifest setting:
        // OVRProjectConfig.TrackedKeyboardSupport.None => manifest entry not present
        // OVRProjectConfig.TrackedKeyboardSupport.Supported => manifest entry present and required=false
        // OVRProjectConfig.TrackedKeyboardSupport.Required => manifest entry present and required=true
        OVRProjectConfig.TrackedKeyboardSupport targetTrackedKeyboardSupport =
            OVRProjectConfig.CachedProjectConfig.trackedKeyboardSupport;
        bool trackedKeyboardEntryNeeded = OVRDeviceSelector.isTargetDeviceQuestFamily &&
                                          (targetTrackedKeyboardSupport !=
                                           OVRProjectConfig.TrackedKeyboardSupport.None);

        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-feature",
            "oculus.software.trackedkeyboard",
            trackedKeyboardEntryNeeded,
            modifyIfFound,
            "required",
            (targetTrackedKeyboardSupport == OVRProjectConfig.TrackedKeyboardSupport.Required) ? "true" : "false");
        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-permission",
            "com.oculus.permission.TRACKED_KEYBOARD",
            trackedKeyboardEntryNeeded,
            modifyIfFound);

        //============================================================================
        // Body Tracking
        // If Quest is the target device, add the bodytracking manifest tags if needed
        var targetBodyTrackingSupport = OVRProjectConfig.CachedProjectConfig.bodyTrackingSupport;
        bool bodyTrackingEntryNeeded = OVRDeviceSelector.isTargetDeviceQuestFamily &&
                                       (targetBodyTrackingSupport != OVRProjectConfig.FeatureSupport.None);

        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-feature",
            "com.oculus.software.body_tracking",
            bodyTrackingEntryNeeded,
            (targetBodyTrackingSupport == OVRProjectConfig.FeatureSupport.Required)
                ? true
                : modifyIfFound, // If Required, we should override the current entry
            "required", (targetBodyTrackingSupport == OVRProjectConfig.FeatureSupport.Required) ? "true" : "false");
        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-permission",
            OVRPermissionsRequester.GetPermissionId(OVRPermissionsRequester.Permission.BodyTracking),
            bodyTrackingEntryNeeded,
            modifyIfFound);

        //============================================================================
        // Face Tracking
        var targetFaceTrackingSupport = OVRProjectConfig.CachedProjectConfig.faceTrackingSupport;
        bool faceTrackingEntryNeeded = OVRDeviceSelector.isTargetDeviceQuestFamily &&
                                       (targetFaceTrackingSupport != OVRProjectConfig.FeatureSupport.None);

        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-feature",
            "oculus.software.face_tracking",
            faceTrackingEntryNeeded,
            (targetFaceTrackingSupport == OVRProjectConfig.FeatureSupport.Required)
                ? true
                : modifyIfFound, // If Required, we should override the current entry
            "required", (targetFaceTrackingSupport == OVRProjectConfig.FeatureSupport.Required) ? "true" : "false");
        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-permission",
            OVRPermissionsRequester.GetPermissionId(OVRPermissionsRequester.Permission.FaceTracking),
            faceTrackingEntryNeeded,
            modifyIfFound);
        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-permission",
            OVRPermissionsRequester.GetPermissionId(OVRPermissionsRequester.Permission.RecordAudio),
            faceTrackingEntryNeeded, // audio recording for audio based face tracking
            modifyIfFound);

        //============================================================================
        // Eye Tracking
        var targetEyeTrackingSupport = OVRProjectConfig.CachedProjectConfig.eyeTrackingSupport;
#if USING_XR_SDK_OPENXR
        if (IsOpenXRLoaderActive())
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
            if (settings != null)
            {
                var foveationFeature = settings.GetFeature<MetaXREyeTrackedFoveationFeature>();
                if (foveationFeature.enabled && targetEyeTrackingSupport == OVRProjectConfig.FeatureSupport.None)
                {
                    targetEyeTrackingSupport = OVRProjectConfig.FeatureSupport.Supported;
                }
            }
        }
#endif
        bool eyeTrackingEntryNeeded = OVRDeviceSelector.isTargetDeviceQuestFamily &&
                                    (targetEyeTrackingSupport != OVRProjectConfig.FeatureSupport.None);

        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-feature",
            "oculus.software.eye_tracking",
            eyeTrackingEntryNeeded,
            (targetEyeTrackingSupport == OVRProjectConfig.FeatureSupport.Required)
                ? true
                : modifyIfFound, // If Required, we should override the current entry
            "required", (targetEyeTrackingSupport == OVRProjectConfig.FeatureSupport.Required) ? "true" : "false");
        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-permission",
            OVRPermissionsRequester.GetPermissionId(OVRPermissionsRequester.Permission.EyeTracking),
            eyeTrackingEntryNeeded,
            modifyIfFound);

        //============================================================================
        // Virtual Keyboard
        var virtualKeyboardSupport = OVRProjectConfig.CachedProjectConfig.virtualKeyboardSupport;
        bool virtualKeyboardEntryNeeded = OVRDeviceSelector.isTargetDeviceQuestFamily &&
                                          (virtualKeyboardSupport != OVRProjectConfig.FeatureSupport.None);

        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-feature",
            "com.oculus.feature.VIRTUAL_KEYBOARD",
            virtualKeyboardEntryNeeded,
            (virtualKeyboardSupport == OVRProjectConfig.FeatureSupport.Required)
                ? true
                : modifyIfFound, // If Required, we should override the current entry
            "required", (virtualKeyboardSupport == OVRProjectConfig.FeatureSupport.Required) ? "true" : "false");

        //============================================================================
        // Scene
        bool sceneEntryNeeded = OVRDeviceSelector.isTargetDeviceQuestFamily &&
                                (sceneSupport != OVRProjectConfig.FeatureSupport.None);

        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-permission",
            OVRPermissionsRequester.GetPermissionId(OVRPermissionsRequester.Permission.Scene),
            sceneEntryNeeded,
            modifyIfFound);

        //============================================================================
        // Boundary Visibility
        var boundaryVisibilitySupport = OVRProjectConfig.CachedProjectConfig.boundaryVisibilitySupport;
        bool boundaryVisibilityEntryNeeded = OVRDeviceSelector.isTargetDeviceQuestFamily &&
                                (boundaryVisibilitySupport != OVRProjectConfig.FeatureSupport.None);

        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest",
            "uses-permission",
            "com.oculus.permission.BOUNDARY_VISIBILITY",
            boundaryVisibilityEntryNeeded,
            modifyIfFound);

        //============================================================================
        // Processor Favor
        var processorFavor = OVRProjectConfig.CachedProjectConfig.processorFavor;
        bool tradeCpuForGpuAmountNeeded = processorFavor != OVRProjectConfig.ProcessorFavor.FavorEqually;

        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest/application",
            "meta-data",
            "com.oculus.trade_cpu_for_gpu_amount",
            required: tradeCpuForGpuAmountNeeded,
            modifyIfFound: true,
            "value", ((int)processorFavor).ToString());

        //============================================================================
        // Telemetry Project GUID
        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest/application",
            "meta-data",
            "com.oculus.telemetry.project_guid",
            required: true,
            modifyIfFound: true,
            "value",
            runtimeSettings.TelemetryProjectGuid);
    }



    private static void ApplyOculusXRManifestTags(XmlDocument doc, string androidNamespaceURI, bool modifyIfFound)
    {
        // Add focus aware tag if this app is targeting Quest Family
        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest/application/activity",
            "meta-data",
            "com.oculus.vr.focusaware",
            OVRDeviceSelector.isTargetDeviceQuestFamily,
            modifyIfFound,
            "value", "true");

        // Add VR intent filter tag in the manifest
        AddOrRemoveTag(doc,
            androidNamespaceURI,
            "/manifest/application/activity/intent-filter",
            "category",
            "com.oculus.intent.category.VR",
            required: true,
            modifyIfFound: true);
    }

    private static void ApplyTargetDevicesManifestTags(XmlDocument doc, string androidNamespaceURI, bool modifyIfFound)
    {
        // Add support devices manifest according to the target devices
        if (OVRDeviceSelector.isTargetDeviceQuestFamily)
        {
            string targetDeviceValue = "";
            if (OVRDeviceSelector.isTargetDeviceQuest)
            {
                if (string.IsNullOrEmpty(targetDeviceValue))
                    targetDeviceValue = "quest";
                else
                    targetDeviceValue += "|quest";
            }

            if (OVRDeviceSelector.isTargetDeviceQuest2)
            {
                if (string.IsNullOrEmpty(targetDeviceValue))
                    targetDeviceValue = "quest2";
                else
                    targetDeviceValue += "|quest2";
            }

            if (OVRDeviceSelector.isTargetDeviceQuestPro)
            {
                if (string.IsNullOrEmpty(targetDeviceValue))
                    targetDeviceValue = "questpro";
                else
                    targetDeviceValue += "|questpro";
            }
            if (OVRDeviceSelector.isTargetDeviceQuest3)
            {
                if (string.IsNullOrEmpty(targetDeviceValue))
                    targetDeviceValue = "eureka";
                else
                    targetDeviceValue += "|eureka";
            }
            if (string.IsNullOrEmpty(targetDeviceValue))
            {
                Debug.LogError("Empty target devices");
            }

            AddOrRemoveTag(doc,
                androidNamespaceURI,
                "/manifest/application",
                "meta-data",
                "com.oculus.supportedDevices",
                true,
                modifyIfFound,
                "value", targetDeviceValue);

#if XR_MGMT_4_4_0_OR_NEWER && USING_XR_SDK_OPENXR
            // Fixes a manifest merge edge case where the supported devices tag collides with a cached version when using new XR Management manifest system
            AddReplaceValueTag(doc,
                androidNamespaceURI,
                "/manifest/application",
                "meta-data",
                "com.oculus.supportedDevices");
#endif
        }
    }

    private static bool IsOpenXRLoaderActive()
    {
#if USING_XR_SDK_OPENXR
        var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
        if (settings.Manager.activeLoaders.Count > 0)
        {
            var openXRLoader = settings.Manager.activeLoaders[0] as OpenXRLoader;
            return openXRLoader != null;
        }
#endif
        return false;
    }

    private static string ColorSpaceToManifestTag(OVRManager.ColorSpace colorSpace)
    {
        switch (colorSpace)
        {
            case OVRManager.ColorSpace.Unmanaged:
                return "!Unmanaged";
            case OVRManager.ColorSpace.Rec_2020:
                return "Rec.2020";
            case OVRManager.ColorSpace.Rec_709:
                return "Rec.709";
            case OVRManager.ColorSpace.Rift_CV1:
                return "!RiftCV1";
            case OVRManager.ColorSpace.Rift_S:
                return "!RiftS";
            case OVRManager.ColorSpace.Quest:
                return "!Quest";
            case OVRManager.ColorSpace.P3:
                return "P3";
            case OVRManager.ColorSpace.Adobe_RGB:
                return "Adobe";
            default:
                return "";
        }
    }
}
