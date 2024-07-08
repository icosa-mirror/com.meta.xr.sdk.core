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
    public class JoystickInput : Input
    {
        public override void Tick()
        {
            if (!_initialized) return;
            base.Tick();
            if (ActiveController.JoystickDown)
            {
                UpdateTeleportAction(ActiveController.JoystickPosition);
            }
            else if (ActiveController.JoystickUp)
            {
                TeleportExecute = true;
            }
            else if (!ActiveController.JoystickActive)
            {
                TeleportAction = Action.None;
            }

            if (OVRInput.Get(OVRInput.RawButton.LThumbstick) && OVRInput.Get(OVRInput.RawButton.RThumbstick))
            {
                ResetOrientationButtonDown();
            }
        }

        private void UpdateTeleportAction(Vector2 position)
        {
            var nPos = position.normalized;
            var degrees = Mathf.Acos(nPos.x) * Mathf.Rad2Deg;
            if (degrees < 45f)
            {
                TeleportAction = Action.MoveRight;
                Strafe = true;
            }
            else if (degrees > 135f)
            {
                TeleportAction = Action.MoveLeft;
                Strafe = true;
            }
            else if (nPos.y < 0)
            {
                TeleportAction = Action.MoveBack;
                Strafe = true;
            }
            else
            {
                TeleportAction = Action.Teleport;
                TeleportInit = true;
            }
        }
    }
}
