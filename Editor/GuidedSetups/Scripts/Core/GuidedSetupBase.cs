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
using UnityEditor;
using UnityEngine;
using static Meta.XR.Editor.UserInterface.Styles.Constants;
using static Meta.XR.Editor.UserInterface.Styles.Colors;
using static Meta.XR.Editor.UserInterface.Utils;

namespace Meta.XR.GuidedSetups.Editor
{
    public abstract class GuidedSetupBase : EditorWindow
    {
        [SerializeField, OVRReadOnly] internal string id = Guid.NewGuid().ToString();
        public string Id => id;

        public string Title
        {
            get => _title;
            set => _title = value;
        }

        public string Description
        {
            get => _description;
            set => _description = value;
        }

        public const int DefaultMinSize = 520;
        public const int DefaultMaxSize = 480;

        private string _title = "Meta Guided Setup";
        private string _description = "Description placeholder.";

        protected void SetupWindow(
            string windowTitle,
            string description,
            int minSizeW = DefaultMinSize,
            int maxSizeW = DefaultMinSize,
            int minSizeH = DefaultMaxSize,
            int maxSizeH = DefaultMaxSize)
        {
            Title = windowTitle;
            Description = description;

            name = windowTitle;
            titleContent = new GUIContent(windowTitle);
            minSize = new Vector2(minSizeW, minSizeH);
            maxSize = new Vector2(maxSizeW, maxSizeH);
        }

        protected virtual void OnGUI()
        {
            OnHeaderGUI();
        }

        protected virtual void OnHeaderGUI()
        {
            var titleGuiContent = new GUIContent(Title);
            var expectedHeight = 84;
            var rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, expectedHeight);
            GUI.DrawTexture(rect, GuidedSetupStyles.Contents.BannerImage.GUIContent.image, ScaleMode.ScaleAndCrop);

            GUILayout.BeginArea(new Rect(Margin, LargeMargin, EditorGUIUtility.currentViewWidth, expectedHeight));

            EditorGUILayout.BeginHorizontal(GuidedSetupStyles.GUIStyles.Header);
            using (new ColorScope(ColorScope.Scope.Content, OffWhite))
            {
                EditorGUILayout.LabelField(GuidedSetupStyles.Contents.HeaderIcon, GuidedSetupStyles.GUIStyles.HeaderIconStyle, GUILayout.Width(32.0f),
                    GUILayout.ExpandWidth(false));
            }

            EditorGUILayout.LabelField(titleGuiContent, GuidedSetupStyles.GUIStyles.HeaderBoldLabel);
            EditorGUILayout.EndHorizontal();

            GUILayout.EndArea();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Description, GuidedSetupStyles.GUIStyles.SubtitleLabel);
            EditorGUILayout.EndHorizontal();
        }

        protected virtual void OpenURL(string url, string sourceWindow = "")
        {
            Application.OpenURL(url);

            OVRTelemetry.Start(OVRTelemetryConstants.GuidedSetup.MarkerId.URLOpen)
                .AddAnnotation(OVRTelemetryConstants.GuidedSetup.AnnotationType.GSTSource, sourceWindow)
                .AddAnnotation(OVRTelemetryConstants.GuidedSetup.AnnotationType.URL, url)
                .Send();
        }

        internal void AddGUIContent(GUIContent content)
        {
            EditorGUILayout.LabelField(content, GuidedSetupStyles.GUIStyles.LabelTopPadding);
        }

        internal void AddBulletedGUIContent(GUIContent content)
        {
            AddBulletedGUIContent(GuidedSetupStyles.ContentStatusType.Normal, content);
        }

        internal void AddBulletedGUIContent(GuidedSetupStyles.ContentStatusType type, GUIContent content)
        {
            Color color = GetColorByStatus(type);

            EditorGUILayout.BeginHorizontal();

            using (new ColorScope(ColorScope.Scope.Content, color))
            {
                EditorGUILayout.LabelField(GuidedSetupStyles.Contents.DefaultIcon, GuidedSetupStyles.GUIStyles.IconStyleTopPadding, GUILayout.Width(SmallIconSize),
                    GUILayout.Height(SmallIconSize + Padding));
            }

            AddGUIContent(content);

            EditorGUILayout.EndHorizontal();
        }

        internal Color GetColorByStatus(GuidedSetupStyles.ContentStatusType type)
        {
            Color color;

            switch (type)
            {
                case GuidedSetupStyles.ContentStatusType.Warning:
                    color = WarningColor;
                    break;
                case GuidedSetupStyles.ContentStatusType.Error:
                    color = ErrorColor;
                    break;
                default:
                    color = LightGray;
                    break;
            }

            return color;
        }

        internal Rect GuidedSetupBeginVertical() => EditorGUILayout.BeginVertical(GuidedSetupStyles.GUIStyles.ContentMargin);
        internal void GuidedSetupEndVertical() => EditorGUILayout.EndVertical();
    }
}
