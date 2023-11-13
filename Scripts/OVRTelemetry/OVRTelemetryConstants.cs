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

using MarkerPoint = OVRTelemetry.MarkerPoint;

internal static class OVRTelemetryConstants
{
    public static class OVRManager
    {
        public static class MarkerId
        {
            public const int Init = 163069401;
            public const int Consent = 163056770;
        }

        public static class AnnotationTypes
        {
            public const string Origin = "Origin";
            public const string EngineVersion = "developer_platform_version";
        }

        public enum ConsentOrigins
        {
            Popup,
            Settings,
            Legacy
        }

        public static readonly MarkerPoint InitializeInsightPassthrough =
            new MarkerPoint("InitializeInsightPassthrough");

        public static readonly MarkerPoint InitPermissionRequest = new MarkerPoint("InitPermissionRequest");
    }

    public static class Editor
    {
        public static class MarkerId
        {
            public const int Start = 163067235;
            public const int ComponentAdd = 163060094;
        }

        public static class AnnotationType
        {
            public const string ComponentName = "ComponentName";
        }
    }

    public static class BB
    {
        public static class MarkerId
        {
            public const int OpenWindow = 163062905;
            public const int AddBlock = 163060420;
            public const int UpdateBlock = 163064521;
            public const int RunBlock = 163063912;
            public const int InstallSDK = 163067801;
            public const int RemoveSDK = 163067560;
            public const int InstallBlockData = 163065449;
            public const int OpenSceneWithBlock = 163063649;
        }

        public static class AnnotationType
        {
            public const string BlockId = "BlockId";
            public const string BlockName = "BlockName";
            public const string InstanceId = "InstanceId";
            public const string Version = "Version";
            public const string ActionTrigger = "action_trigger";
            public const string Error = "error";
        }
    }
}
