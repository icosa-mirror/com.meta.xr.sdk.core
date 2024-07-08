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

#if USING_META_XR_PLATFORM_SDK

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Oculus.Platform;
using static Meta.XR.Editor.UserInterface.Styles;

namespace Meta.XR.GuidedSetups.Editor
{
    public class GuidedSetupAccount : GuidedSetupBase
    {
        private const string MetaDashboardURL = "https://developer.oculus.com/manage";
        private const string AppIDRetrieveDocURL = "https://developer.oculus.com/documentation/unity/unity-platform-entitlements/#retrieve-the-appid-from-the-developer-portal";
        private const string CreateOrgDocURL = "https://developer.oculus.com/resources/publish-account-management-intro/";
        private const string AddTestUserDocURL = "https://developer.oculus.com/resources/test-users/";
        private const string AddPlatformFeaturesDocURL = "https://developer.oculus.com/documentation/unity/unity-shared-spatial-anchors/#prerequisites";

        private string _appId = "Paste you App Id here";
        private bool _appIdSet;

        [MenuItem("Oculus/Tools/Meta Account Setup Guide")]
        private static void ShowWindow()
        {
            ShowWindow(Utils.TriggerSource.Menu);
        }

        public static void ShowWindow(Utils.TriggerSource source)
        {
            var window = GetWindow<GuidedSetupAccount>();

            var title = "Meta Account Setup Guide";
            var description = "This will assist you in setting up your Meta developer account and guide you to retrieve the AppID to use it in your project.";
            window.SetupWindow(title, description);

            OVRTelemetry.Start(OVRTelemetryConstants.GuidedSetup.MarkerId.OpenSSAWindow)
                .AddAnnotation(OVRTelemetryConstants.GuidedSetup.AnnotationType.ActionTrigger, source.ToString())
                .AddAnnotation(OVRTelemetryConstants.GuidedSetup.AnnotationType.HasAppId, HasAppId().ToString())
                .Send();
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            // Instructions
            EditorGUILayout.BeginVertical(GuidedSetupStyles.GUIStyles.ContentMargin);

            OpenURLGUI();
            SetAppIdGUI();
            DataUseCheckGUI();
            TestUserAddGUI();

            GUILayout.FlexibleSpace();

            // Basic confirmation
            OnAppIDSetGUI();


            // Close & Platform settings
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Platform Settings"))
            {
                EditorApplication.ExecuteMenuItem("Oculus/Platform/Edit Settings");
            }
            if (GUILayout.Button("Close"))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void OpenURLGUI()
        {
            EditorGUILayout.BeginHorizontal();

            GUIContent text = new("Click here to open");
            float width = new GUIStyle(EditorStyles.label).CalcSize(text).x - Constants.TextWidthOffset;

            using (new Meta.XR.Editor.UserInterface.Utils.ColorScope(Meta.XR.Editor.UserInterface.Utils.ColorScope.Scope.Content, Colors.LightGray))
            {
                EditorGUILayout.LabelField(GuidedSetupStyles.Contents.DefaultIcon, GuidedSetupStyles.GUIStyles.IconStyleTopPadding, GUILayout.Width(Constants.SmallIconSize),
                    GUILayout.Height(Constants.SmallIconSize + Constants.Padding));
            }

            EditorGUILayout.LabelField(text, GuidedSetupStyles.GUIStyles.LabelTopPadding, GUILayout.Width(width));
            if (EditorGUILayout.LinkButton("Meta Quest Developer Dashboard"))
            {
                OpenURL(MetaDashboardURL, nameof(GuidedSetupAccount));
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void SetAppIdGUI()
        {
            var color = HasAppId() ? Colors.LightGray : Colors.WarningColor;

            var labelStyle = new GUIStyle(EditorStyles.label);
            var t0 = new GUIContent("Follow the steps to");
            var w0 = labelStyle.CalcSize(t0).x - Constants.TextWidthOffset;

            var t1 = new GUIContent("If you already have an Organization follow these");
            var w1 = labelStyle.CalcSize(t1).x - Constants.TextWidthOffset;

            var link0 = new GUIContent("create an Organization.");
            var link1 = new GUIContent("steps to retrieve AppID.");

            EditorGUILayout.BeginVertical(GuidedSetupStyles.GUIStyles.TopMargin);
            EditorGUILayout.BeginHorizontal();

            using (new Meta.XR.Editor.UserInterface.Utils.ColorScope(Meta.XR.Editor.UserInterface.Utils.ColorScope.Scope.Content, color))
            {
                EditorGUILayout.LabelField(GuidedSetupStyles.Contents.DefaultIcon, GuidedSetupStyles.GUIStyles.IconStyleTopPadding, GUILayout.Width(Constants.SmallIconSize),
                    GUILayout.Height(Constants.SmallIconSize + Constants.Padding));
            }

            EditorGUILayout.LabelField(t0, GuidedSetupStyles.GUIStyles.LabelTopPadding, GUILayout.Width(w0));
            if (EditorGUILayout.LinkButton(link0))
            {
                OpenURL(CreateOrgDocURL, nameof(GuidedSetupAccount));
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(22);
            EditorGUILayout.LabelField(t1, GuidedSetupStyles.GUIStyles.LabelTopPadding, GUILayout.Width(w1));
            if (EditorGUILayout.LinkButton(link1))
            {
                OpenURL(AppIDRetrieveDocURL, nameof(GuidedSetupAccount));
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // App id field
            EditorGUILayout.BeginHorizontal(GuidedSetupStyles.GUIStyles.TopMargin);
            GUILayout.Space(20);
            _appId = EditorGUILayout.TextField(_appId);
            var invalid = !_appId.All(char.IsDigit) || String.IsNullOrEmpty(_appId);

            if (GUILayout.Button("Set") && !invalid)
            {
                PlatformSettings.MobileAppID = _appId;
                PlatformSettings.AppID = _appId;
                EditorApplication.ExecuteMenuItem("Oculus/Platform/Edit Settings");
                _appIdSet = true;

                OVRTelemetry.Start(OVRTelemetryConstants.GuidedSetup.MarkerId.SetAppIdFromGuidedSetup).Send();
            }

            EditorGUILayout.EndHorizontal();

            // Validation
            if (invalid)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(22);
                using (new Meta.XR.Editor.UserInterface.Utils.ColorScope(Meta.XR.Editor.UserInterface.Utils.ColorScope.Scope.Content, Colors.ErrorColor))
                {
                    EditorGUILayout.LabelField(GuidedSetupStyles.Contents.StatusIcon, GuidedSetupStyles.GUIStyles.IconStyleTopPadding,
                        GUILayout.Width(Constants.SmallIconSize),
                        GUILayout.Height(Constants.SmallIconSize));
                }
                EditorGUILayout.LabelField("Invalid AppID.", EditorStyles.whiteLabel);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void TestUserAddGUI()
        {
            var labelStyle = new GUIStyle(EditorStyles.label);
            var t0 = new GUIContent("Follow these");
            var w0 = labelStyle.CalcSize(t0).x - Constants.TextWidthOffset;

            var t1 = new GUIContent("to test your Shared Spatial Anchor app before publish it publicly.");
            var w1 = labelStyle.CalcSize(t1).x - Constants.TextWidthOffset;

            var link0 = new GUIContent("steps to add test users in Members Management");

            EditorGUILayout.BeginVertical(GuidedSetupStyles.GUIStyles.TopMargin);
            EditorGUILayout.BeginHorizontal();

            using (new Meta.XR.Editor.UserInterface.Utils.ColorScope(Meta.XR.Editor.UserInterface.Utils.ColorScope.Scope.Content, Colors.LightGray))
            {
                EditorGUILayout.LabelField(GuidedSetupStyles.Contents.DefaultIcon, GuidedSetupStyles.GUIStyles.IconStyleTopPadding, GUILayout.Width(Constants.SmallIconSize),
                    GUILayout.Height(Constants.SmallIconSize + Constants.Padding));
            }

            EditorGUILayout.LabelField(t0, GuidedSetupStyles.GUIStyles.LabelTopPadding, GUILayout.Width(w0));
            if (EditorGUILayout.LinkButton(link0))
            {
                OpenURL(AddTestUserDocURL, nameof(GuidedSetupAccount));
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(22);
            EditorGUILayout.LabelField(t1, GuidedSetupStyles.GUIStyles.LabelTopPadding, GUILayout.Width(w1));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DataUseCheckGUI()
        {
            var labelStyle = new GUIStyle(EditorStyles.label);
            var t0 = new GUIContent("Please refer to the");
            var w0 = labelStyle.CalcSize(t0).x - Constants.TextWidthOffset;

            var t1 = new GUIContent("section for more details.");
            var w1 = labelStyle.CalcSize(t1).x - Constants.TextWidthOffset;

            var linkText = new GUIContent("App Configuration");

            EditorGUILayout.BeginVertical(GuidedSetupStyles.GUIStyles.TopMargin);

            EditorGUILayout.BeginHorizontal();
            using (new Meta.XR.Editor.UserInterface.Utils.ColorScope(Meta.XR.Editor.UserInterface.Utils.ColorScope.Scope.Content, Colors.LightGray))
            {
                EditorGUILayout.LabelField(GuidedSetupStyles.Contents.DefaultIcon, GuidedSetupStyles.GUIStyles.IconStyleTopPadding, GUILayout.Width(Constants.SmallIconSize),
                    GUILayout.Height(Constants.SmallIconSize + Constants.Padding));
            }
            EditorGUILayout.LabelField("To use the Shared Spatial Anchor, the <color=lightblue><b>UserID</b></color> " +
                                       "and <color=lightblue><b>UserProfile</b></color> Platform\n" +
                                       "features must be enabled in <b>Data Use Checkup</b>.", GuidedSetupStyles.GUIStyles.LabelTopPadding);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(22);
            EditorGUILayout.LabelField(t0, GuidedSetupStyles.GUIStyles.LabelTopPadding, GUILayout.Width(w0));
            if (EditorGUILayout.LinkButton(linkText))
            {
                OpenURL(AddPlatformFeaturesDocURL, nameof(GuidedSetupAccount));
            }
            EditorGUILayout.LabelField(t1, GuidedSetupStyles.GUIStyles.LabelTopPadding, GUILayout.Width(w1));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void OnAppIDSetGUI()
        {
            var appId = "";
#if UNITY_ANDROID
            appId = PlatformSettings.MobileAppID;
#else
            appId = PlatformSettings.AppID;
#endif

            if (HasAppId() && !_appIdSet)
            {
                DrawStatusGUI($"Your project already has an AppID: {appId}");
            }
            else if(_appIdSet)
            {
                DrawStatusGUI($"Plaform settings has been set with AppID: {appId}");
            }

            EditorGUILayout.Space();
        }

        private void DrawStatusGUI(string msg)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(GuidedSetupStyles.Contents.SuccessIcon, GuidedSetupStyles.GUIStyles.IconStyle, GUILayout.Width(Constants.SmallIconSize),
                GUILayout.Height(Constants.SmallIconSize + Constants.Padding));

            EditorGUILayout.LabelField(msg);

            EditorGUILayout.EndHorizontal();
        }

        private static bool HasAppId()
        {
#if UNITY_ANDROID
            return !String.IsNullOrEmpty(PlatformSettings.MobileAppID);
#else
            return !String.IsNullOrEmpty(PlatformSettings.AppID);
#endif
        }

        private void OnDestroy()
        {
            OVRTelemetry.Start(OVRTelemetryConstants.GuidedSetup.MarkerId.CloseSSAWindow)
                .AddAnnotation(OVRTelemetryConstants.GuidedSetup.AnnotationType.HasAppId, HasAppId().ToString())
                .Send();
        }
    }
}

#endif // USING_META_XR_PLATFORM_SDK
