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

[CustomEditor(typeof(OVRManager))]
public class OVRManagerEditor : Editor
{
    private SerializedProperty _requestBodyTrackingPermissionOnStartup;
    private SerializedProperty _requestFaceTrackingPermissionOnStartup;
    private SerializedProperty _requestEyeTrackingPermissionOnStartup;
    private SerializedProperty _requestScenePermissionOnStartup;
    private SerializedProperty _requestRecordAudioPermissionOnStartup;
    private bool _expandPermissionsRequest;
    private bool _showFaceTrackingDataSources = false;

    void OnEnable()
    {
        _requestBodyTrackingPermissionOnStartup =
            serializedObject.FindProperty(nameof(OVRManager.requestBodyTrackingPermissionOnStartup));
        _requestFaceTrackingPermissionOnStartup =
            serializedObject.FindProperty(nameof(OVRManager.requestFaceTrackingPermissionOnStartup));
        _requestEyeTrackingPermissionOnStartup =
            serializedObject.FindProperty(nameof(OVRManager.requestEyeTrackingPermissionOnStartup));
        _requestScenePermissionOnStartup =
            serializedObject.FindProperty(nameof(OVRManager.requestScenePermissionOnStartup));
        _requestRecordAudioPermissionOnStartup =
            serializedObject.FindProperty(nameof(OVRManager.requestRecordAudioPermissionOnStartup));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.ApplyModifiedProperties();
        OVRRuntimeSettings runtimeSettings = OVRRuntimeSettings.GetRuntimeSettings();
        OVRProjectConfig projectConfig = OVRProjectConfig.GetProjectConfig();

#if UNITY_ANDROID
        OVRProjectConfigEditor.DrawTargetDeviceInspector(projectConfig);
        EditorGUILayout.Space();
#endif

        DrawDefaultInspector();

        bool modified = false;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
        OVRManager manager = (OVRManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);

        OVRManager.ColorSpace colorGamut = runtimeSettings.colorSpace;
        OVREditorUtil.SetupEnumField(target, new GUIContent("Color Gamut",
                "The target color gamut when displayed on the HMD"), ref colorGamut, ref modified,
            "https://developer.oculus.com/documentation/unity/unity-color-space/");
        manager.colorGamut = colorGamut;

        if (modified)
        {
            runtimeSettings.colorSpace = colorGamut;
            OVRRuntimeSettings.CommitRuntimeSettings(runtimeSettings);
        }
#endif

        EditorGUILayout.Space();
        OVRProjectConfigEditor.DrawProjectConfigInspector(projectConfig);

#if UNITY_ANDROID
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mixed Reality Capture for Quest", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        OVREditorUtil.SetupEnumField(target, "ActivationMode", ref manager.mrcActivationMode, ref modified);
        EditorGUI.indentLevel--;
#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        manager.expandMixedRealityCapturePropertySheet =
            EditorGUILayout.BeginFoldoutHeaderGroup(manager.expandMixedRealityCapturePropertySheet,
                "Mixed Reality Capture");
        OVREditorUtil.DisplayDocLink("https://developer.oculus.com/documentation/unity/unity-mrc/");
        EditorGUILayout.EndHorizontal();
        if (manager.expandMixedRealityCapturePropertySheet)
        {
            string[] layerMaskOptions = new string[32];
            for (int i = 0; i < 32; ++i)
            {
                layerMaskOptions[i] = LayerMask.LayerToName(i);
                if (layerMaskOptions[i].Length == 0)
                {
                    layerMaskOptions[i] = "<Layer " + i.ToString() + ">";
                }
            }

            EditorGUI.indentLevel++;

            OVREditorUtil.SetupBoolField(target, "Enable MixedRealityCapture", ref manager.enableMixedReality,
                ref modified);
            OVREditorUtil.SetupEnumField(target, "Composition Method", ref manager.compositionMethod, ref modified);
            OVREditorUtil.SetupLayerMaskField(target, "Extra Hidden Layers", ref manager.extraHiddenLayers,
                layerMaskOptions, ref modified);
            OVREditorUtil.SetupLayerMaskField(target, "Extra Visible Layers", ref manager.extraVisibleLayers,
                layerMaskOptions, ref modified);
            OVREditorUtil.SetupBoolField(target, "Dynamic Culling Mask", ref manager.dynamicCullingMask, ref modified);

            // CompositionMethod.External is the only composition method that is available.
            // All other deprecated composition methods should fallback to the path below.
            {
                // CompositionMethod.External
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("External Composition", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                OVREditorUtil.SetupColorField(target, "Backdrop Color (Target, Rift)",
                    ref manager.externalCompositionBackdropColorRift, ref modified);
                OVREditorUtil.SetupColorField(target, "Backdrop Color (Target, Quest)",
                    ref manager.externalCompositionBackdropColorQuest, ref modified);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
        // Multimodal hands and controllers section
#if UNITY_ANDROID
        bool launchMultimodalHandsControllersOnStartup =
            projectConfig.multimodalHandsControllersSupport != OVRProjectConfig.MultimodalHandsControllersSupport.Disabled;
        EditorGUI.BeginDisabledGroup(!launchMultimodalHandsControllersOnStartup);
        GUIContent enableConcurrentHandsAndControllersOnStartup = new GUIContent("Launch concurrent hands and controllers mode on startup",
            "Launches concurrent hands and controllers on startup for the scene. Concurrent Hands and Controllers Capability must be enabled in the project settings.");
#else
        GUIContent enableConcurrentHandsAndControllersOnStartup = new GUIContent("Enable concurrent hands and controllers mode on startup",
            "Launches concurrent hands and controllers on startup for the scene.");
#endif
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Concurrent hands and controllers", EditorStyles.boldLabel);
#if UNITY_ANDROID
        if (!launchMultimodalHandsControllersOnStartup)
        {
            EditorGUILayout.LabelField(
                "Requires Concurrent Hands and Controllers Capability to be enabled in the General section of the Quest features.",
                EditorStyles.wordWrappedLabel);
        }
#endif
        OVREditorUtil.SetupBoolField(target, enableConcurrentHandsAndControllersOnStartup,
            ref manager.launchMultimodalHandsControllersOnStartup,
            ref modified);
#if UNITY_ANDROID
        EditorGUI.EndDisabledGroup();
#endif
#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
        // Insight Passthrough section
#if UNITY_ANDROID
        bool passthroughCapabilityEnabled =
            projectConfig.insightPassthroughSupport != OVRProjectConfig.FeatureSupport.None;
        EditorGUI.BeginDisabledGroup(!passthroughCapabilityEnabled);
        GUIContent enablePassthroughContent = new GUIContent("Enable Passthrough",
            "Enables passthrough functionality for the scene. Can be toggled at runtime. Passthrough Capability must be enabled in the project settings.");
#else
        GUIContent enablePassthroughContent = new GUIContent("Enable Passthrough",
            "Enables passthrough functionality for the scene. Can be toggled at runtime.");
#endif
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Insight Passthrough", EditorStyles.boldLabel);
#if UNITY_ANDROID
        if (!passthroughCapabilityEnabled)
        {
            EditorGUILayout.LabelField(
                "Requires Passthrough Capability to be enabled in the General section of the Quest features.",
                EditorStyles.wordWrappedLabel);
        }
#endif
        OVREditorUtil.SetupBoolField(target, enablePassthroughContent, ref manager.isInsightPassthroughEnabled,
            ref modified);
#if UNITY_ANDROID
        EditorGUI.EndDisabledGroup();
#endif

        // Common Movement Tracking section header
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Movement Tracking", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        // Body Tracking settings
        EditorGUI.BeginDisabledGroup(projectConfig.bodyTrackingSupport == OVRProjectConfig.FeatureSupport.None);
        OVRPlugin.BodyTrackingFidelity2 bodyTrackingFidelity = runtimeSettings.BodyTrackingFidelity;
        OVREditorUtil.SetupEnumField(target, new GUIContent("Body Tracking Fidelity",
                "Body Tracking Fidelity defines quality of tracking"), ref bodyTrackingFidelity, ref modified,
            "https://developer.oculus.com/documentation/unity/move-body-tracking/");

        if (modified)
        {
            runtimeSettings.BodyTrackingFidelity = bodyTrackingFidelity;
            OVRRuntimeSettings.CommitRuntimeSettings(runtimeSettings);
        }

        OVRPlugin.BodyJointSet bodyTrackingJointSet = runtimeSettings.BodyTrackingJointSet;
        OVREditorUtil.SetupEnumField(target, new GUIContent("Body Tracking Joint Set",
                "Body Tracking Joint Set"), ref bodyTrackingJointSet, ref modified,
            "https://developer.oculus.com/documentation/unity/move-body-tracking/");
        if (modified)
        {
            runtimeSettings.BodyTrackingJointSet = bodyTrackingJointSet;
            OVRRuntimeSettings.CommitRuntimeSettings(runtimeSettings);
        }
        EditorGUI.EndDisabledGroup();

        // Face Tracking settings
        EditorGUI.BeginDisabledGroup(projectConfig.faceTrackingSupport == OVRProjectConfig.FeatureSupport.None);

        _showFaceTrackingDataSources = EditorGUILayout.Foldout(
            _showFaceTrackingDataSources,
            new GUIContent("Face Tracking Data Sources", "Specifies the face tracking data sources accepted by the application.\n\n" +
                "Requires Face Tracking Support to be \"Required\" or \"Supported\" in the General section of Quest Features."));

        bool visualFaceTracking = runtimeSettings.RequestsVisualFaceTracking;
        bool audioFaceTracking = runtimeSettings.RequestsAudioFaceTracking;
        bool dataSourceSelected = visualFaceTracking || audioFaceTracking;
        // Show the warning under the foldout, even if the foldout is collapsed.
        if (projectConfig.faceTrackingSupport != OVRProjectConfig.FeatureSupport.None && !dataSourceSelected)
        {
            EditorGUILayout.HelpBox("Please specify at least one face tracking data source. If you don't choose, all available data sources will be chosen.", MessageType.Warning, true);
        }

        if (_showFaceTrackingDataSources)
        {
            EditorGUI.indentLevel++;
            OVREditorUtil.SetupBoolField(
                target,
                new GUIContent("Visual", "Estimate face expressions with visual or audiovisual data."),
                ref visualFaceTracking,
                ref modified);
            OVREditorUtil.SetupBoolField(
                target,
                new GUIContent("Audio", "Estimate face expressions using audio data only."),
                ref audioFaceTracking,
                ref modified);
            EditorGUI.indentLevel--;
        }

        if (modified)
        {
            runtimeSettings.RequestsVisualFaceTracking = visualFaceTracking;
            runtimeSettings.RequestsAudioFaceTracking = audioFaceTracking;
            OVRRuntimeSettings.CommitRuntimeSettings(runtimeSettings);
        }
        EditorGUI.EndDisabledGroup();

        // Common Movement Tracking section footer
        EditorGUI.indentLevel--;

#endif

        #region PermissionRequests

        EditorGUILayout.Space();
        _expandPermissionsRequest =
            EditorGUILayout.BeginFoldoutHeaderGroup(_expandPermissionsRequest, "Permission Requests On Startup");
        if (_expandPermissionsRequest)
        {
            void AddPermissionGroup(bool featureEnabled, string permissionName, string capabilityName, SerializedProperty property)
            {
                using (new EditorGUI.DisabledScope(!featureEnabled))
                {
                    if (!featureEnabled)
                    {
                        EditorGUILayout.LabelField(
                            $"Requires {capabilityName} Capability to be enabled in the Quest features section.",
                            EditorStyles.wordWrappedLabel);
                    }

                    var label = new GUIContent(permissionName,
                        $"Requests {permissionName} permission on start up. {capabilityName} Capability must be enabled in the project settings.");
                    EditorGUILayout.PropertyField(property, label);
                }
            }

            AddPermissionGroup(projectConfig.bodyTrackingSupport != OVRProjectConfig.FeatureSupport.None,
                "Body Tracking", "Body Tracking", _requestBodyTrackingPermissionOnStartup);
            AddPermissionGroup(projectConfig.faceTrackingSupport != OVRProjectConfig.FeatureSupport.None,
                "Face Tracking", "Face Tracking", _requestFaceTrackingPermissionOnStartup);
            AddPermissionGroup(projectConfig.eyeTrackingSupport != OVRProjectConfig.FeatureSupport.None,
                "Eye Tracking", "Eye Tracking", _requestEyeTrackingPermissionOnStartup);
            AddPermissionGroup(projectConfig.sceneSupport != OVRProjectConfig.FeatureSupport.None,
                "Scene", "Scene", _requestScenePermissionOnStartup);
            AddPermissionGroup(projectConfig.faceTrackingSupport != OVRProjectConfig.FeatureSupport.None,
                "Record Audio for audio based Face Tracking", "Face Tracking", _requestRecordAudioPermissionOnStartup);
        }

        EditorGUILayout.EndFoldoutHeaderGroup();

#endregion


        if (modified)
        {
            EditorUtility.SetDirty(target);
        }

        serializedObject.ApplyModifiedProperties();

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
#if !OCULUS_XR_3_3_0_OR_NEWER || UNITY_2020
        if (manager.enableDynamicResolution && !PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android))
        {
            UnityEngine.Rendering.GraphicsDeviceType[] apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            if (apis.Length >= 1 && apis[0] == UnityEngine.Rendering.GraphicsDeviceType.Vulkan)
            {
                Debug.LogError("Vulkan Dynamic Resolution is not supported on your current build version. Ensure you are on Unity 2021+ with Oculus XR plugin v3.3.0+");
                manager.enableDynamicResolution = false;
            }
        }
#endif
#endif
    }
}
