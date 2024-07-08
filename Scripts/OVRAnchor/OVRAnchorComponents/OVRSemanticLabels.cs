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

/// <summary>
/// Descriptive labels of the <see cref="OVRAnchor"/>, as a list of enum values.
/// </summary>
/// <remarks>
/// This component can be accessed from an <see cref="OVRAnchor"/> that supports it by calling
/// <see cref="OVRAnchor.GetComponent{T}"/> from the anchor.
/// </remarks>
/// <seealso cref="Labels"/>
public readonly partial struct OVRSemanticLabels : IOVRAnchorComponent<OVRSemanticLabels>, IEquatable<OVRSemanticLabels>
{
    /// <summary>
    /// An enum that contains all possible classification values.
    /// </summary>
    public enum Classification
    {
        Floor = 0,
        Ceiling = 1,
        WallFace = 2,
        Table = 3,
        Couch = 4,
        DoorFrame = 5,
        WindowFrame = 6,
        Other = 7,
        Storage = 8,
        Bed = 9,
        Screen = 10,
        Lamp = 11,
        Plant = 12,
        WallArt = 13,
        SceneMesh = 14,
        InvisibleWallFace = 15,
    }

    internal const string DeprecationMessage = "String-based labels are deprecated (v65). Please use the equivalent enum-based methods.";

    /// <summary>
    /// Semantic Labels. Please use <see cref="GetClassifications"/> instead.
    /// </summary>
    /// <returns>
    /// <para>Comma-separated values in one <see cref="string"/></para>
    /// </returns>
    /// <exception cref="Exception">If it fails to get the semantic labels</exception>
    [Obsolete(DeprecationMessage)]
    public string Labels
    {
        get
        {
            if (!OVRPlugin.GetSpaceSemanticLabels(Handle, out var labels))
            {
                throw new Exception("Could not Get Semantic Labels");
            }

#pragma warning disable CS0618 // Type or member is obsolete
            return OVRSemanticClassification.ValidateAndUpgradeLabels(labels);
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }

    /// <summary>
    /// Get the Semantic Labels. Non-allocating.
    /// </summary>
    public void GetClassifications(ICollection<Classification> classifications)
    {
        classifications.Clear();
#pragma warning disable CS0618 // Type or member is obsolete
        FromApiString(Labels, classifications);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    /// Convert a single string label into a Classification.
    /// Be aware: unsupported labels are always OTHER (including deprecated DESK).
    /// </summary>
    internal static Classification FromApiLabel(string singleLabel)
    {
        return singleLabel switch
        {
            "FLOOR" => Classification.Floor,
            "CEILING" => Classification.Ceiling,
            "WALL_FACE" => Classification.WallFace,
            "COUCH" => Classification.Couch,
            "DOOR_FRAME" => Classification.DoorFrame,
            "WINDOW_FRAME" => Classification.WindowFrame,
            "OTHER" => Classification.Other,
            "STORAGE" => Classification.Storage,
            "BED" => Classification.Bed,
            "SCREEN" => Classification.Screen,
            "LAMP" => Classification.Lamp,
            "PLANT" => Classification.Plant,
            "TABLE" => Classification.Table,
            "WALL_ART" => Classification.WallArt,
            "INVISIBLE_WALL_FACE" => Classification.InvisibleWallFace,
            "GLOBAL_MESH" => Classification.SceneMesh,
            _ => Classification.Other,
        };
    }

    /// <summary>
    /// Converts a comma separated list of labels into a list of Classifications.
    /// </summary>
    internal static void FromApiString(string apiLabels, ICollection<Classification> classifications)
    {
        var labels = apiLabels.Split(',');
        foreach (var label in labels)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            // skip previously deprecated labels
            if (label == OVRSceneManager.Classification.Desk) continue;
#pragma warning restore CS0618 // Type or member is obsolete
            classifications.Add(FromApiLabel(label));
        }
    }

    /// <summary>
    /// Convert a single classification into an API label.
    /// </summary>
    internal static string ToApiLabel(Classification classification) =>
        classification switch
        {
#pragma warning disable CS0618, CS0612 // Type or member is obsolete
            Classification.Floor => OVRSceneManager.Classification.Floor,
            Classification.Ceiling => OVRSceneManager.Classification.Ceiling,
            Classification.WallFace => OVRSceneManager.Classification.WallFace,
            Classification.Couch => OVRSceneManager.Classification.Couch,
            Classification.DoorFrame => OVRSceneManager.Classification.DoorFrame,
            Classification.WindowFrame => OVRSceneManager.Classification.WindowFrame,
            Classification.Other => OVRSceneManager.Classification.Other,
            Classification.Storage => OVRSceneManager.Classification.Storage,
            Classification.Bed => OVRSceneManager.Classification.Bed,
            Classification.Screen => OVRSceneManager.Classification.Screen,
            Classification.Lamp => OVRSceneManager.Classification.Lamp,
            Classification.Plant => OVRSceneManager.Classification.Plant,
            Classification.Table => OVRSceneManager.Classification.Table,
            Classification.WallArt => OVRSceneManager.Classification.WallArt,
            Classification.InvisibleWallFace => OVRSceneManager.Classification.InvisibleWallFace,
            Classification.SceneMesh => OVRSceneManager.Classification.GlobalMesh,
            _ => OVRSceneManager.Classification.Other,
#pragma warning restore CS0618, CS0612 // Type or member is obsolete
        };

    /// <summary>
    /// Convert a list of classifications into a comma-separated string.
    /// Null returns the empty string.
    /// </summary>
    internal static string ToApiString(IReadOnlyList<Classification> classifications)
    {
        if (classifications == null) return string.Empty;

        using (new OVRObjectPool.ListScope<string>(out var labels))
        {
            foreach (var classification in classifications)
                labels.Add(ToApiLabel(classification));
            return string.Join(',', labels);
        }
    }
}
