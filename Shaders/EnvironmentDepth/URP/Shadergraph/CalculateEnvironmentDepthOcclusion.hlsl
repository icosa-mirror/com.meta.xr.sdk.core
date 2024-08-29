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

//#pragma multi_compile would normally need to be set in the shader file, but this hlsl is needed in shadergraph
//so we do not have that option. Disabling the warning.
#pragma warning (disable : 3568)
#pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

#ifndef SHADERGRAPH_PREVIEW
#include "../EnvironmentOcclusionURP.hlsl"
#endif

void CalculateEnvironmentDepthOcclusion_float(float3 posWorld, float environmentDepthBias, out float occlusionValue)
{
#ifndef SHADERGRAPH_PREVIEW
    occlusionValue = META_DEPTH_GET_OCCLUSION_VALUE_WORLDPOS(posWorld, environmentDepthBias);
#else
		occlusionValue = 1.0;
#endif
}

void CalculateEnvironmentDepthOcclusion_half(float3 posWorld, float environmentDepthBias, out half occlusionValue)
{
#ifndef SHADERGRAPH_PREVIEW
    occlusionValue = META_DEPTH_GET_OCCLUSION_VALUE_WORLDPOS(posWorld, environmentDepthBias);
#else
		occlusionValue = 1.0;
#endif
}

