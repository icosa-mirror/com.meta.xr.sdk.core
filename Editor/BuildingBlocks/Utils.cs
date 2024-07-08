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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Meta.XR.Editor.Callbacks;
using Meta.XR.Editor.StatusMenu;
using Meta.XR.Editor.Tags;
using Meta.XR.Editor.UserInterface;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Meta.XR.Editor.UserInterface.Styles.Colors;
using static Meta.XR.Editor.UserInterface.Styles.Contents;
using Object = UnityEngine.Object;


namespace Meta.XR.BuildingBlocks.Editor
{
    [InitializeOnLoad]
    internal static class Utils
    {
        internal const string BlocksPublicName = "Building Blocks";
        internal const string BlockPublicName = "Building Block";
        internal const string BlockPublicTag = "[BuildingBlock]";

        internal static readonly TextureContent.Category BuildingBlocksIcons = new("BuildingBlocks/Icons");
        internal static readonly TextureContent.Category BuildingBlocksThumbnails = new("BuildingBlocks/Thumbnails");
        internal static readonly TextureContent.Category BuildingBlocksAnimations = new("BuildingBlocks/Animations");

        internal static readonly TextureContent StatusIcon = TextureContent.CreateContent("ovr_icon_bbw.png",
            Utils.BuildingBlocksIcons, $"Open {BlocksPublicName}");

        internal static readonly TextureContent GotoIcon = TextureContent.CreateContent("ovr_icon_link.png",
            Utils.BuildingBlocksIcons, "Select Block");

        internal static readonly TextureContent AddIcon = TextureContent.CreateContent("ovr_icon_addblock.png",
            Utils.BuildingBlocksIcons, "Add Block to current scene");

        private const string ExperimentalTagName = "Experimental";

        internal static readonly TextureContent ExperimentalIcon =
            TextureContent.CreateContent("ovr_icon_experimental.png", Utils.BuildingBlocksIcons,
                ExperimentalTagName);

        internal static Tag ExperimentalTag = new(ExperimentalTagName)
        {
            Behavior =
            {
                Color = ExperimentalColor,
                Icon = ExperimentalIcon,
                Order = 100,
                ShowOverlay = true,
                ToggleableVisibility = true,
            }
        };

        private const string PrototypingTagName = "Prototyping";

        internal static readonly TextureContent PrototypingIcon =
            TextureContent.CreateContent("ovr_icon_prototype.png", Utils.BuildingBlocksIcons,
                PrototypingTagName);

        internal static Tag PrototypingTag = new(PrototypingTagName)
        {
            Behavior =
            {
                Color = ExperimentalColor,
                Icon = PrototypingIcon,
                Order = 101,
                ShowOverlay = true,
                ToggleableVisibility = true,
            }
        };

        private const string DebugTagName = "Debug";

        internal static readonly TextureContent DebugIcon =
            TextureContent.CreateContent("ovr_icon_debug.png", Utils.BuildingBlocksIcons, DebugTagName);

        internal static Tag DebugTag = new(DebugTagName)
        {
            Behavior =
            {
                Color = DebugColor,
                Icon = DebugIcon,
                Order = 90,
                ShowOverlay = true,
                ToggleableVisibility = true,
            }
        };

        private const string InternalTagName = "Internal";

        internal static Tag InternalTag = new(InternalTagName)
        {
            Behavior =
            {
                Order = 200,
                Automated = true,
                Show = false,
                DefaultVisibility = false
            }
        };

        private const string HiddenTagName = "Hidden";

        internal static Tag HiddenTag = new(HiddenTagName)
        {
            Behavior =
            {
                Order = 201,
                Show = false,
                DefaultVisibility = false,
            }
        };

        private const string DeprecatedTagName = "Deprecated";

        internal static Tag DeprecatedTag = new(DeprecatedTagName)
        {
            Behavior =
            {
                Order = 203,
                Color = ErrorColor,
                Icon = TextureContent.CreateContent("ovr_icon_deprecated.png", Utils.BuildingBlocksIcons,
                    HiddenTagName),
                Show = true,
                ShowOverlay = true,
                ToggleableVisibility = true,
                DefaultVisibility = false,
            }
        };

        private const string NewTagName = "New";

        internal static Tag NewTag = new(NewTagName)
        {
            Behavior =
            {
                Automated = true,
                Order = 202,
                Color = NewColor,
                Icon = TextureContent.CreateContent("ovr_icon_new.png", Utils.BuildingBlocksIcons,
                    NewTagName),
                Show = true,
                CanFilterBy = false,
                ShowOverlay = true,
            }
        };

        private static readonly Dictionary<string, BlockBaseData> IDToBlockDataDictionary = new();

        private static bool _dirty = true;



        private const string DocumentationUrl = "https://developer.oculus.com/documentation/unity/unity-buildingblocks-overview";


        internal static readonly Item Item = new()
        {
            Name = BlocksPublicName,
            Color = Styles.Colors.AccentColor,
            Icon = StatusIcon,
            InfoTextDelegate = ComputeInfoText,
            PillIcon = GetPillIcon,
            OnClickDelegate = OnStatusMenuClick,
            Order = 1,
            HeaderIcons = new List<Item.HeaderIcon>()
            {
                new()
                {
                    TextureContent = ConfigIcon,
                    Color = LightGray,
                    Action = BuildingBlocksWindow.ShowSettingsMenu
                },
                new()
                {
                    TextureContent = DocumentationIcon,
                    Color = LightGray,
                    Action = () => Application.OpenURL(DocumentationUrl)
                },
            }
        };

        static Utils()
        {
            StatusMenu.RegisterItem(Item);

            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.projectChanged += OnProjectChanged;

        }

        public static void OnProjectChanged()
        {
            _dirty = true;
        }

        private static int ComputeNumberOfNewBlocks() =>
            GetAllBlockData().Count(data => !data.Hidden && data.Tags.Contains(NewTag));

        private static (string, Color?) ComputeInfoText()
        {
            var numberOfNewBlocks = ComputeNumberOfNewBlocks();
            if (numberOfNewBlocks > 0)
            {
                return (
                    $"There {OVREditorUtils.ChoosePlural(numberOfNewBlocks, "is", "are")} {numberOfNewBlocks} new {OVREditorUtils.ChoosePlural(numberOfNewBlocks, "block", "blocks")} available!",
                    NewColor);
            }

            var numberOfBlocks = GetBlocksInScene().Count;
            return (
                $"{numberOfBlocks} {OVREditorUtils.ChoosePlural(numberOfBlocks, "block", "blocks")} in current scene.",
                null);
        }

        private static (TextureContent, Color?) GetPillIcon()
        {
            if (ComputeNumberOfNewBlocks() > 0)
            {
                return (NewTag.Behavior.Icon, NewColor);
            }

            return (null, null);
        }

        private static void OnStatusMenuClick(Item.Origins origin)
        {
            BuildingBlocksWindow.ShowWindow(origin);
        }

        public static void RefreshList(bool force = false)
        {
            if (!_dirty && !force)
            {
                return;
            }

            var blockDataList =
                AllBlockData
                    .Where(t => t != null)
                    .ToList();

            IDToBlockDataDictionary.Clear();
            foreach (var blockData in blockDataList)
            {
                IDToBlockDataDictionary[blockData.Id] = blockData;
            }


            _dirty = false;
        }

        public static BlockData GetBlockData(this BuildingBlock block)
        {
            return GetBlockData(block.BlockId);
        }

        public static BlockData GetBlockData(string blockId)
        {
            RefreshList();
            IDToBlockDataDictionary.TryGetValue(blockId, out var blockData);
            return blockData as BlockData;
        }

        private static IEnumerable<BlockBaseData> GetAllBlockData()
        {
            RefreshList();
            return IDToBlockDataDictionary.Values.ToArray();
        }

        public static BuildingBlock GetBlock(this BlockData data)
        {
            return Object.FindObjectsByType<BuildingBlock>(FindObjectsSortMode.None)
                .FirstOrDefault(x => x.BlockId == data.Id);
        }

        public static BuildingBlock GetBlock(string blockId)
        {
            return GetBlockData(blockId)?.GetBlock();
        }

        public static List<BuildingBlock> GetBlocks(this BlockData data)
        {
            return Object.FindObjectsByType<BuildingBlock>(FindObjectsSortMode.None).Where(x => x.BlockId == data.Id)
                .ToList();
        }

        public static List<BuildingBlock> GetBlocks(string blockId)
        {
            return GetBlockData(blockId)?.GetBlocks();
        }

        public static List<T> GetBlocksWithType<T>() where T : Component
        {
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None)
                .Where(controller => controller.GetComponent<BuildingBlock>() != null).ToList();
        }

        public static List<T> GetBlocksWithBaseClassType<T>() where T : Component
        {
            var objects = GetBlocksWithType<T>();
            return objects
                .Select(obj => obj.GetComponent<T>())
                .Where(component => component != null && component.GetType() == typeof(T))
                .ToList();
        }

        private static bool IsRequiredBy(this BlockData data, BlockData other)
        {
            if (data == null || other == null)
            {
                return false;
            }

            return data == other || other.Dependencies.Any(data.IsRequiredBy);
        }

        public static List<BuildingBlock> GetBlocksInScene()
        {
            return Object.FindObjectsByType<BuildingBlock>(FindObjectsSortMode.InstanceID).ToList();
        }

        public static List<BuildingBlock> GetUsingBlocksInScene(this BlockData requiredData)
        {
            return Object.FindObjectsByType<BuildingBlock>(FindObjectsSortMode.None).Where(x =>
            {
                var data = x.GetBlockData();
                return requiredData != data && requiredData.IsRequiredBy(data);
            }).ToList();
        }

        public static List<BlockData> GetUsingBlockDatasInScene(this BlockData requiredData)
        {
            return requiredData.GetUsingBlocksInScene().Select(x => x.GetBlockData()).ToList();
        }

        public static List<BlockData> GetAllDependencyDatas(this BlockData data)
        {
            return data.Dependencies
                .Where(dependency => dependency != null)
                .SelectMany(dependency => GetAllDependencyDatas(dependency).Concat(new[] { dependency }))
                .Distinct()
                .ToList();
        }

        public static void SelectBlockInScene(this BuildingBlock block)
        {
            Selection.activeGameObject = block.gameObject;
        }

        public static void SelectBlocksInScene(IEnumerable<GameObject> blockList)
        {
            Selection.objects = blockList.Cast<Object>().ToArray();
        }

        public static void SelectBlocksInScene(this BlockData blockData)
        {
            var blocksInScene = blockData.GetBlocks();

            if (blocksInScene.Count == 1)
            {
                SelectBlockInScene(blocksInScene[0]);
            }
            else if (blocksInScene.Count > 1)
            {
                SelectBlocksInScene(blocksInScene.Select(block => block.gameObject));
            }
        }

        public static void HighlightBlockInScene(this BuildingBlock block)
        {
            EditorGUIUtility.PingObject(block.gameObject);
        }

        public static int ComputeNumberOfBlocksInScene(this BlockData blockData)
        {
            return Object.FindObjectsByType<BuildingBlock>(FindObjectsSortMode.None)
                .Count(x => x.BlockId == blockData.Id);
        }

        public static T FindComponentInScene<T>() where T : Component
        {
            var scene = SceneManager.GetActiveScene();
            var rootGameObjects = scene.GetRootGameObjects();
            return rootGameObjects.FirstOrDefault(go => go.GetComponentInChildren<T>())?.GetComponentInChildren<T>();
        }

        public static IEnumerable<BlockBaseData> AllBlockData =>
            AssetDatabase.FindAssets($"t:{nameof(BlockBaseData)}")
                .Select(id =>
                    AssetDatabase.LoadAssetAtPath<BlockBaseData>(AssetDatabase.GUIDToAssetPath(id))
                );


        public static TResult Let<TSource, TResult>(this TSource source, Func<TSource, TResult> func) => func(source);

    }
}
