//  Copyright(c) 2016, Michal Skalsky
//  All rights reserved.
//
//  Redistribution and use in source and binary forms, with or without modification,
//  are permitted provided that the following conditions are met:
//
//  1. Redistributions of source code must retain the above copyright notice,
//     this list of conditions and the following disclaimer.
//
//  2. Redistributions in binary form must reproduce the above copyright notice,
//     this list of conditions and the following disclaimer in the documentation
//     and/or other materials provided with the distribution.
//
//  3. Neither the name of the copyright holder nor the names of its contributors
//     may be used to endorse or promote products derived from this software without
//     specific prior written permission.
//
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
//  EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
//  OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.IN NO EVENT
//  SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
//  SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT
//  OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
//  HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR
//  TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
//  EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.



Shader "Hidden/BilateralBlur"
{
	Properties
	{
        _MainTex("Texture", any) = "" {}
	}
		SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		CGINCLUDE

        //--------------------------------------------------------------------------------------------
        // Downsample, bilateral blur and upsample config
        //--------------------------------------------------------------------------------------------        
        // method used to downsample depth buffer: 0 = min; 1 = max; 2 = min/max in chessboard pattern
        #define DOWNSAMPLE_DEPTH_MODE 2
        #define UPSAMPLE_DEPTH_THRESHOLD 1.5f
        #define BLUR_DEPTH_FACTOR 0.5
        #define GAUSS_BLUR_DEVIATION 1.5        
        #define FULL_RES_BLUR_KERNEL_SIZE 7
        #define HALF_RES_BLUR_KERNEL_SIZE 5
        #define QUARTER_RES_BLUR_KERNEL_SIZE 6
        //--------------------------------------------------------------------------------------------

		#define PI 3.1415927f

#include "UnityCG.cginc"	

        float4 _FullResTexelSize;
		float4 _HalfResTexelSize;
		float4 _QuarterResTexelSize;
		sampler2D _CameraDepthTexture;
		sampler2D _HalfResDepthBuffer;
		sampler2D _QuarterResDepthBuffer;
        sampler2D _HalfResColor;
		sampler2D _QuarterResColor;
		sampler2D _MainTex;
		float4 _RenderTargetSize;

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
		};

		struct v2fDownsample
		{
			//float2 uv : TEXCOORD0;
			float2 uv00 : TEXCOORD0;
			float2 uv01 : TEXCOORD1;
			float2 uv10 : TEXCOORD2;
			float2 uv11 : TEXCOORD3;
			float4 vertex : SV_POSITION;
		};

		struct v2fUpsample
		{
			float2 uv : TEXCOORD0;
			float2 uv00 : TEXCOORD1;
			float2 uv01 : TEXCOORD2;
			float2 uv10 : TEXCOORD3;
			float2 uv11 : TEXCOORD4;
			float4 vertex : SV_POSITION;
		};

		v2f vert(appdata v)
		{
			v2f o;
			o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
			o.uv = v.uv;
			return o;
		}

		//-----------------------------------------------------------------------------------------
		// vertDownsampleDepth
		//-----------------------------------------------------------------------------------------
		v2fDownsample vertDownsampleDepth(appdata v, float2 texelSize)
		{
			v2fDownsample o;
			o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
			//o.uv = v.uv;

            o.uv00 = v.uv - 0.5 * texelSize.xy;
            o.uv10 = o.uv00 + float2(texelSize.x, 0);
            o.uv01 = o.uv00 + float2(0, texelSize.y);
            o.uv11 = o.uv00 + texelSize.xy;
			return o;
		}

		//-----------------------------------------------------------------------------------------
		// vertUpsample
		//-----------------------------------------------------------------------------------------
        v2fUpsample vertUpsample(appdata v, float2 texelSize)
        {
            v2fUpsample o;
            o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
            o.uv = v.uv;

            o.uv00 = v.uv - 0.5 * texelSize.xy;
            o.uv10 = o.uv00 + float2(texelSize.x, 0);
            o.uv01 = o.uv00 + float2(0, texelSize.y);
            o.uv11 = o.uv00 + texelSize.xy;
            return o;
        }

		//-----------------------------------------------------------------------------------------
		// BilateralUpsample
		//-----------------------------------------------------------------------------------------
		float4 BilateralUpsample(v2fUpsample input, sampler2D hiDepth, sampler2D loDepth, sampler2D loColor)
		{
            const float threshold = UPSAMPLE_DEPTH_THRESHOLD;
			float4 highResDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(hiDepth, input.uv)).xxxx;
			float4 lowResDepth;

			lowResDepth[0] = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(loDepth, input.uv00));
			lowResDepth[1] = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(loDepth, input.uv10));
			lowResDepth[2] = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(loDepth, input.uv01));
			lowResDepth[3] = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(loDepth, input.uv11));

			float4 depthDiff = abs(lowResDepth - highResDepth);

			float accumDiff = dot(depthDiff, float4(1, 1, 1, 1));

			[branch]
			if (accumDiff < threshold) // small error, not an edge -> use bilinear filter
			{
				// should be bilinear sampler, dont know how to use two different samplers for one texture in Unity
				//return float4(1, 0, 0, 1);
				return tex2D(loColor, input.uv);
			}


			// find nearest sample
			float minDepthDiff = depthDiff[0];
			float2 nearestUv = input.uv00;

			if (depthDiff[1] < minDepthDiff)
			{
				nearestUv = input.uv10;
				minDepthDiff = depthDiff[1];
			}

			if (depthDiff[2] < minDepthDiff)
			{
				nearestUv = input.uv01;
				minDepthDiff = depthDiff[2];
			}

			if (depthDiff[3] < minDepthDiff)
			{
				nearestUv = input.uv11;
				minDepthDiff = depthDiff[3];
			}

			return tex2D(loColor, nearestUv);
		}

		//-----------------------------------------------------------------------------------------
		// DownsampleDepth
		//-----------------------------------------------------------------------------------------
		float DownsampleDepth(v2fDownsample input, sampler2D depthTexture)
		{
#if DOWNSAMPLE_DEPTH_MODE == 0 // min  depth
			float depth = tex2D(depthTexture, input.uv00).x;
			depth = min(depth, tex2D(depthTexture, input.uv01)).x;
			depth = min(depth, tex2D(depthTexture, input.uv10)).x;
			depth = min(depth, tex2D(depthTexture, input.uv11)).x;
			return depth;
#elif DOWNSAMPLE_DEPTH_MODE == 1 // max  depth
            float depth = tex2D(depthTexture, input.uv00).x;
            depth = max(depth, tex2D(depthTexture, input.uv01)).x;
            depth = max(depth, tex2D(depthTexture, input.uv10)).x;
            depth = max(depth, tex2D(depthTexture, input.uv11)).x;
            return depth;
#elif DOWNSAMPLE_DEPTH_MODE == 2 // min/max depth in chessboard pattern
			float4 depth;
			depth.x = tex2D(depthTexture, input.uv00).x;
			depth.y = tex2D(depthTexture, input.uv01).x;
			depth.z = tex2D(depthTexture, input.uv10).x;
			depth.w = tex2D(depthTexture, input.uv11).x;

			float minDepth = min(min(depth.x, depth.y), min(depth.z, depth.w));
			float maxDepth = max(max(depth.x, depth.y), max(depth.z, depth.w));

			// chessboard pattern
			int2 position = input.vertex.xy % 2;
			int index = position.x + position.y;
			return index == 1 ? minDepth : maxDepth;
#endif
		}

		//-----------------------------------------------------------------------------------------
		// DownsampleDepthMax
		//-----------------------------------------------------------------------------------------
		float DownsampleDepthMax(v2fDownsample input, sampler2D depthTexture)
		{
			float depth = tex2D(depthTexture, input.uv00).x;
			depth = max(depth, tex2D(depthTexture, input.uv01)).x;
			depth = max(depth, tex2D(depthTexture, input.uv10)).x;
			depth = max(depth, tex2D(depthTexture, input.uv11)).x;
			return depth;
		}

		//-----------------------------------------------------------------------------------------
		// GaussianWeight
		//-----------------------------------------------------------------------------------------
		float GaussianWeight(float offset, float deviation)
		{
			float weight = 1.0f / sqrt(2.0f * PI * deviation * deviation);
			weight *= exp(-(offset * offset) / (2.0f * deviation * deviation));
			return weight;
		}

		//-----------------------------------------------------------------------------------------
		// BilateralBlur
		//-----------------------------------------------------------------------------------------
		float4 BilateralBlur(v2f input, float2 direction, sampler2D depth, const float kernelRadius, float2 pixelSize)
		{
			//const float deviation = kernelRadius / 2.5;
			const float deviation = kernelRadius / GAUSS_BLUR_DEVIATION; // make it really strong

			float2 uv = input.uv;
			float4 centerColor = tex2D(_MainTex, uv);
			float3 color = centerColor.xyz;
			//return float4(color, 1);
			float centerDepth = (LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(depth, uv)));

			float weightSum = 0;

			// gaussian weight is computed from constants only -> will be computed in compile time
            float weight = GaussianWeight(0, deviation);
			color *= weight;
			weightSum += weight;
						
			[unroll] for (float i = -kernelRadius; i < 0; i += 1)
			{
				float2 uv = input.uv + (direction * i) * pixelSize;
				float3 sampleColor = tex2D(_MainTex, uv);
				float sampleDepth = (LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(depth, uv)));

				float depthDiff = abs(centerDepth - sampleDepth);
                float dFactor = depthDiff * BLUR_DEPTH_FACTOR;
				float w = exp(-(dFactor * dFactor));

				// gaussian weight is computed from constants only -> will be computed in compile time
				weight = GaussianWeight(i, deviation) *w;

				color += weight * sampleColor;
				weightSum += weight;
			}

			[unroll] for (i = 1; i <= kernelRadius; i += 1)
			{
				float2 uv = input.uv + (direction * i) * pixelSize;
				float3 sampleColor = tex2D(_MainTex, uv);
				float sampleDepth = (LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(depth, uv)));

				float depthDiff = abs(centerDepth - sampleDepth);
                float dFactor = depthDiff * BLUR_DEPTH_FACTOR;
				float w = exp(-(dFactor * dFactor));
				
				// gaussian weight is computed from constants only -> will be computed in compile time
				weight = GaussianWeight(i, deviation) *w;

				color += weight * sampleColor;
				weightSum += weight;
			}

			color /= weightSum;
			return float4(color, centerColor.w);
		}

		ENDCG

		// pass 0 - horizontal blur (hires)
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment horizontalFrag
#pragma target 4.0
			
			fixed4 horizontalFrag(v2f input) : SV_Target
			{
				return BilateralBlur(input, float2(1, 0), _CameraDepthTexture, FULL_RES_BLUR_KERNEL_SIZE, _FullResTexelSize.xy);
			}

			ENDCG
		}

		// pass 1 - vertical blur (hires)
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment verticalFrag
#pragma target 4.0
			
			fixed4 verticalFrag(v2f input) : SV_Target
			{
                return BilateralBlur(input, float2(0, 1), _CameraDepthTexture, FULL_RES_BLUR_KERNEL_SIZE, _FullResTexelSize.xy);
			}

			ENDCG
		}

		// pass 2 - horizontal blur (lores)
		Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment horizontalFrag
#pragma target 4.0

			fixed4 horizontalFrag(v2f input) : SV_Target
		{
            return BilateralBlur(input, float2(1, 0), _HalfResDepthBuffer, HALF_RES_BLUR_KERNEL_SIZE, _HalfResTexelSize.xy);
		}

			ENDCG
		}

		// pass 3 - vertical blur (lores)
		Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment verticalFrag
#pragma target 4.0

			fixed4 verticalFrag(v2f input) : SV_Target
		{
            return BilateralBlur(input, float2(0, 1), _HalfResDepthBuffer, HALF_RES_BLUR_KERNEL_SIZE, _HalfResTexelSize.xy);
		}

			ENDCG
		}

		// pass 4 - downsample depth to half
		Pass
		{
			CGPROGRAM
			#pragma vertex vertHalfDepth
			#pragma fragment frag
#pragma target 4.0

			v2fDownsample vertHalfDepth(appdata v)
			{
				return vertDownsampleDepth(v, _FullResTexelSize);
			}

			float frag(v2fDownsample input) : SV_Target
			{
				return DownsampleDepth(input, _CameraDepthTexture);
			}

			ENDCG
		}

		// pass 5 - bilateral upsample
		Pass
		{
			Blend One Zero

			CGPROGRAM
			#pragma vertex vertUpsampleToFull
			#pragma fragment frag		
#pragma target 4.0

			v2fUpsample vertUpsampleToFull(appdata v)
			{
				return vertUpsample(v, _HalfResTexelSize);
			}
			float4 frag(v2fUpsample input) : SV_Target
			{
				return BilateralUpsample(input, _CameraDepthTexture, _HalfResDepthBuffer, _HalfResColor);
			}

			ENDCG
		}

		// pass 6 - downsample depth to quarter
		Pass
		{
			CGPROGRAM
#pragma vertex vertQuarterDepth
#pragma fragment frag
#pragma target 4.0

			v2fDownsample vertQuarterDepth(appdata v)
			{
				return vertDownsampleDepth(v, _HalfResTexelSize);
			}

			float frag(v2fDownsample input) : SV_Target
			{
				return DownsampleDepth(input, _HalfResDepthBuffer);
			}

			ENDCG
		}

		// pass 7 - bilateral upsample to half
		Pass
		{
			Blend One Zero

			CGPROGRAM
#pragma vertex vertUpsampleToHalf
#pragma fragment frag		
#pragma target 4.0

			v2fUpsample vertUpsampleToHalf(appdata v)
			{
				return vertUpsample(v, _QuarterResTexelSize);
			}
			float4 frag(v2fUpsample input) : SV_Target
			{
				return BilateralUpsample(input, _HalfResDepthBuffer, _QuarterResDepthBuffer, _QuarterResColor);
			}

			ENDCG
		}

		// pass 8 - horizontal blur (quarter res)
		Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment horizontalFrag
#pragma target 4.0

			fixed4 horizontalFrag(v2f input) : SV_Target
			{
                return BilateralBlur(input, float2(1, 0), _QuarterResDepthBuffer, QUARTER_RES_BLUR_KERNEL_SIZE, _QuarterResTexelSize.xy);
			}

			ENDCG
		}

		// pass 9 - vertical blur (quarter res)
		Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment verticalFrag
#pragma target 4.0

			fixed4 verticalFrag(v2f input) : SV_Target
			{
                return BilateralBlur(input, float2(0, 1), _QuarterResDepthBuffer, QUARTER_RES_BLUR_KERNEL_SIZE, _QuarterResTexelSize.xy);
			}

			ENDCG
		}
	}
}
