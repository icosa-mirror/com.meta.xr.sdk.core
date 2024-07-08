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
    public class SpatialAnchorCoreBuildingBlock : MonoBehaviour
    {
        public UnityEvent<OVRSpatialAnchor, OVRSpatialAnchor.OperationResult> OnAnchorCreateCompleted { get => _onAnchorCreateCompleted; set => _onAnchorCreateCompleted = value; }
        public UnityEvent<List<OVRSpatialAnchor>> OnAnchorsLoadCompleted { get => _onAnchorsLoadCompleted; set => _onAnchorsLoadCompleted = value; }
        public UnityEvent<OVRSpatialAnchor.OperationResult> OnAnchorsEraseAllCompleted { get => _onAnchorsEraseAllCompleted; set => _onAnchorsEraseAllCompleted = value; }
        public UnityEvent<OVRSpatialAnchor, OVRSpatialAnchor.OperationResult> OnAnchorEraseCompleted { get => _onAnchorEraseCompleted; set => _onAnchorEraseCompleted = value; }

        [Header("# Events")]
        [SerializeField] private UnityEvent<OVRSpatialAnchor, OVRSpatialAnchor.OperationResult> _onAnchorCreateCompleted;
        [SerializeField] private UnityEvent<List<OVRSpatialAnchor>> _onAnchorsLoadCompleted;
        [SerializeField] private UnityEvent<OVRSpatialAnchor.OperationResult> _onAnchorsEraseAllCompleted;
        [SerializeField] private UnityEvent<OVRSpatialAnchor, OVRSpatialAnchor.OperationResult> _onAnchorEraseCompleted;

        protected OVRSpatialAnchor.OperationResult Result { get; set; } = OVRSpatialAnchor.OperationResult.Success;

        protected virtual OVRSpatialAnchor.EraseOptions EraseOptions => new() { Storage = OVRSpace.StorageLocation.Local };

        /// <summary>
        /// Create an spatial anchor.
        /// </summary>
        /// <param name="prefab">A prefab to add the <see cref="OVRSpatialAnchor"/> component.</param>
        /// <param name="position">Position for the new anchor.</param>
        /// <param name="rotation">Orientation of the new anchor</param>
        public void InstantiateSpatialAnchor(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                prefab = new GameObject("Spatial Anchor");
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
            OnAnchorCreateCompleted?.Invoke(anchor, Result);
        }

        protected IEnumerator WaitForInit(OVRSpatialAnchor anchor)
        {
            float timeoutThreshold = 5f;
            float startTime = Time.time;

            while (anchor && !anchor.Created)
            {
                if (Time.time - startTime >= timeoutThreshold)
                {
                    Debug.LogWarning($"[{nameof(SpatialAnchorCoreBuildingBlock)}] Failed to create the spatial anchor.");
                    Result = OVRSpatialAnchor.OperationResult.Failure;
                    yield break;
                }
                yield return null;
            }

            if (anchor == null)
            {
                Debug.LogWarning($"[{nameof(SpatialAnchorCoreBuildingBlock)}] Failed to create the spatial anchor.");
                Result = OVRSpatialAnchor.OperationResult.Failure;
            }
        }

        protected IEnumerator SaveLocalAsync(OVRSpatialAnchor anchor)
        {
            var saveOption = new OVRSpatialAnchor.SaveOptions
            {
                Storage = OVRSpace.StorageLocation.Local
            };
            using var _ = new OVRObjectPool.ListScope<OVRSpatialAnchor>(out var anchors);
            anchors.Add(anchor);

            var task = OVRSpatialAnchor.SaveAsync(anchors, saveOption);
            while (!task.IsCompleted)
                yield return null;

            if (!task.TryGetResult(out var result))
            {
                Debug.LogWarning($"[{nameof(SpatialAnchorCoreBuildingBlock)}] Failed to save the spatial anchor.");
                Result = result;
            }
        }

        /// <summary>
        /// Load and instantiate anchors from a list of uuids.
        /// </summary>
        /// <param name="prefab">Prefab for instantiating the loaded anchors.</param>
        /// <param name="uuids">List of anchor's uuid to load.</param>
        public void LoadAndInstantiateAnchors(GameObject prefab, List<Guid> uuids)
        {
            if (uuids == null)
            {
                throw new ArgumentNullException();
            }

            if (uuids.Count == 0)
            {
                Debug.Log($"[{nameof(SpatialAnchorCoreBuildingBlock)}] Uuid list is empty.");
                return;
            }

            var options = new OVRSpatialAnchor.LoadOptions
            {
                Timeout = 0,
                StorageLocation = OVRSpace.StorageLocation.Local,
                Uuids = uuids
            };

            StartCoroutine(LoadAnchorsRoutine(prefab, options));
        }

        /// <summary>
        /// Erase all instantiated anchors anchors.
        /// </summary>
        /// <remarks>It'll collect the uuid(s) of the instantiated anchor(s) and erase them.</remarks>
        public void EraseAllAnchors()
        {
            // Nothing to erase.
            if (OVRSpatialAnchor.SpatialAnchors.Count == 0)
                return;

            StartCoroutine(EraseAnchorsRoutine());
        }

        /// <summary>
        /// Erase a anchor by <see cref="Guid"/>.
        /// </summary>
        /// <param name="uuid">Anchor's uuid to erase.</param>
        public void EraseAnchorByUuid(Guid uuid)
        {
            // Nothing to erase.
            if (OVRSpatialAnchor.SpatialAnchors.Count == 0)
                return;

            if (!OVRSpatialAnchor.SpatialAnchors.TryGetValue(uuid, out var anchor))
            {
                Debug.LogWarning($"[{nameof(SpatialAnchorCoreBuildingBlock)}] Spatial anchor with uuid [{uuid}] not found.");
                return;
            }

            StartCoroutine(EraseAnchorByUuidRoutine(anchor));
        }

        protected IEnumerator LoadAnchorsRoutine(GameObject prefab, OVRSpatialAnchor.LoadOptions options)
        {
            // Load unbounded anchors
            var task = OVRSpatialAnchor.LoadUnboundAnchorsAsync(options);
            while (!task.IsCompleted)
                yield return null;

            var unboundAnchors = task.GetResult();
            if (unboundAnchors == null || unboundAnchors.Length == 0)
            {
                Debug.LogWarning($"[{nameof(SpatialAnchorCoreBuildingBlock)}] Failed to load the anchors.");
                yield break;
            }

            // Localize the anchors
            using var _ = new OVRObjectPool.ListScope<OVRSpatialAnchor>(out var loadedAnchors);
            foreach (var unboundAnchor in unboundAnchors)
            {
                if (!unboundAnchor.Localized)
                {
                    var localizeTask = unboundAnchor.LocalizeAsync();
                    while (!localizeTask.IsCompleted)
                        yield return null;

                    if (!localizeTask.GetResult())
                    {
                        Debug.LogWarning($"[{nameof(SpatialAnchorCoreBuildingBlock)}] Failed to localize the anchor. Uuid: {unboundAnchor.Uuid}");
                        continue;
                    }
                }

                var spatialAnchorGo = Instantiate(prefab, unboundAnchor.Pose.position, unboundAnchor.Pose.rotation);
                var anchor = spatialAnchorGo.AddComponent<OVRSpatialAnchor>();
                unboundAnchor.BindTo(anchor);
                loadedAnchors.Add(anchor);
            }

            OnAnchorsLoadCompleted?.Invoke(loadedAnchors);
        }

        private IEnumerator EraseAnchorsRoutine()
        {
            using var _ = new OVRObjectPool.ListScope<OVRSpatialAnchor>(out var anchorsToErase);
            foreach (var value in OVRSpatialAnchor.SpatialAnchors.Values)
            {
                anchorsToErase.Add(value);
            }

            for (int i = 0; i < anchorsToErase.Count; i++)
            {
                var anchor = anchorsToErase[i];
                yield return EraseAnchorByUuidRoutine(anchor);
            }

            var result = OVRSpatialAnchor.SpatialAnchors.Count == 0
                ? OVRSpatialAnchor.OperationResult.Success
                : OVRSpatialAnchor.OperationResult.Failure;
            OnAnchorsEraseAllCompleted?.Invoke(result);
        }

        private IEnumerator EraseAnchorByUuidRoutine(OVRSpatialAnchor anchor)
        {
            var task = anchor.EraseAsync(EraseOptions);
            while (!task.IsCompleted)
                yield return null;

            if (!task.GetResult())
            {
                OnAnchorEraseCompleted?.Invoke(anchor, OVRSpatialAnchor.OperationResult.Failure);
                yield break;
            }

            Destroy(anchor.gameObject);
            if (OVRSpatialAnchor.SpatialAnchors.ContainsKey(anchor.Uuid))
                yield return null;

            OnAnchorEraseCompleted?.Invoke(anchor, OVRSpatialAnchor.OperationResult.Success);
        }

        internal static List<SpatialAnchorCoreBuildingBlock> GetBaseInstances()
        {
            var baseClassObjects = OVRObjectPool.List<SpatialAnchorCoreBuildingBlock>();
            var objects = FindObjectsByType<SpatialAnchorCoreBuildingBlock>(FindObjectsSortMode.None);

            foreach (var obj in objects)
            {
                if (obj != null && obj.GetType() == typeof(SpatialAnchorCoreBuildingBlock))
                    baseClassObjects.Add(obj);
            }

            return baseClassObjects;
        }
    }
}
