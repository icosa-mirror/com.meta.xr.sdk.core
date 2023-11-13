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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class OVRSceneChangeListener
{
    private static readonly HashSet<string> TrackedAssemblies = new HashSet<string>
    {
        "Oculus.VR",
        "Oculus.VR.Editor",
        "Oculus.VR.Scripts.Editor"
    };

    private static readonly List<Component> ComponentList = new List<Component>();

    static OVRSceneChangeListener()
    {
        if (OVRRuntimeSettings.Instance.TelemetryEnabled)
        {
            RegisterCallback();
        }

        OVRRuntimeSettings.Instance.OnTelemetrySet += OnTelemetrySet;
    }

    private static void OnTelemetrySet(bool enabled)
    {
        if (enabled)
        {
            RegisterCallback();
        }
        else
        {
            RemoveCallback();
        }
    }

    private static void RegisterCallback()
    {
        ObjectChangeEvents.changesPublished -= ChangesPublished;
        ObjectChangeEvents.changesPublished += ChangesPublished;
    }

    private static void RemoveCallback()
    {
        ObjectChangeEvents.changesPublished -= ChangesPublished;
    }

    private static void ProcessComponent(Component component)
    {
        if (component == null)
        {
            return;
        }

        var type = component.GetType();
        if (!TrackedAssemblies.Contains(type.Assembly.GetName().Name))
        {
            return;
        }

        OVRTelemetry.Start(OVRTelemetryConstants.Editor.MarkerId.ComponentAdd)
             .AddAnnotation(OVRTelemetryConstants.Editor.AnnotationType.ComponentName, type.Name)
             .Send();
    }

    private static void ProcessGameObject(GameObject go)
    {
        if (go == null)
        {
            return;
        }

        go.GetComponentsInChildren(ComponentList);
        foreach (var component in ComponentList)
        {
            ProcessComponent(component);
        }
    }

    private static void ChangesPublished(ref ObjectChangeEventStream stream)
    {
        for (var i = 0; i < stream.length; i++)
        {
            ParseEvent(stream, i);
        }
    }

    private static void ParseEvent(ObjectChangeEventStream stream, int i)
    {
        switch (stream.GetEventType(i))
        {
            case ObjectChangeKind.CreateGameObjectHierarchy:
                stream.GetCreateGameObjectHierarchyEvent(i, out var createGameObjectHierarchyEvent);
                ProcessGameObject(
                    EditorUtility.InstanceIDToObject(createGameObjectHierarchyEvent.instanceId) as GameObject);
                break;
            case ObjectChangeKind.ChangeGameObjectStructure:
                stream.GetChangeGameObjectStructureEvent(i, out var changeGameObjectStructure);
                ProcessGameObject(EditorUtility.InstanceIDToObject(changeGameObjectStructure.instanceId) as GameObject);
                break;
        }
    }
}
