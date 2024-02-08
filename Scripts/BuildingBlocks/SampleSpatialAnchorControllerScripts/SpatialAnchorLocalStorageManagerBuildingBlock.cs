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
using System.Collections.Generic;
using UnityEngine;

namespace Meta.XR.BuildingBlocks
{
    /// <summary>
    /// Anchor's uuid on local storage manager.
    /// </summary>
    public class SpatialAnchorLocalStorageManagerBuildingBlock : MonoBehaviour
    {
        private SpatialAnchorCoreBuildingBlock _spatialAnchorCore;
        private List<Guid> _uuids = OVRObjectPool.Get<List<Guid>>();

        private const string NumUuidsPlayerPref = "numUuids";

        private void Start()
        {
            _spatialAnchorCore = FindObjectOfType<SpatialAnchorCoreBuildingBlock>();
            _spatialAnchorCore.OnAnchorCreateCompleted.AddListener(SaveAnchorUuidToLocalStorage);
            _spatialAnchorCore.OnAnchorEraseCompleted.AddListener(RemoveAnchorFromLocalStorage);
        }

        internal void SaveAnchorUuidToLocalStorage(OVRSpatialAnchor anchor)
        {
            if (!PlayerPrefs.HasKey(NumUuidsPlayerPref))
            {
                PlayerPrefs.SetInt(NumUuidsPlayerPref, 0);
            }

            int playerNumUuids = PlayerPrefs.GetInt(NumUuidsPlayerPref);
            PlayerPrefs.SetString("uuid" + playerNumUuids, anchor.Uuid.ToString());
            PlayerPrefs.SetInt(NumUuidsPlayerPref, ++playerNumUuids);
        }

        internal void RemoveAnchorFromLocalStorage(Guid uuid)
        {
            var playerUuidCount = PlayerPrefs.GetInt(NumUuidsPlayerPref, 0);
            for (int i = 0; i < playerUuidCount; i++)
            {
                var key = "uuid" + i;
                var value = PlayerPrefs.GetString(key, "");
                if (value.Equals(uuid.ToString()))
                {
                    var lastKey = "uuid" + (playerUuidCount - 1);
                    var lastValue = PlayerPrefs.GetString(lastKey);
                    PlayerPrefs.SetString(key, lastValue);
                    PlayerPrefs.DeleteKey(lastKey);

                    playerUuidCount--;
                    if (playerUuidCount < 0) playerUuidCount = 0;
                    PlayerPrefs.SetInt(NumUuidsPlayerPref, playerUuidCount);
                    break;
                }
            }
        }

        /// <summary>
        /// Load spatial anchors stored in local storage.
        /// </summary>
        internal List<Guid> GetAnchorAnchorUuidFromLocalStorage()
        {
            // Get number of saved anchor uuids
            if (!PlayerPrefs.HasKey(NumUuidsPlayerPref))
            {
                Reset();
                Debug.Log($"[{nameof(SpatialAnchorLocalStorageManagerBuildingBlock)}] Anchor not found.");
                return null;
            }

            // Load unbounded anchors
            _uuids.Clear();
            var playerUuidCount = PlayerPrefs.GetInt(NumUuidsPlayerPref);
            for (int i = 0; i < playerUuidCount; ++i)
            {
                var uuidKey = "uuid" + i;
                if (!PlayerPrefs.HasKey(uuidKey))
                    continue;

                var currentUuid = PlayerPrefs.GetString(uuidKey);
                _uuids.Add(new Guid(currentUuid));
            }

            return _uuids;
        }

        public void Reset()
        {
            PlayerPrefs.SetInt(NumUuidsPlayerPref, 0);
        }

        private void OnDestroy()
        {
            _spatialAnchorCore.OnAnchorCreateCompleted.RemoveAllListeners();
            OVRObjectPool.Return(_uuids);
        }
    }
}
