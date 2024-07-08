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
using System.Collections.Generic;
using Meta.XR.MultiplayerBlocks.Shared;
using Unity.Netcode;

namespace Meta.XR.MultiplayerBlocks.NGO
{
    public class AvatarBehaviourNGO : NetworkBehaviour, IAvatarBehaviour
    {
        private NetworkVariable<ulong> _oculusId = new();
        private NetworkVariable<int> _localAvatarIndex = new();
        private Transform _cameraRig;
        private readonly List<ulong> _targetClientIds = new();
#if META_AVATAR_SDK_DEFINED
        private AvatarEntity _avatarEntity;
#endif // META_AVATAR_SDK_DEFINED
        private void Awake()
        {
            if (OVRManager.instance)
            {
                _cameraRig = OVRManager.instance.GetComponentInChildren<OVRCameraRig>().transform;
            }

#if META_AVATAR_SDK_DEFINED
            _avatarEntity = GetComponent<AvatarEntity>();
            if (_avatarEntity == null)
            {
                _avatarEntity = gameObject.AddComponent<AvatarEntity>();
            }
#endif // META_AVATAR_SDK_DEFINED
        }

        public override void OnNetworkSpawn()
        {
            _oculusId.OnValueChanged += OnAvatarIdChanged;
            _localAvatarIndex.OnValueChanged += OnAvatarIdChanged;
        }

        public override void OnNetworkDespawn()
        {
            _oculusId.OnValueChanged -= OnAvatarIdChanged;
            _localAvatarIndex.OnValueChanged -= OnAvatarIdChanged;
        }

        private void OnAvatarIdChanged<T>(T prev, T val) where T : IEquatable<T>
        {
#if META_AVATAR_SDK_DEFINED
            if (_avatarEntity != null && !prev.Equals(val))
            {
                _avatarEntity.ReloadAvatarManually();
            }
#endif // META_AVATAR_SDK_DEFINED
        }

        private void FixedUpdate()
        {
            if (!IsOwner)
            {
                return;
            }

            if (_cameraRig == null)
            {
                return;
            }

            var t = transform;
            t.position = _cameraRig.position;
            t.rotation = _cameraRig.rotation;
        }

        [ServerRpc]
        private void ReceiveStreamDataServerRpc(byte[] bytes, ServerRpcParams serverRpcParams = default)
        {
            _targetClientIds.Clear();
            foreach (var (clientId, _) in NetworkManager.ConnectedClients)
            {
                if (clientId == serverRpcParams.Receive.SenderClientId)
                {
                    continue;
                }

                _targetClientIds.Add(clientId);
            }

            ReceiveStreamDataClientRpc(bytes, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = _targetClientIds
                }
            });
        }

        [ClientRpc]
        private void ReceiveStreamDataClientRpc(byte[] bytes, ClientRpcParams clientRpcParams = default)
        {
#if META_AVATAR_SDK_DEFINED
            if (_avatarEntity != null)
            {
                _avatarEntity.AddToStreamDataList(bytes);
            }
#endif // META_AVATAR_SDK_DEFINED
        }

        #region IAvatarBehaviour
        public ulong OculusId
        {
            get => _oculusId.Value;
            set => _oculusId.Value = value;
        }

        public int LocalAvatarIndex
        {
            get => _localAvatarIndex.Value;
            set => _localAvatarIndex.Value = value;
        }

        public bool HasInputAuthority => IsOwner;
        public void ReceiveStreamData(byte[] bytes)
        {
            ReceiveStreamDataServerRpc(bytes);
        }

        public bool ShouldReduceLOD(int nAvatars) => DynamicLOD && nAvatars >= 3;

        public bool DynamicLOD { get; set; }

        #endregion
    }
}
