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
using System.Collections;
using System.Collections.Generic;
using Meta.XR.BuildingBlocks;
using UnityEngine;
#if META_AVATAR_SDK_DEFINED
using Oculus.Avatar2;
#endif // META_AVATAR_SDK_DEFINED

namespace Meta.XR.MultiplayerBlocks.Shared
{
    public interface IAvatarBehaviour
    {
        // synced to network, can be 0 if user not entitled
        public ulong OculusId { get; }
        // synced to network, indicating which avatar from sample assets is used
        // this should be initialized randomly for each user before entity spawned
        public int LocalAvatarIndex { get; }
        public bool HasInputAuthority { get; }
        public void ReceiveStreamData(byte[] bytes);
        public bool ShouldReduceLOD(int nAvatars);
    }

#if META_AVATAR_SDK_DEFINED
    /// <summary>
    /// Avatar Entity implementation for Networked Avatar, loads remote/local avatar according to IAvatarBehaviour
    /// and also provide fallback solution to local zip avatar with a randomized preloaded avatar from sample assets
    /// when the user is not entitled (no Oculus Id) or has no avatar setup
    /// </summary>
    public class AvatarEntity : OvrAvatarEntity
    {
        [SerializeField] private StreamLOD streamLOD = StreamLOD.Medium;
        [SerializeField] private float intervalToSendDataInSec = 0.08f;

        private static int _avatarCount;

        private readonly List<byte[]> _streamedDataArray = new();
        private const int MaxBytesToLog = 5;
        private float _cycleStartTime;
        private bool _skeletonLoaded;
        private bool _initialAvatarLoaded;
        private IAvatarBehaviour _avatarBehaviour;

        private float IntervalToSendDataInSec => _avatarBehaviour.ShouldReduceLOD(_avatarCount) ? intervalToSendDataInSec * 2 : intervalToSendDataInSec;

        /// <summary>
        /// Could be triggered by any changes like oculus id, local avatar index, network connection etc.
        /// </summary>
        public void ReloadAvatarManually()
        {
            if (!_initialAvatarLoaded)
            {
                return;
            }

            _skeletonLoaded = false;
            EntityActive = false;
            Teardown();
            CreateEntity();
            LoadAvatar();
        }

        protected override void Awake()
        {
            _avatarBehaviour = this.GetInterfaceComponent<IAvatarBehaviour>();
            if (_avatarBehaviour == null)
            {
                throw new InvalidOperationException("Using AvatarEntity without an IAvatarBehaviour");
            }
        }

        private void OnEnable()
        {
            _avatarCount++;
        }

        private void OnDisable()
        {
            _avatarCount--;
        }

        private void Start()
        {
            if (_avatarBehaviour == null)
            {
                return;
            }

            ConfigureAvatar();
            base.Awake(); // creating avatar entity here

            if (!_avatarBehaviour.HasInputAuthority)
            {
                SetActiveView(CAPI.ovrAvatar2EntityViewFlags.ThirdPerson);
            }

            LoadAvatar();
            _initialAvatarLoaded = true;
        }

        private void ConfigureAvatar()
        {
            if (_avatarBehaviour.HasInputAuthority)
            {
                SetIsLocal(true);
                _creationInfo.features = CAPI.ovrAvatar2EntityFeatures.Preset_Default;
                var entityInputManager = OvrAvatarManager.Instance.gameObject.GetComponent<EntityInputManager>();
                SetBodyTracking(entityInputManager);
                var lipSyncInput = FindObjectOfType<OvrAvatarLipSyncContext>();
                SetLipSync(lipSyncInput);
                gameObject.name = "LocalAvatar";
            }
            else
            {
                SetIsLocal(false);
                _creationInfo.features = CAPI.ovrAvatar2EntityFeatures.Preset_Remote;
                gameObject.name = "RemoteAvatar";
            }
        }

        private void LoadAvatar()
        {
            if (_avatarBehaviour.OculusId == 0)
            {
                LoadLocalAvatar();
            }
            else
            {
                StartCoroutine(TryToLoadUserAvatar());
            }
        }

        private IEnumerator TryToLoadUserAvatar()
        {
            while (!OvrAvatarEntitlement.AccessTokenIsValid())
            {
                yield return null;
            }
            _userId = _avatarBehaviour.OculusId;
            var hasAvatarRequest = OvrAvatarManager.Instance.UserHasAvatarAsync(_userId);
            while (hasAvatarRequest.IsCompleted == false)
            {
                yield return null;
            }
            if (hasAvatarRequest.Result == OvrAvatarManager.HasAvatarRequestResultCode.HasAvatar)
            {
                LoadUser();
            }
            else // fallback to local avatar
            {
                LoadLocalAvatar();
            }
        }

        private void LoadLocalAvatar()
        {
#if META_AVATAR_SAMPLE_ASSETS_DEFINED
            // we only load local avatar from zip after Avatar Sample Assets is installed
            var assetPath = $"{_avatarBehaviour.LocalAvatarIndex}{GetAssetPostfix()}";
            LoadAssets(new[] { assetPath }, AssetSource.Zip);
#else
            Debug.LogWarning("Meta Avatar Sample Assets package not installed, local avatar cannot be loaded from zip");
#endif // META_AVATAR_SAMPLE_ASSETS_DEFINED
        }

        private string GetAssetPostfix(bool isFromZip = true) {
            return "_" + OvrAvatarManager.Instance.GetPlatformGLBPostfix(_creationInfo.renderFilters.quality, isFromZip)
                       + OvrAvatarManager.Instance.GetPlatformGLBVersion(_creationInfo.renderFilters.quality, isFromZip)
                       + OvrAvatarManager.Instance.GetPlatformGLBExtension(isFromZip);
        }

        protected override void OnSkeletonLoaded()
        {
            base.OnSkeletonLoaded();
            _skeletonLoaded = true;
        }

        private void Update()
        {
            if (!_skeletonLoaded || _streamedDataArray.Count <= 0 || IsLocal) return;
            var firstBytesInList = _streamedDataArray[0];
            if (firstBytesInList != null)
            {
                //Apply the remote avatar state and smooth the animation
                ApplyStreamData(firstBytesInList);
                SetPlaybackTimeDelay(IntervalToSendDataInSec / 2);
            }
            _streamedDataArray.RemoveAt(0);
        }

        private void LateUpdate()
        {
            if (!_skeletonLoaded)
            {
                return;
            }

            var elapsedTime = Time.time - _cycleStartTime;
            if (elapsedTime > IntervalToSendDataInSec)
            {
                RecordAndSendStreamDataIfHasAuthority();
                _cycleStartTime = Time.time;
            }
        }

        private void RecordAndSendStreamDataIfHasAuthority()
        {
            if (!IsLocal || _avatarBehaviour == null)
            {
                return;
            }

            var bytes = RecordStreamData(_avatarBehaviour.ShouldReduceLOD(_avatarCount) ? StreamLOD.Low : streamLOD);
            _avatarBehaviour.ReceiveStreamData(bytes);
        }

        public void AddToStreamDataList(byte[] bytes)
        {
            if (_streamedDataArray.Count == MaxBytesToLog)
            {
                _streamedDataArray.RemoveAt(_streamedDataArray.Count - 1);
            }

            _streamedDataArray.Add(bytes);
        }
    }
#endif // META_AVATAR_SDK_DEFINED
}
