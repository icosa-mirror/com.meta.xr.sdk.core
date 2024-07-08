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
using UnityEngine;

namespace Meta.XR.Locomotion.Teleporter
{
    public enum Hand
    {
        None,
        Left,
        Right,
    }

    public enum Action
    {
        None,
        MoveForward,
        MoveBack,
        MoveLeft,
        MoveRight,
        RotateLeft,
        RotateRight,
        Teleport,
    }

    public class Teleporter : MonoBehaviour
    {
        public OVRCameraRig ovrCameraRig;

        [SerializeField] private Input _input;
        [SerializeField] private Targeter _targeter;
        [SerializeField] private TeleportTelegraph _telegraph;
        [SerializeField] private bool _allowStrafing = true;

        // members used for non-teleportation locomotion logic.
        [SerializeField] private float _rotateBy = 25.0f;

        // a public event fired when a teleportation has successfully finished
        public event Action<Pose> OnTeleport;

        private bool _aiming;
        private bool _isTeleporterActive;

        private void Awake()
        {
            _isTeleporterActive = true;
        }

        private void OnEnable()
        {
            _input.Init();

            if (!ovrCameraRig || !_input || !_targeter || !_telegraph)
            {
                Debug.Log("Teleporter: null properties detected. Disabling.");
                enabled = false;
            }
        }

        private void Update()
        {
            if (!_isTeleporterActive && !_aiming) return;

            // Using Init->Tick->Kill cycle to ensure the order of updates.
            _input.Tick();
            if (!_aiming)
            {
                if (_input.TeleportInit)
                {
                    StartAiming();
                }
                else if (_input.Strafe && _allowStrafing)
                {
                    TryStrafe();
                }
            }
            else
            {
                if (_input.TeleportExecute)
                {
                    ExecuteTeleport();
                }
                else if (_input.TeleportCancel)
                {
                    EndAiming();
                }
                else
                {
                    UpdateAiming();
                }
            }
        }

        private void StartAiming()
        {
            _aiming = true;
            _targeter.Init(_input.ActiveHand);
        }

        private void UpdateAiming()
        {
            // Handle rotation.
            var inputRotationRaw = Mathf.Rad2Deg * Mathf.Atan2(
              _input.ActiveJoystickPosition.x,
              _input.ActiveJoystickPosition.y);

            var rotation = Quaternion.Euler(0, _targeter.Origin.rotation.eulerAngles.y, 0) *
              Quaternion.Euler(0, inputRotationRaw, 0);

            // Handle targeting.
            _targeter.Tick(rotation);

            _telegraph.transform.position = _targeter.TargetPosition;
            _telegraph.transform.rotation = Quaternion.Euler(0, Mathf.Round(rotation.eulerAngles.y / 1f) * 1f, 0);
            _telegraph.Renderer.enabled = _targeter.ValidTarget;
        }

        private void EndAiming()
        {
            _aiming = false;
            _telegraph.Renderer.enabled = false;
            _targeter.Kill();
        }

        private void ExecuteTeleport()
        {
            if (!_aiming) return;

            EndAiming();

            if (!_targeter.ValidTarget) return;

            var teleportPosition = _targeter.TargetPosition;
            var teleportRotation = _telegraph.transform.rotation;
            var pose = new Pose(teleportPosition, teleportRotation);
            Teleport(pose);
        }

        private void TryStrafe()
        {
            if (_input.TeleportAction != Action.MoveLeft && _input.TeleportAction != Action.MoveRight)
                return;

            var rotateDir = 0f;
            switch (_input.TeleportAction)
            {
                case Action.MoveLeft:
                    rotateDir = _rotateBy * -1;
                    break;
                case Action.MoveRight:
                    rotateDir = _rotateBy;
                    break;
                case Action.None:
                case Action.MoveForward:
                case Action.MoveBack:
                case Action.RotateLeft:
                case Action.RotateRight:
                case Action.Teleport:
                default:
                    break;
            }

            var cameraTransform = ovrCameraRig.gameObject.transform;
            cameraTransform.RotateAround(ovrCameraRig.centerEyeAnchor.position, Vector3.up, rotateDir);
            _targeter.Clean();

            OnTeleport?.Invoke(new Pose(cameraTransform.position, cameraTransform.rotation));
        }

        public void Teleport(Pose targetPose)
        {
            // We are only calculating the rotation around the Y axes which would affect a Character relative to a horizontal floor.
            // We don't want pitch and roll of the headset to affect the new position.
            var deltaYRotation = targetPose.rotation.eulerAngles.y - ovrCameraRig.centerEyeAnchor.rotation.eulerAngles.y;
            var targetToHmdDeltaRotation = Vector3.up * deltaYRotation;
            var newRootRotation = Quaternion.Euler(targetToHmdDeltaRotation) * ovrCameraRig.transform.rotation;
            var rootToHmdDeltaPos = ovrCameraRig.centerEyeAnchor.position - ovrCameraRig.transform.position;

            // Ignore the delta height between the headset and the ovrCameraRig root node in calculation of the delta.
            rootToHmdDeltaPos.y = 0;
            var deltaPosRotated = Quaternion.Euler(targetToHmdDeltaRotation) * rootToHmdDeltaPos;
            var userPose = new Pose(targetPose.position - deltaPosRotated, newRootRotation);
            ovrCameraRig.transform.SetPositionAndRotation(userPose.position, userPose.rotation);

            OnTeleport?.Invoke(userPose);
        }
    }
}
