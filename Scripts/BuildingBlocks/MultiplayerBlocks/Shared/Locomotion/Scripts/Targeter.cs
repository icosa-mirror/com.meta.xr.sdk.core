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
    public abstract class Targeter : MonoBehaviour
    {
        protected const float SlopeToleranceRadians = Mathf.Deg2Rad * 45f;

        protected bool _initialized;

        protected RaycastHit _hitInfo;

        public RaycastHit HitInfo => _hitInfo;

        public bool DidHit { get; protected set; }

        public abstract bool ValidTarget { get; }

        public Vector3 TargetPosition => !DidHit ? Vector3.zero : GetHitGeometryTargetPosition();

        public Transform Origin { get; protected set; }

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
            Clean();
        }

        public abstract void Init(Hand targetingHand);

        /// <summary>
        /// Custom Targeting Logic to be implemented by children. Updates DitHit, _hitInfo, and HitTeleportObject.
        /// </summary>
        public abstract void Tick(Quaternion inputRotation);

        public abstract void Kill();

        public virtual void Clean()
        {
            _hitInfo = default;
            DidHit = false;
            Origin = null;
            _initialized = false;
        }

        protected virtual Vector3 GetHitGeometryTargetPosition()
        {
            if (!DidHit) return Vector3.zero;

            if (Vector3.Dot(_hitInfo.normal, Vector3.up) < Mathf.Cos(SlopeToleranceRadians))
            {
                // Hit surface is at a slope greater than 45deg.
                // In this case, we'll snap the target to the top of the surface's collider.
                return new Vector3(
                    _hitInfo.point.x,
                    _hitInfo.collider.bounds.max.y,
                    _hitInfo.point.z);
            }

            return _hitInfo.point;
        }
    }
}
