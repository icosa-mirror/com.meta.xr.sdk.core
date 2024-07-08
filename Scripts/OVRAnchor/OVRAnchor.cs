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
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using static OVRPlugin;

/// <summary>
/// Represents an anchor.
/// </summary>
/// <remarks>
/// Scenes anchors are uniquely identified with their <see cref="Uuid"/>.
/// <para>You may dispose of an anchor by calling their <see cref="Dispose"/> method.</para>
/// </remarks>
public readonly partial struct OVRAnchor : IEquatable<OVRAnchor>, IDisposable
{

    #region Static

    public static readonly OVRAnchor Null = new OVRAnchor(0, Guid.Empty);

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

    internal static OVRPlugin.SpaceQueryInfo GetQueryInfo(SpaceComponentType type,
        OVRSpace.StorageLocation location, int maxResults, double timeout) => new OVRSpaceQuery.Options
        {
            QueryType = OVRPlugin.SpaceQueryType.Action,
            ActionType = OVRPlugin.SpaceQueryActionType.Load,
            ComponentFilter = type,
            Location = location,
            Timeout = timeout,
            MaxResults = maxResults,
        }.ToQueryInfo();

    internal static OVRPlugin.SpaceQueryInfo GetQueryInfo(IEnumerable<Guid> uuids,
        OVRSpace.StorageLocation location, double timeout) => new OVRSpaceQuery.Options
        {
            QueryType = OVRPlugin.SpaceQueryType.Action,
            ActionType = OVRPlugin.SpaceQueryActionType.Load,
            UuidFilter = uuids,
            Location = location,
            Timeout = timeout,
            MaxResults = OVRSpaceQuery.Options.MaxUuidCount,
        }.ToQueryInfo();


    internal static OVRTask<bool> FetchAnchorsAsync(SpaceComponentType type, IList<OVRAnchor> anchors,
        OVRSpace.StorageLocation location = OVRSpace.StorageLocation.Local,
        int maxResults = OVRSpaceQuery.Options.MaxUuidCount, double timeout = 0.0)
        => FetchAnchors(anchors, GetQueryInfo(type, location, maxResults, timeout));

    /// <summary>
    /// Asynchronous method that fetches anchors with a specific component.
    /// </summary>
    /// <typeparam name="T">The type of component the fetched anchor must have.</typeparam>
    /// <param name="anchors">IList that will get cleared and populated with the requested anchors.</param>s
    /// <param name="location">Storage location to query</param>
    /// <param name="maxResults">The maximum number of results the query can return</param>
    /// <param name="timeout">Timeout in seconds for the query.</param>
    /// <remarks>Dispose of the returned <see cref="OVRTask{T}"/> if you don't use the results</remarks>
    /// <returns>An <see cref="OVRTask{T}"/> that will eventually let you test if the fetch was successful or not.
    /// If the result is true, then the <see cref="anchors"/> parameter has been populated with the requested anchors.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="anchors"/> is `null`.</exception>
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
    /// Asynchronous method that fetches anchors with specifics uuids.
    /// </summary>
    /// <param name="uuids">Enumerable of uuids that anchors fetched must verify</param>
    /// <param name="anchors">IList that will get cleared and populated with the requested anchors.</param>s
    /// <param name="location">Storage location to query</param>
    /// <param name="timeout">Timeout in seconds for the query.</param>
    /// <remarks>Dispose of the returned <see cref="OVRTask{T}"/> if you don't use the results</remarks>
    /// <returns>An <see cref="OVRTask{T}"/> that will eventually let you test if the fetch was successful or not.
    /// If the result is true, then the <see cref="anchors"/> parameter has been populated with the requested anchors.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="uuids"/> is `null`.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="anchors"/> is `null`.</exception>
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

        return FetchAnchors(anchors, GetQueryInfo(uuids, location, timeout));
    }



    private static OVRTask<bool> FetchAnchors(IList<OVRAnchor> anchors, OVRPlugin.SpaceQueryInfo queryInfo)
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
            return OVRTask.FromResult(false);
        }

        var task = OVRTask.FromRequest<bool>(requestId);
        task.SetInternalData(anchors);
        return task;
    }


    internal static void OnSpaceQueryComplete(OVRDeserialize.SpaceQueryCompleteData data)
    {
        OVRTelemetryMarker? telemetryMarker = null;
        var task = OVRTask.GetExisting<bool>(data.RequestId);
        bool? taskResult = null;
        try
        {
            telemetryMarker =
                Telemetry.SetAsyncResult(Telemetry.MarkerId.QuerySpaces, data.RequestId, (long)data.Result);

            var requestId = data.RequestId;
            if (!task.IsPending)
            {
                return;
            }

            if (!task.TryGetInternalData<IList<OVRAnchor>>(out var anchors) || anchors == null)
            {
                taskResult = false;
                return;
            }

            if (!RetrieveSpaceQueryResults(requestId, out var rawResults, Allocator.Temp))
            {
                taskResult = false;
                return;
            }

            using (rawResults)
            {
                telemetryMarker?.AddAnnotation(Telemetry.Annotation.ResultsCount, (long)rawResults.Length);

                foreach (var result in rawResults)
                {
                    anchors.Add(new OVRAnchor(result.space, result.uuid));
                }

                taskResult = true;
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

    /// <summary>
    /// Creates a new spatial anchor.
    /// </summary>
    /// <remarks>
    /// Spatial anchor creation is asynchronous. This method initiates a request to create a spatial anchor at
    /// <paramref name="trackingSpacePose"/>. The returned <see cref="OVRTask{TResult}"/> can be awaited or used to
    /// track the completion of the request.
    ///
    /// If spatial anchor creation fails, the resulting <see cref="OVRAnchor"/> will be <see cref="OVRAnchor.Null"/>.
    /// </remarks>
    /// <param name="trackingSpacePose">The pose, in tracking space, at which you wish to create the spatial anchor.</param>
    /// <returns>A task which can be used to track completion of the request.</returns>
    public static OVRTask<OVRAnchor> CreateSpatialAnchorAsync(Pose trackingSpacePose)
        => CreateSpatialAnchor(new SpatialAnchorCreateInfo
        {
            BaseTracking = GetTrackingOriginType(),
            PoseInSpace = new Posef
            {
                Orientation = trackingSpacePose.rotation.ToFlippedZQuatf(),
                Position = trackingSpacePose.position.ToFlippedZVector3f(),
            },
            Time = GetTimeInSeconds(),
        }, out var requestId)
            ? OVRTask.FromRequest<OVRAnchor>(requestId)
            : OVRTask.FromResult(Null);

    /// <summary>
    /// Creates a new spatial anchor.
    /// </summary>
    /// <remarks>
    /// Spatial anchor creation is asynchronous. This method initiates a request to create a spatial anchor at
    /// <paramref name="transform"/>. The returned <see cref="OVRTask{TResult}"/> can be awaited or used to
    /// track the completion of the request.
    ///
    /// If spatial anchor creation fails, the resulting <see cref="OVRAnchor"/> will be <see cref="OVRAnchor.Null"/>.
    /// </remarks>
    /// <param name="transform">The transform at which you wish to create the spatial anchor.</param>
    /// <param name="centerEyeCamera">The `Camera` associated with the Meta Quest's center eye.</param>
    /// <returns>A task which can be used to track completion of the request.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="transform"/> is `null`.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="centerEyeCamera"/> is `null`.</exception>
    public static OVRTask<OVRAnchor> CreateSpatialAnchorAsync(Transform transform, Camera centerEyeCamera)
    {
        if (transform == null)
            throw new ArgumentNullException(nameof(transform));

        if (centerEyeCamera == null)
            throw new ArgumentNullException(nameof(centerEyeCamera));

        var pose = transform.ToTrackingSpacePose(centerEyeCamera);
        return CreateSpatialAnchorAsync(new Pose
        {
            position = pose.position,
            rotation = pose.orientation,
        });
    }

    #endregion



    internal ulong Handle { get; }

    /// <summary>
    /// Unique Identifier representing the anchor.
    /// </summary>
    public Guid Uuid { get; }

    internal OVRAnchor(ulong handle, Guid uuid)
    {
        Handle = handle;
        Uuid = uuid;
    }

    /// <summary>
    /// Gets the anchor's component of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <returns>The requested component.</returns>
    /// <remarks>Make sure the anchor supports the specified type of component using <see cref="SupportsComponent{T}"/></remarks>
    /// <exception cref="InvalidOperationException">Thrown if the anchor doesn't support the specified type of component.</exception>
    /// <seealso cref="TryGetComponent{T}"/>
    /// <seealso cref="SupportsComponent{T}"/>
    public T GetComponent<T>() where T : struct, IOVRAnchorComponent<T>
    {
        if (!TryGetComponent<T>(out var component))
        {
            throw new InvalidOperationException($"Anchor {Uuid} does not have component {typeof(T).Name}");
        }

        return component;
    }

    /// <summary>
    /// Tries to get the anchor's component of a specific type.
    /// </summary>
    /// <param name="component">The requested component, as an <c>out</c> parameter.</param>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <returns>Whether or not the request succeeded. It may fail if the anchor doesn't support this type of component.</returns>
    /// <seealso cref="GetComponent{T}"/>
    public bool TryGetComponent<T>(out T component) where T : struct, IOVRAnchorComponent<T>
    {
        component = default;
        if (!GetSpaceComponentStatusInternal(Handle, component.Type, out _, out _).IsSuccess())
        {
            return false;
        }

        component = component.FromAnchor(this);
        return true;
    }

    /// <summary>
    /// Tests whether or not the anchor supports a specific type of component.
    /// </summary>
    /// <remarks>
    /// For performance reasons, we use xrGetSpaceComponentStatusFB, which can
    /// result in an error in the logs when the component is not available.
    ///
    /// This error does not have impact on the control flow. The alternative method,
    /// <seealso cref="GetSupportedComponents(List{SpaceComponentType})"/> avoids
    /// this error reporting, but does have performance constraints.
    /// </remarks>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <returns>Whether or not the specified type of component is supported.</returns>
    public bool SupportsComponent<T>() where T : struct, IOVRAnchorComponent<T>
        => GetSpaceComponentStatusInternal(Handle, default(T).Type, out _, out _).IsSuccess();

    /// <summary>
    /// Get all the supported components of an anchor.
    /// </summary>
    /// <param name="components">The list to populate with the supported components. The list is cleared first.</param>
    /// <returns>`True` if the supported components could be retrieved, otherwise `False`.</returns>
    public bool GetSupportedComponents(List<SpaceComponentType> components)
    {
        components.Clear();

        unsafe
        {
            if (!EnumerateSpaceSupportedComponents(Handle, 0, out var count, null).IsSuccess())
                return false;

            var buffer = stackalloc SpaceComponentType[(int)count];
            if (!EnumerateSpaceSupportedComponents(Handle, count, out count, buffer).IsSuccess())
                return false;

            for (uint i = 0; i < count; i++)
            {
                components.Add(buffer[i]);
            }

            return true;
        }
    }

    public bool Equals(OVRAnchor other) => Handle.Equals(other.Handle) && Uuid.Equals(other.Uuid);
    public override bool Equals(object obj) => obj is OVRAnchor other && Equals(other);
    public static bool operator ==(OVRAnchor lhs, OVRAnchor rhs) => lhs.Equals(rhs);
    public static bool operator !=(OVRAnchor lhs, OVRAnchor rhs) => !lhs.Equals(rhs);
    public override int GetHashCode() => unchecked(Handle.GetHashCode() * 486187739 + Uuid.GetHashCode());
    public override string ToString() => Uuid.ToString();

    /// <summary>
    /// Disposes of an anchor.
    /// </summary>
    /// <remarks>
    /// Calling this method will destroy the anchor so that it won't be managed by internal systems until
    /// the next time it is fetched again.
    /// </remarks>
    public void Dispose() => OVRPlugin.DestroySpace(Handle);

    [RuntimeInitializeOnLoadMethod]
    internal static void Init()
    {
        _deferredTasks.Clear();
        Telemetry.OnInit();
    }
}
