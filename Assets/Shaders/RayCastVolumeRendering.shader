﻿ Shader "Sereno/RayCastVolumeRendering"
{
    Properties
    {
		_TextureData("TextureData", 3D) = "defaulttexture" {}
    }

    SubShader
    {
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		
		Lighting Off
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		Fog {Mode Off}

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
				float4x4 invMVP       : TEXCOORD4;
				float2   uvDepth      : TEXCOORD2;
				float4   vertexBis    : TEXCOORD3;
				float3   begRayOrigin : TEXCOORD1;   //The beginning of ray origin only if in perspective mode
				float4   endRayOrigin : TEXCOORD0;
				float4   vertex       : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			/** The Camera depth texture*/
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_CameraDepthTexture);

			/** The volume data*/
            sampler3D _TextureData;

			/** The maximum sampling dimension along all axis*/
			float _MaxDimension = 128;

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex    = v.vertex;
				o.vertex.z = UNITY_NEAR_CLIP_VALUE;

				o.vertexBis = v.vertex;
				o.invMVP    = mul(transpose(UNITY_MATRIX_IT_MV), unity_CameraInvProjection);
				o.uvDepth   = float2(o.vertex.x, o.vertex.y*_ProjectionParams.x)*0.5 + 0.5;

				//Perspective
				if(unity_OrthoParams.w == 0.0)
				{
					float4 begRayOrigin = mul(float4(0, 0, 0, 1.0), UNITY_MATRIX_IT_MV);
					o.begRayOrigin = begRayOrigin.xyz / begRayOrigin.w;
					o.endRayOrigin = mul(o.invMVP, float4(v.vertex.x, v.vertex.y*_ProjectionParams.x, 1.0, 1.0));
				}

				//Orthographic
				else
				{
					float4 begRayOrigin = mul(o.invMVP, float4(v.vertex.x, v.vertex.y*_ProjectionParams.x, -1.0, 1.0));
					o.begRayOrigin = begRayOrigin.xyz / begRayOrigin.w;
				}

				return o;
			}

			/** \brief  Compute the intersection between a ray and a plane
			 * \param rayOrigin the ray origin
			 * \param planeNormal the plane normal
			 * \param planePosition the plane position
			 * \param t[out] the parameter t of the ray equation
			 * \return   true if intersection, false otherwise */
			bool computeRayPlaneIntersection(in float3 rayOrigin, in float3 rayNormal, in float3 planeNormal, in float3 planePosition, out float t)
			{
				float nDir = dot(planeNormal, rayNormal);
				//if (nDir == 0)
				//	return false;

				t = dot(planeNormal, planePosition - rayOrigin) / nDir;
				return t >= 0.0;
			}

			/** \brief  Compute the ray-cube intersection
			 *
			 * \param rayOrigin the ray origin
			 * \param t[6] the t values (pos = rayOrigin +t*varyRayNormal)
			 * \param tValidity[6] the t validity (is t[i] a valid value?) */
			void computeRayCubeIntersection(in fixed3 rayOrigin, in fixed3 rayNormal, out fixed t[6], out bool tValidity[6])
			{
				//Left
				tValidity[0] = computeRayPlaneIntersection(rayOrigin, rayNormal, fixed3(-1, 0, 0),
					fixed3(-0.5, 0, 0), t[0]);
				//Right
				tValidity[1] = computeRayPlaneIntersection(rayOrigin, rayNormal, fixed3(1, 0, 0),
					fixed3(+0.5, 0, 0), t[1]);
				//Bottom
				tValidity[2] = computeRayPlaneIntersection(rayOrigin, rayNormal, fixed3(0, -1, 0),
					fixed3(0, -0.5, 0), t[2]);
				//tOP
				tValidity[3] = computeRayPlaneIntersection(rayOrigin, rayNormal, fixed3(0, 1, 0),
					fixed3(0, +0.5, 0), t[3]);
				//Front
				tValidity[4] = computeRayPlaneIntersection(rayOrigin, rayNormal, fixed3(0, 0, -1),
					fixed3(0, 0, -0.5), t[4]);
				//Back
				tValidity[5] = computeRayPlaneIntersection(rayOrigin, rayNormal, fixed3(0, 0, 1),
					fixed3(0, 0, +0.5), t[5]);

				//Test the limits
				for (int i = 0; i < 2; i++)
				{
					//Left / Right
					if (tValidity[i])
					{
						fixed3 p = t[i] * rayNormal + rayOrigin;
						if (p.y < -0.5 || p.y > +0.5 ||
							p.z < -0.5 || p.z > +0.5)
							tValidity[i] = false;
					}

					//Top / Bottom
					if (tValidity[i + 2])
					{
						fixed3 p = t[i + 2] * rayNormal + rayOrigin;
						if (p.x < -0.5 || p.x > +0.5 ||
							p.z < -0.5 || p.z > +0.5)
							tValidity[i + 2] = false;
					}

					//Front / Back
					if(tValidity[i + 4])
					{
						fixed3 p = t[i + 4] * rayNormal + rayOrigin;
						if (p.x < -0.5 || p.x > +0.5 ||
							p.y < -0.5 || p.y > +0.5)
							tValidity[i + 4] = false;
					}
				}
			}

			fixed4  frag(v2f input) : COLOR
			{
				UNITY_SETUP_INSTANCE_ID(input);
				fixed4  fragColor = fixed4(0, 0, 0, 0);

				//Optimization when in perspective mode
				fixed3 rayNormal = normalize(input.endRayOrigin.xyz / input.endRayOrigin.w - input.begRayOrigin.xyz);

				//Compute ray - cube intersections
				fixed t[6];
				bool  tValidity[6];
				computeRayCubeIntersection(input.begRayOrigin.xyz, rayNormal, t, tValidity);

				//Determine if the ray touched the cube or not
				int startValidity = 0;
				for (; !tValidity[startValidity] && startValidity < 6; startValidity++);

				if(startValidity == 6)
					return fragColor;

				//If yes, look at the starting and end points
				fixed minT = t[startValidity];
				fixed maxT = minT;

				for(int i = startValidity + 1; i < 6; i++)
				{
					if(tValidity[i])
					{
						minT = min(minT, t[i]);
						maxT = max(maxT, t[i]);
						break; //Maximum two points
					}
				}

				//If start == end -> only one point. Go from our position to the end!
				if(minT == maxT)
					minT = 0;
				
				fixed3 rayPos       = input.begRayOrigin.xyz + minT * rayNormal;
				const half rayStep  = 1.0 / (sqrt(3)*_MaxDimension);
				half3 rayStepNormal = rayStep*rayNormal;

				fixed2 uvDepth = input.uvDepth;
				//Determine max displacement (the displacement the ray can perform) regarding the depth
				fixed depthPos = UNITY_SAMPLE_DEPTH(UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, uvDepth));
				
				//Reverse Z
#if defined(UNITY_REVERSED_Z)
				depthPos = 1.0 - depthPos;
#endif
				//Between -1 and 1.0
				depthPos = 2.0*depthPos-1.0;
				uvDepth  = 2.0*uvDepth -1.0;

				fixed4 endRayDepth = mul(input.invMVP, fixed4(uvDepth, depthPos, 1.0));
				endRayDepth /= endRayDepth.w;

				half maxDepthDisplacement = dot(rayNormal, endRayDepth.xyz - rayPos);

				rayPos += 0.5;
				//Ray marching algorithm
				for(half j = min(maxDepthDisplacement, maxT-minT); j > 0; j -= rayStep, rayPos += rayStepNormal)
				{
					half4 tfColor = tex3Dlod(_TextureData, fixed4(rayPos.xyz, 0.0));

					//tfColor.a *= 1.0 ; //Apply the modification of raystep for stability
					half4 col = half4(tfColor.xyz, 1.0);
					fragColor = fragColor + (1 - fragColor.a)*tfColor.a*col;
					
					//If enough contribution
					if (fragColor.a > 0.90)
					{
						fragColor.a = 1.0;
						return fragColor;
					}
				}

				return fragColor;
			}
			ENDCG
        }
    }
}
