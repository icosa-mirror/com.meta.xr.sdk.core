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
using Unity.Collections;
using static OVRPlugin;

partial struct OVRAnchor
{
    /// <summary>
    /// (Obsolete) Asynchronous method that fetches anchors with a specific component.
    /// </summary>
    /// <typeparam name="T">The type of component the fetched anchor must have.</typeparam>
    /// <param name="anchors">IList that will get cleared and populated with the requested anchors.</param>s
    /// <param name="location">Storage location to query</param>
    /// <param name="maxResults">The maximum number of results the query can return</param>
    /// <param name="timeout">Timeout in seconds for the query.</param>
    /// <remarks>
    /// \deprecated This method is obsolete. Use
    /// <see cref="FetchAnchorsAsync(List{OVRAnchor},FetchOptions,Action{List{OVRAnchor}, int})"/> instead.
    ///
    /// Dispose of the returned <see cref="OVRTask{T}"/> if you don't use the results
    /// </remarks>
    /// <returns>An <see cref="OVRTask{T}"/> that will eventually let you test if the fetch was successful or not.
    /// If the result is true, then the <see cref="anchors"/> parameter has been populated with the requested anchors.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="anchors"/> is `null`.</exception>
    [Obsolete("Use the overload of FetchAnchorsAsync that accepts a FetchOptions parameter")]
    public static OVRTask<bool> FetchAnchorsAsync<T>(IList<OVRAnchor> anchors,
        OVRSpace.StorageLocation location = OVRSpace.StorageLocation.Local,
        int maxResults = OVRSpaceQuery.Options.MaxUuidCount, double timeout = 0.0)
        where T : struct, IOVRAnchorComponent<T>
    {
        if (anchors == null)
        {
            throw new ArgumentNullException(nameof(anchors));
        }

        return FetchAnchorsAsync(default(T).Type, anchors, location, maxResults, timeout);
    }

    /// <summary>
    /// (Obsolete) Asynchronous method that fetches anchors with specifics uuids.
    /// </summary>
    /// <param name="uuids">Enumerable of uuids that anchors fetched must verify</param>
    /// <param name="anchors">IList that will get cleared and populated with the requested anchors.</param>s
    /// <param name="location">Storage location to query</param>
    /// <param name="timeout">Timeout in seconds for the query.</param>
    /// <remarks>
    /// \deprecated This method is obsolete. Use
    /// <see cref="FetchAnchorsAsync(List{OVRAnchor},FetchOptions,Action{List{OVRAnchor}, int})"/> instead.
    ///
    /// Dispose of the returned <see cref="OVRTask{T}"/> if you don't use the results
    /// </remarks>
    /// <returns>An <see cref="OVRTask{T}"/> that will eventually let you test if the fetch was successful or not.
    /// If the result is true, then the <see cref="anchors"/> parameter has been populated with the requested anchors.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="uuids"/> is `null`.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="anchors"/> is `null`.</exception>
    [Obsolete("Use the overload of FetchAnchorsAsync that accepts a FetchOptions parameter")]
    public static OVRTask<bool> FetchAnchorsAsync(IEnumerable<Guid> uuids, IList<OVRAnchor> anchors,
        OVRSpace.StorageLocation location = OVRSpace.StorageLocation.Local, double timeout = 0.0)
    {
        if (uuids == null)
        {
            throw new ArgumentNullException(nameof(uuids));
        }

        if (anchors == null)
        {
            throw new ArgumentNullException(nameof(anchors));
        }

        async OVRTask<bool> Execute(IEnumerable<Guid> uuids, IList<OVRAnchor> anchors,
            OVRSpace.StorageLocation location, double timeout)
            => (await FetchAnchors(anchors, GetQueryInfo(uuids, location, timeout))).IsSuccess();

        return Execute(uuids, anchors, location, timeout);
    }

    internal static OVRTask<Result> FetchAnchors(IList<OVRAnchor> anchors, OVRPlugin.SpaceQueryInfo queryInfo)
    {
        if (anchors == null)
        {
            throw new ArgumentNullException(nameof(anchors));
        }

        anchors.Clear();

        var telemetryMarker = OVRTelemetry
            .Start((int)Telemetry.MarkerId.QuerySpaces)
            .AddAnnotation(Telemetry.Annotation.Timeout, (double)queryInfo.Timeout)
            .AddAnnotation(Telemetry.Annotation.MaxResults, (long)queryInfo.MaxQuerySpaces)
            .AddAnnotation(Telemetry.Annotation.StorageLocation, (long)queryInfo.Location);

        if (queryInfo is { FilterType: SpaceQueryFilterType.Components, ComponentsInfo: { Components: { Length: > 0 } } })
        {
            unsafe
            {
                var componentTypes = stackalloc long[queryInfo.ComponentsInfo.NumComponents];
                for (var i = 0; i < queryInfo.ComponentsInfo.NumComponents; i++)
                {
                    componentTypes[i] = (long)queryInfo.ComponentsInfo.Components[i];
                }
                telemetryMarker.AddAnnotation(Telemetry.Annotation.ComponentTypes, componentTypes,
                    queryInfo.ComponentsInfo.NumComponents);
            }
        }
        else if (queryInfo is { FilterType: SpaceQueryFilterType.Ids })
        {
            telemetryMarker.AddAnnotation(Telemetry.Annotation.UuidCount, (long)queryInfo.IdInfo.NumIds);
        }

        var result = QuerySpacesWithResult(queryInfo, out var requestId);
        Telemetry.SetSyncResult(telemetryMarker, requestId, result);

        if (!result.IsSuccess())
        {
            return OVRTask.FromResult(result);
        }

        var task = OVRTask.FromRequest<Result>(requestId);
        task.SetInternalData(anchors);
        return task;
    }

    internal static void OnSpaceQueryComplete(OVRDeserialize.SpaceQueryCompleteData data)
    {
        OVRTelemetryMarker? telemetryMarker = null;
        var task = OVRTask.GetExisting<Result>(data.RequestId);
        Result? taskResult = null;
        try
        {
            telemetryMarker =
                Telemetry.SetAsyncResult(Telemetry.MarkerId.QuerySpaces, data.RequestId, (long)data.Result);

            var requestId = data.RequestId;
            if (!task.IsPending)
            {
                return;
            }

            taskResult = (Result)data.Result;
            if (data.Result < 0)
            {
                // Only continue if the query succeeded
                return;
            }

            if (!task.TryGetInternalData<IList<OVRAnchor>>(out var anchors) || anchors == null)
            {
                taskResult = Result.Failure_DataIsInvalid;
                return;
            }

            if (!RetrieveSpaceQueryResults(requestId, out var rawResults, Allocator.Temp))
            {
                taskResult = Result.Failure_OperationFailed;
                return;
            }

            using (rawResults)
            {
                telemetryMarker?.AddAnnotation(Telemetry.Annotation.ResultsCount, (long)rawResults.Length);

                foreach (var result in rawResults)
                {
                    anchors.Add(new OVRAnchor(result.space, result.uuid));
                }

                taskResult = (Result)data.Result;
            }
        }
        finally
        {
            telemetryMarker?.Send();
            if (taskResult.HasValue)
            {
                task.SetResult(taskResult.Value);
            }
        }
    }

    [Obsolete]
    internal static async OVRTask<bool> FetchAnchorsAsync(SpaceComponentType type, IList<OVRAnchor> anchors,
        OVRSpace.StorageLocation location = OVRSpace.StorageLocation.Local,
        int maxResults = OVRSpaceQuery.Options.MaxUuidCount, double timeout = 0.0)
        => (await FetchAnchors(anchors, GetQueryInfo(type, location, maxResults, timeout))).IsSuccess();

    [Obsolete]
    internal static SpaceQueryInfo GetQueryInfo(SpaceComponentType type,
        OVRSpace.StorageLocation location, int maxResults, double timeout) => new OVRSpaceQuery.Options
        {
            QueryType = SpaceQueryType.Action,
            ActionType = SpaceQueryActionType.Load,
            ComponentFilter = type,
            Location = location,
            Timeout = timeout,
            MaxResults = maxResults,
        }.ToQueryInfo();

    [Obsolete]
    internal static SpaceQueryInfo GetQueryInfo(IEnumerable<Guid> uuids,
        OVRSpace.StorageLocation location, double timeout) => new OVRSpaceQuery.Options
        {
            QueryType = SpaceQueryType.Action,
            ActionType = SpaceQueryActionType.Load,
            UuidFilter = uuids,
            Location = location,
            Timeout = timeout,
            MaxResults = OVRSpaceQuery.Options.MaxUuidCount,
        }.ToQueryInfo();

    [Obsolete]
    internal static unsafe Result SaveSpaceList(ulong* spaces, uint numSpaces, SpaceStorageLocation location,
        out ulong requestId)
    {
        var marker = OVRTelemetry
            .Start((int)Telemetry.MarkerId.SaveSpaceList)
            .AddAnnotation(Telemetry.Annotation.SpaceCount, (long)numSpaces)
            .AddAnnotation(Telemetry.Annotation.StorageLocation, (long)location);

        var result = OVRPlugin.SaveSpaceList(spaces, numSpaces, location, out requestId);

        Telemetry.SetSyncResult(marker, requestId, result);
        return result;
    }

    // Invoked by OVRManager event loop
    internal static void OnSpaceListSaveResult(OVRDeserialize.SpaceListSaveResultData eventData)
        => Telemetry.SetAsyncResultAndSend(Telemetry.MarkerId.SaveSpaceList, eventData.RequestId, eventData.Result);

    [Obsolete]
    internal static Result EraseSpace(ulong space, SpaceStorageLocation location, out ulong requestId)
    {
        var marker = OVRTelemetry
            .Start((int)Telemetry.MarkerId.EraseSingleSpace)
            .AddAnnotation(Telemetry.Annotation.StorageLocation, (long)location);

        var result = OVRPlugin.EraseSpaceWithResult(space, location, out requestId);

        Telemetry.SetSyncResult(marker, requestId, result);
        return result;
    }

    // Invoked by OVRManager event loop
    internal static void OnSpaceEraseComplete(OVRDeserialize.SpaceEraseCompleteData eventData)
        => Telemetry.SetAsyncResultAndSend(Telemetry.MarkerId.EraseSingleSpace, eventData.RequestId, eventData.Result);
}
