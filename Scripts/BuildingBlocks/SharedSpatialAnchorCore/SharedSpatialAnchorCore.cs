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
using UnityEngine;
using UnityEngine.Events;

namespace Meta.XR.BuildingBlocks
{
    public class SharedSpatialAnchorCore : SpatialAnchorCoreBuildingBlock
    {
        public UnityEvent<List<OVRSpatialAnchor>, OVRSpatialAnchor.OperationResult> OnSpatialAnchorsShareCompleted
        {
            get => _onSpatialAnchorsShareCompleted;
            set => _onSpatialAnchorsShareCompleted = value;
        }

        [SerializeField] private UnityEvent<List<OVRSpatialAnchor>, OVRSpatialAnchor.OperationResult> _onSpatialAnchorsShareCompleted;

        private Action<OVRSpatialAnchor.OperationResult, IEnumerable<OVRSpatialAnchor>> _onShareCompleted;

        protected override OVRSpatialAnchor.EraseOptions EraseOptions => new() { Storage = OVRSpace.StorageLocation.Cloud };

        private void Start() => _onShareCompleted += OnShareCompleted;

        public new void InstantiateSpatialAnchor(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                prefab = new GameObject("Shared Spatial Anchor");
            }

            var anchorGameObject = Instantiate(prefab, position, rotation);
            var spatialAnchor = anchorGameObject.AddComponent<OVRSpatialAnchor>();
            StartCoroutine(InitSpatialAnchor(spatialAnchor));
        }

        private IEnumerator InitSpatialAnchor(OVRSpatialAnchor anchor)
        {
            yield return WaitForInit(anchor);
            if (Result == OVRSpatialAnchor.OperationResult.Failure)
            {
                OnAnchorCreateCompleted?.Invoke(anchor, Result);
                yield break;
            }

            yield return SaveLocalAsync(anchor);
            if (Result == OVRSpatialAnchor.OperationResult.Failure)
            {
                OnAnchorCreateCompleted?.Invoke(anchor, Result);
                yield break;
            }

            yield return SaveCloudAsync(anchor);
            OnAnchorCreateCompleted?.Invoke(anchor, Result);
        }

        public new void LoadAndInstantiateAnchors(GameObject prefab, List<Guid> uuids)
        {
            if (uuids == null)
            {
                throw new ArgumentNullException();
            }

            if (uuids.Count == 0)
            {
                throw new ArgumentException($"[{nameof(SpatialAnchorCoreBuildingBlock)}] Uuid list is empty.");
            }

            var options = new OVRSpatialAnchor.LoadOptions
            {
                Timeout = 0,
                StorageLocation = OVRSpace.StorageLocation.Cloud,
                Uuids = uuids
            };

            StartCoroutine(LoadAnchorsRoutine(prefab, options));
        }

        private IEnumerator SaveCloudAsync(OVRSpatialAnchor anchor)
        {
            var saveOption = new OVRSpatialAnchor.SaveOptions
            {
                Storage = OVRSpace.StorageLocation.Cloud
            };

            using var _ = new OVRObjectPool.ListScope<OVRSpatialAnchor>(out var anchors);
            anchors.Add(anchor);

            var task = OVRSpatialAnchor.SaveAsync(anchors, saveOption);
            while (!task.IsCompleted)
                yield return null;

            if (!task.TryGetResult(out var result))
            {
                Result = OVRSpatialAnchor.OperationResult.Failure;
                yield break;
            }

            if (result != OVRSpatialAnchor.OperationResult.Success)
            {
                Result = result;
            }
        }

        public void ShareSpatialAnchors(List<OVRSpatialAnchor> anchors, List<OVRSpaceUser> users)
        {
            if (anchors == null || users == null)
            {
                throw new ArgumentNullException();
            }

            if (anchors.Count == 0 || users.Count == 0)
            {
                throw new ArgumentException($"[{nameof(SharedSpatialAnchorCore)}] Anchors or users cannot be zero.");
            }

            OVRSpatialAnchor.ShareAsync(anchors, users).ContinueWith(_onShareCompleted, anchors);
        }

        private void OnShareCompleted(OVRSpatialAnchor.OperationResult result, IEnumerable<OVRSpatialAnchor> anchors)
        {
            if (result != OVRSpatialAnchor.OperationResult.Success)
            {
                OnSpatialAnchorsShareCompleted?.Invoke(null, result);
                return;
            }

            using var _ = new OVRObjectPool.ListScope<OVRSpatialAnchor>(out var sharedAnchors);
            sharedAnchors.AddRange(anchors);

            OnSpatialAnchorsShareCompleted?.Invoke(sharedAnchors, OVRSpatialAnchor.OperationResult.Success);
        }

        private void OnDestroy() => _onShareCompleted -= OnShareCompleted;
    }
}
