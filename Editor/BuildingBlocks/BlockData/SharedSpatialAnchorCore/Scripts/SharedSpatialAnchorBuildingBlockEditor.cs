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

using System;
using Meta.XR.Editor.Tags;
using Meta.XR.Editor.UserInterface;
using UnityEditor;
using UnityEngine;
using Meta.XR.GuidedSetups.Editor;

#if USING_META_XR_PLATFORM_SDK
using Oculus.Platform;
#endif // USING_META_XR_PLATFORM_SDK

namespace Meta.XR.BuildingBlocks.Editor
{
    [CustomEditor(typeof(SharedSpatialAnchorCoreBuildingBlock))]
    public class SharedSpatialAnchorBuildingBlockEditor : BuildingBlockEditor
    {
        protected override void ShowAdditionals()
        {
            EditorGUILayout.BeginVertical(Styles.GUIStyles.ErrorHelpBox);
            DrawMessageWithIcon(Styles.Contents.InfoIcon, "A Meta Quest AppID is required to use Shared Spatial Anchor.");
#if USING_META_XR_PLATFORM_SDK
            if (HasAppId())
            {
                var appId = "";
#if UNITY_ANDROID
                appId = PlatformSettings.MobileAppID;
#else // UNITY_ANDROID
                appId = PlatformSettings.AppID;
#endif // UNITY_ANDROID
                DrawMessageWithIcon(Styles.Contents.SuccessIcon, $"AppID found in Platform Settings: {appId}");
            }
            else
            {
                DrawMessageWithIcon(Styles.Contents.ErrorIcon, "AppID is missing. Use <color=#66aaff>Meta Account Setup Guide</color> to configure your project");
            }

            EditorGUILayout.Space();
            DrawButtons();
#else // USING_META_XR_PLATFORM_SDK
            DrawMessageWithIcon(Styles.Contents.ErrorIcon, "Meta Platform SDK is missing.");
            EditorGUILayout.Space();

#endif // USING_META_XR_PLATFORM_SDK
            EditorGUILayout.EndVertical();
        }

#if USING_META_XR_PLATFORM_SDK
        private void DrawButtons()
        {
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Open Meta Account Setup Guide"))
            {
                GuidedSetupAccount.ShowWindow(GuidedSetups.Editor.Utils.TriggerSource.Inspector);
            }

            if (GUILayout.Button("Open Platform Settings"))
            {
                EditorApplication.ExecuteMenuItem("Oculus/Platform/Edit Settings");
            }
            EditorGUILayout.EndVertical();
        }

        internal static bool HasAppId()
        {
#if UNITY_ANDROID
            return !String.IsNullOrEmpty(PlatformSettings.MobileAppID);
#else
            return !String.IsNullOrEmpty(PlatformSettings.AppID);
#endif
        }
#endif // USING_META_XR_PLATFORM_SDK
    }
}
