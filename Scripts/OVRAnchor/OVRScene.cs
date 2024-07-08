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
using static OVRPlugin;

/// <summary>
/// Functionality related to Scene Understanding
/// </summary>
/// <remarks>
/// See https://developer.oculus.com/documentation/unity/unity-scene-overview for more details on Scene.
/// </remarks>
public static class OVRScene
{
    /// <summary>
    /// Requests Space Setup
    /// </summary>
    /// <remarks>
    /// Requests [Space Setup](https://developer.oculus.com/documentation/unity/unity-scene-overview/#how-does-scene-work).
    /// Space Setup pauses the application and prompts the user to setup their Space. The app resumes when the user
    /// either cancels or completes Space Setup.
    ///
    /// If not null, <parmaref name="labels"/> is a collection of comma-separated list of semantic labels that the user
    /// must define during Space Setup. You may specify the same label multiple times. For example,
    /// <code><![CDATA[
    /// await RequestSpaceSetup("TABLE,TABLE");
    /// ]]></code>
    /// would prompt the user to define two tables.
    ///
    /// See <see cref="OVRSceneManager.Classification"/> for the list of valid semantic labels.
    ///
    /// This method is asynchronous. The result of the task indicates whether the operation was successful. `False`
    /// usually indicates an unexpected failure; if the user cancels Space Setup, the operation still completes
    /// successfully.
    /// </remarks>
    /// <param name="labels">(Optional) The types of anchors that the user should define.</param>
    /// <returns>A task that can be used to track the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="labels"/> contains a label that is not one of
    /// those provided by <see cref="OVRSceneManager.Classification"/></exception>
    public static OVRTask<bool> RequestSpaceSetup(string labels = null)
    {
#if DEVELOPMENT_BUILD || OVRPLUGIN_TESTING
        if (!string.IsNullOrEmpty(labels))
        {
            ValidateRequestString(labels.Split(','), nameof(labels));
        }
#endif

        return RequestSceneCapture(labels, out var requestId)
            ? OVRTask.FromRequest<bool>(requestId)
            : OVRTask.FromResult(false);
    }

    static void ValidateRequestString(IEnumerable<string> labels, string paramName)
    {
        foreach (var label in labels.ToNonAlloc())
        {
            if (label == null || !s_allowedClassifications.Contains(label))
            {
                throw new ArgumentException($"'{label}' is not a valid label. See " +
                                            $"{nameof(OVRSceneManager)}.{nameof(OVRSceneManager.Classification)}." +
                                            $"{nameof(OVRSceneManager.Classification.List)}", paramName);
            }
        }
    }

    static readonly HashSet<string> s_allowedClassifications = new(OVRSceneManager.Classification.List);
}
