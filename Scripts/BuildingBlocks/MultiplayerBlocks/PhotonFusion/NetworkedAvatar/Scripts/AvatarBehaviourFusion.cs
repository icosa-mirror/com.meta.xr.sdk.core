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
using Fusion;
using Meta.XR.MultiplayerBlocks.Shared;

namespace Meta.XR.MultiplayerBlocks.Fusion
{
    public class AvatarBehaviourFusion : NetworkBehaviour, IAvatarBehaviour
    {
        private const float LERP_TIME = 0.5f;

        [Networked, OnChangedRender(nameof(OnAvatarIdChanged))] public ulong OculusId { get; set; }
        [Networked, OnChangedRender(nameof(OnAvatarIdChanged))] public int LocalAvatarIndex { get; set; }
        private Transform _cameraRig;

#if META_AVATAR_SDK_DEFINED
        private AvatarEntity _avatar;
#endif // META_AVATAR_SDK_DEFINED
        public override void Spawned()
        {
            if (OVRManager.instance)
            {
                _cameraRig = OVRManager.instance.GetComponentInChildren<OVRCameraRig>().transform;
            }
#if META_AVATAR_SDK_DEFINED
            _avatar = GetComponent<AvatarEntity>();
            if (_avatar == null)
            {
                _avatar = gameObject.AddComponent<AvatarEntity>();
            }
#endif // META_AVATAR_SDK_DEFINED
        }

        private void OnAvatarIdChanged()
        {
#if META_AVATAR_SDK_DEFINED
            if (_avatar != null)
            {
                _avatar.ReloadAvatarManually();
            }
#endif // META_AVATAR_SDK_DEFINED
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.Proxies, InvokeLocal = false, Channel = RpcChannel.Unreliable)]
        private void RPC_RecieveStreamData(byte[] bytes)
        {
#if META_AVATAR_SDK_DEFINED
            _avatar.AddToStreamDataList(bytes);
#endif // META_AVATAR_SDK_DEFINED
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority)
            {
                return;
            }

            if (_cameraRig == null)
            {
                return;
            }

            var t = transform;
            transform.position = Vector3.Lerp(t.position, _cameraRig.position, LERP_TIME);
            transform.rotation = Quaternion.Lerp(t.rotation, _cameraRig.rotation, LERP_TIME);
        }

        #region IAvatarBehaviour

        public void ReceiveStreamData(byte[] bytes)
        {
            RPC_RecieveStreamData(bytes);
        }

        public bool ShouldReduceLOD(int nAvatars) => true; // For Fusion LOD must be low as RPC maximum payload size is 512 bytes

        public bool DynamicLOD { get; set; }

        #endregion
    }
}
