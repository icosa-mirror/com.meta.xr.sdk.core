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
using UnityEngine;

namespace Meta.XR.Locomotion.Teleporter
{
    public class ArcTargeter : Targeter
    {
        private readonly Vector3 _g = new(0, -9.81f, 0);

        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private bool _enableLineRenderer = true;
        [SerializeField] private float _speed = 20;
        [SerializeField] private Color[] _validColors = new Color[2];
        [SerializeField] private Color[] _invalidColors = new Color[2];

        private List<Vector3> _trajectoryPositions = new List<Vector3>();
        private Transform leftAnchor;
        private Transform rightAnchor;

        protected virtual void Awake()
        {
            Debug.Assert(_lineRenderer != null);
        }

        public override bool ValidTarget =>
            DidHit && Vector3.Dot(_hitInfo.normal, Vector3.up) >= Mathf.Cos(SlopeToleranceRadians);

        public override void Init(Hand targetingHand)
        {
            leftAnchor = OVRManager.instance.GetComponentInChildren<OVRCameraRig>().leftControllerAnchor;
            rightAnchor = OVRManager.instance.GetComponentInChildren<OVRCameraRig>().rightControllerAnchor;
            Origin = targetingHand == Hand.Left ? leftAnchor : rightAnchor;
            _lineRenderer.enabled = _enableLineRenderer;
            _initialized = true;
        }

        public override void Tick(Quaternion inputRotation)
        {
            if (!_initialized)
            {
                Debug.LogError("Trying to tick the ArcTargeter without initializing.");
                return;
            }

            _trajectoryPositions.Clear();

            const float timeStep = 0.01f; // 2 per second
            const int maxSteps = 500;

            var velocity = _speed * Origin.forward;
            var pos = Origin.position + velocity * timeStep;

            for (var i = 0; i < maxSteps; i++)
            {
                _trajectoryPositions.Add(pos);
                velocity += _g * timeStep;
                var frameMove = velocity * timeStep;

                DidHit = Physics.Raycast(pos, frameMove, out _hitInfo, frameMove.magnitude);
                if (DidHit)
                {
                    break;
                }

                pos += frameMove;
                _trajectoryPositions.Add(pos);
            }

            if (!_enableLineRenderer) return;

            _lineRenderer.positionCount = _trajectoryPositions.Count;
            _lineRenderer.SetPositions(_trajectoryPositions.ToArray());

            _lineRenderer.startColor = ValidTarget ? _validColors[0] : _invalidColors[0];
            _lineRenderer.endColor = ValidTarget ? _validColors[1] : _invalidColors[1];
        }

        public override void Kill()
        {
            _lineRenderer.enabled = false;
            _initialized = false;
        }

        public override void Clean()
        {
            _trajectoryPositions.Clear();
            base.Clean();
        }
    }
}
