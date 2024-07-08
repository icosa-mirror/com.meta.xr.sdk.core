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
using UnityEditor;
using UnityEngine;

namespace Meta.XR.BuildingBlocks.Editor
{
    public class ControllerTrackingBlockData : BlockData
    {
        protected override List<GameObject> InstallRoutine(GameObject selectedGameObject)
            => new List<GameObject>
            {
                InstantiateController(OVRInput.Hand.HandLeft),
                InstantiateController(OVRInput.Hand.HandRight)
            };

        private GameObject InstantiateController(OVRInput.Hand handedness)
        {
            var controllerType = handedness == OVRInput.Hand.HandLeft
                ? OVRInput.Controller.LTouch
                : OVRInput.Controller.RTouch;

            var cameraRigBB = Utils.GetBlocksWithType<OVRCameraRig>().First();

            var idealParent = handedness == OVRInput.Hand.HandLeft
                ? cameraRigBB.leftControllerAnchor
                : cameraRigBB.rightControllerAnchor;

            // Early out if we can find a pre-existing non block version. It will get blockified
            if (TryGetPreexistingNonBlock(cameraRigBB.transform, controllerType, idealParent, out var nonBlockObject)) return nonBlockObject;

            var controller = Instantiate(Prefab, Vector3.zero, Quaternion.identity);
            controller.SetActive(true);

            var handednessName = handedness == OVRInput.Hand.HandLeft ? "Left" : "Right";
            controller.name = $"{Utils.BlockPublicTag} {BlockName} {handednessName}";
            Undo.RegisterCreatedObjectUndo(controller, $"Create {BlockName} {handednessName}");
            Undo.SetTransformParent(controller.transform, idealParent, true, "Parent to camera rig");
            controller.GetComponent<OVRControllerHelper>().m_controller = controllerType;

            return controller;
        }

        private bool TryGetPreexistingNonBlock(Transform root, OVRInput.Controller controllerType, Transform idealParent, out GameObject nonBlockObject)
        {
            nonBlockObject = root.GetComponentsInChildren<OVRControllerHelper>()
                .FirstOrDefault(controller => IsCorrectHandedness(controller, controllerType)
                && HasCorrectParent(controller, idealParent))?.gameObject;
            return nonBlockObject != null;
        }

        private bool IsCorrectHandedness(OVRControllerHelper controller, OVRInput.Controller controllerType)
            => controller.m_controller == controllerType;

        private bool HasCorrectParent(OVRControllerHelper controller, Transform idealParent)
            => controller.transform.parent == idealParent;

    }
}
