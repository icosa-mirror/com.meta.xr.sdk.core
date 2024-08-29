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
using UnityEngine.UI;

namespace Meta.XR.ImmersiveDebugger.UserInterface.Generic
{
    public class Panel : InteractableController
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init() // reset static fields in case of domain reload disabled
        {
            _hapticsClip = null;
        }

        private static OVRHapticsClip _hapticsClip;
        private static OVRHapticsClip HapticsClip
        {
            get
            {
                if (OVRHaptics.Config.SampleSizeInBytes == 0)
                {
                    return null;
                }

                return _hapticsClip ??= new OVRHapticsClip(new byte[5] { 10, 20, 40, 60, 40 }, 5);
            }
        }

        protected Canvas _canvas;
        private CanvasScaler _canvasScaler;
        private OVRRaycaster _ovrRaycaster;
        public float PixelsPerUnit { get; private set; }

        protected Background Background;
        protected ImageStyle _backgroundStyle;

        private Vector3 _sphericalCoordinates = new Vector3(1, 0, 0);
        public Vector3 SphericalCoordinates
        {
            get => _sphericalCoordinates;
            set
            {
                _sphericalCoordinates = value;
                var position = SphericalToCartesian(_sphericalCoordinates.x, _sphericalCoordinates.y, _sphericalCoordinates.z);
                SetPosition(position);
            }
        }
        public Interface Interface => (Owner as Interface);


        private bool _transparent;
        public bool Transparent
        {
            get => _transparent;
            set
            {
                if (_transparent == value)
                {
                    return;
                }

                _transparent = value;
                OnTransparencyChanged();
            }
        }

        public ImageStyle BackgroundStyle
        {
            set
            {
                _backgroundStyle = value;
                Background.Sprite = value.sprite;
                Background.Color = value.color;
            }
        }

        protected override void Setup(Controller owner)
        {
            base.Setup(owner);

            // Canvas
            _canvas = GameObject.AddComponent<Canvas>();
            _canvasScaler = GameObject.AddComponent<CanvasScaler>();

            // Background
            Background = Append<Background>("background");
            Background.LayoutStyle = Style.Load<LayoutStyle>("Fill");
        }

        protected void SetExpectedPixelsPerUnit(float pixelsPerUnit, float dynamicPixelsPerUnit, float referencePixelsPerUnit)
        {
            PixelsPerUnit = pixelsPerUnit;
            _canvasScaler.dynamicPixelsPerUnit = dynamicPixelsPerUnit;
            _canvasScaler.referencePixelsPerUnit = referencePixelsPerUnit;
            Transform.localScale = Vector3.one / PixelsPerUnit;
        }

        private void SetPosition(Vector3 position)
        {
            Transform.localPosition = position;
            Transform.rotation = Quaternion.LookRotation(Transform.position - Owner.Transform.position, Vector3.up);
        }

        private static Vector3 SphericalToCartesian(float radius, float theta, float phi)
        {
            const float pibytwo = Mathf.PI / 2;
            theta = pibytwo - theta;
            phi = pibytwo - phi;

            var x = radius * Mathf.Sin(phi) * Mathf.Cos(theta);
            var z = radius * Mathf.Sin(phi) * Mathf.Sin(theta);
            var y = radius * Mathf.Cos(phi);

            return new Vector3(x, y, z);
        }

        protected virtual void OnTransparencyChanged()
        {
            Background.Color = Transparent ? _backgroundStyle.colorOff : _backgroundStyle.color;
        }

        protected override void OnHoverChanged()
        {
            base.OnHoverChanged();
            if (Hover)
            {
                PlayHaptics(HapticsClip);

                // Grab the cursor
                Interface.Cursor.Attach(this);
            }
        }

        private void RefreshCanvas()
        {
            if (_canvas.worldCamera != Interface.Camera)
            {
                _canvas.worldCamera = Interface.Camera;
            }
        }

        private void RefreshRaycaster()
        {
            if (_ovrRaycaster) return;
            if (!_canvas.worldCamera) return;

            _ovrRaycaster = GameObject.AddComponent<OVRRaycaster>();
            _ovrRaycaster.pointer = Interface.Cursor.GameObject;
            _ovrRaycaster.blockingMask = 1 << 5; // TODO : setting
        }

        private void LateUpdate()
        {
            RefreshCanvas();
            RefreshRaycaster();
        }
    }
}

