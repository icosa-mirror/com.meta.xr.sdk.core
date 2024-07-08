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

using UnityEngine;

namespace Meta.XR.Locomotion.Teleporter
{
    enum ValidHand
    {
        Active,
        LeftOnly,
        RightOnly,
    };

    public abstract class Input : MonoBehaviour
    {
        [SerializeField] private ValidHand _validHand = ValidHand.Active;

        public class Activateable
        {
            private bool _active;

            public bool Down { get; private set; }
            public bool Up { get; private set; }

            public bool Active
            {
                get { return _active; }
                set
                {
                    if (_active != value)
                    {
                        _active = value;
                        Down = value;
                        Up = !value;
                    }
                    else
                    {
                        Down = false;
                        Up = false;
                    }
                }
            }
        }

        protected class Controller : Activateable
        {
            private const float JoystickActivationDistance = 0.5f;
            private const float JoystickToleranceDistance = 0.1f;

            private readonly OVRInput.Controller _controllerEnum;
            private readonly OVRInput.Axis2D _joystickEnum;
            private readonly Activateable _joystickButton = new();

            public Vector2 JoystickPosition { get; private set; }
            public bool JoystickDown => _joystickButton.Down;

            public bool JoystickActive => _joystickButton.Active;

            public bool JoystickUp => _joystickButton.Up;

            public Controller(OVRInput.Controller controllerEnum, OVRInput.Axis2D joystickEnum)
            {
                _controllerEnum = controllerEnum;
                _joystickEnum = joystickEnum;
            }

            public void Refresh()
            {
                UpdateJoystick();
                UpdateFaceButtons();
            }

            private void UpdateJoystick()
            {
                if (!_joystickButton.Active && OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, _controllerEnum))
                {
                    JoystickPosition = Vector2.zero;
                    return;
                }

                var currentPosition = OVRInput.Get(_joystickEnum, _controllerEnum);

                if (!_joystickButton.Active && currentPosition.magnitude > JoystickActivationDistance)
                {
                    _joystickButton.Active = true;
                }
                else if (_joystickButton.Active)
                {
                    _joystickButton.Active = currentPosition.magnitude > JoystickToleranceDistance;
                }

                Active = _joystickButton.Active;

                JoystickPosition = currentPosition;
            }

            // OVRInput handles the actual Down/Up logic. We just want to know if
            // buttons are active so we can set the controller to active.
            private void UpdateFaceButtons()
            {
                this.Active = OVRInput.Get(OVRInput.Button.Any, _controllerEnum);
            }
        }

        public event System.Action OnTeleporterModeChangeButtonDown;
        public event System.Action OnResetPositionButtonDown;
        public event System.Action OnResetOrientationButtonDown;


        protected bool _initialized;

        public bool TeleportInit { get; protected set; }
        public bool TeleportExecute { get; protected set; }
        public bool TeleportCancel { get; protected set; }
        public bool Strafe { get; protected set; }
        public Hand ActiveHand { get; protected set; }

        protected Controller LeftController { get; set; }
        protected Controller RightController { get; set; }
        protected Controller ActiveController =>
            ActiveHand switch
            {
                Hand.Left => LeftController,
                _ => RightController
            };

        public Vector2 ActiveJoystickPosition => ActiveController.JoystickPosition;
        public Action TeleportAction { get; protected set; }

        public virtual void Init()
        {
            LeftController = new Controller(OVRInput.Controller.LTouch, OVRInput.Axis2D.PrimaryThumbstick);
            RightController = new Controller(OVRInput.Controller.RTouch, OVRInput.Axis2D.PrimaryThumbstick);

            ActiveHand = Hand.Right;

            _initialized = true;
        }

        public virtual void Tick()
        {
            if (!_initialized) return;

            UpdateControllerState();

            TeleportAction = Action.None;
            TeleportInit = false;
            TeleportExecute = false;
            TeleportCancel = false;
            Strafe = false;
        }

        protected void UpdateControllerState()
        {
            LeftController.Refresh();
            RightController.Refresh();

            if (_validHand == ValidHand.Active)
            {
                if (LeftController.Active &&
                    !RightController.Active)
                {
                    ActiveHand = Hand.Left;
                }
                else if (RightController.Active &&
                           !LeftController.Active)
                {
                    ActiveHand = Hand.Right;
                }
            }
            else if (_validHand == ValidHand.LeftOnly)
            {
                ActiveHand = Hand.Left;
            }
            else if (_validHand == ValidHand.RightOnly)
            {
                ActiveHand = Hand.Right;
            }
        }

        protected void TeleporterModeChangeButtonDown() => OnTeleporterModeChangeButtonDown?.Invoke();
        protected void ResetPositionButtonDown() => OnResetPositionButtonDown?.Invoke();
        protected void ResetOrientationButtonDown() => OnResetOrientationButtonDown?.Invoke();
    }
}
