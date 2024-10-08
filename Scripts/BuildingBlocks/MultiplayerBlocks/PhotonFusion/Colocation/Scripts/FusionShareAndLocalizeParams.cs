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
using Fusion;

namespace Meta.XR.MultiplayerBlocks.Colocation.Fusion
{
    /// <summary>
    ///     A Photon Fusion wrapper for ShareAndLocalizeParams
    ///     Used to be able to serialize and send the ShareAndLocalizeParams data over the network
    /// </summary>
    internal struct FusionShareAndLocalizeParams : INetworkStruct
    {
        public ulong requestingPlayerId;
        public ulong requestingPlayerOculusId;
        public NetworkString<_64> anchorUUID;
        public NetworkBool anchorFlowSucceeded;

        public FusionShareAndLocalizeParams(ShareAndLocalizeParams data)
        {
            requestingPlayerId = data.requestingPlayerId;
            requestingPlayerOculusId = data.requestingPlayerOculusId;
            anchorUUID = data.anchorUUID.ToString();
            anchorFlowSucceeded = data.anchorFlowSucceeded;
        }

        public ShareAndLocalizeParams GetShareAndLocalizeParams()
        {
            if (!Guid.TryParse(anchorUUID.ToString(), out var uuid))
            {
                Logger.Log("Failed to parse shared Anchor UUID string from network", LogLevel.Error);
            }
            return new ShareAndLocalizeParams(
                requestingPlayerId, requestingPlayerOculusId, uuid, anchorFlowSucceeded);
        }
    }
}
