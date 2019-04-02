// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced '_CameraToWorld' with 'unity_CameraToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Sereno/RayCastVolumeRendering"
{
    Properties
    {
		_TFTexture  ("TFTexture", 2D)   = "defaulttexture" {}
		_TextureData("TextureData", 3D) = "defaulttexture" {}
    }

    SubShader
    {
        Tags { "Queue"      = "Transparent" 
			   "RenderType" = "Transparent"}
		
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

        LOD 250

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
				float4x4 invMVP     : TEXCOORD4;
				float2 uvDepth      : TEXCOORD2;
				float4 vertexBis    : TEXCOORD3;
				float4 vertex       : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			/** The Camera depth texture*/
			sampler2D _CameraDepthTexture;

			/** The Transfer function texture*/
            sampler2D _TFTexture;

			/** The volume data*/
            sampler3D _TextureData;

			/** The maximum sampling dimension along all axis*/
			float _MaxDimension;


			float4x4 inverse(float4x4 m)
			{
				float n11 = m[0][0], n12 = m[1][0], n13 = m[2][0], n14 = m[3][0];
				float n21 = m[0][1], n22 = m[1][1], n23 = m[2][1], n24 = m[3][1];
				float n31 = m[0][2], n32 = m[1][2], n33 = m[2][2], n34 = m[3][2];
				float n41 = m[0][3], n42 = m[1][3], n43 = m[2][3], n44 = m[3][3];
				float t11 = n23 * n34 * n42 - n24 * n33 * n42 + n24 * n32 * n43 - n22 * n34 * n43 - n23 * n32 * n44 + n22 * n33 * n44;
				float t12 = n14 * n33 * n42 - n13 * n34 * n42 - n14 * n32 * n43 + n12 * n34 * n43 + n13 * n32 * n44 - n12 * n33 * n44;
				float t13 = n13 * n24 * n42 - n14 * n23 * n42 + n14 * n22 * n43 - n12 * n24 * n43 - n13 * n22 * n44 + n12 * n23 * n44;
				float t14 = n14 * n23 * n32 - n13 * n24 * n32 - n14 * n22 * n33 + n12 * n24 * n33 + n13 * n22 * n34 - n12 * n23 * n34;
				float det = n11 * t11 + n21 * t12 + n31 * t13 + n41 * t14;
				float idet = 1.0f / det;
				
				float4x4 ret;
				
				ret[0][0] = t11 * idet;
				ret[0][1] = (n24 * n33 * n41 - n23 * n34 * n41 - n24 * n31 * n43 + n21 * n34 * n43 + n23 * n31 * n44 - n21 * n33 * n44) * idet;
				ret[0][2] = (n22 * n34 * n41 - n24 * n32 * n41 + n24 * n31 * n42 - n21 * n34 * n42 - n22 * n31 * n44 + n21 * n32 * n44) * idet;
				ret[0][3] = (n23 * n32 * n41 - n22 * n33 * n41 - n23 * n31 * n42 + n21 * n33 * n42 + n22 * n31 * n43 - n21 * n32 * n43) * idet;
				ret[1][0] = t12 * idet;
				ret[1][1] = (n13 * n34 * n41 - n14 * n33 * n41 + n14 * n31 * n43 - n11 * n34 * n43 - n13 * n31 * n44 + n11 * n33 * n44) * idet;
				ret[1][2] = (n14 * n32 * n41 - n12 * n34 * n41 - n14 * n31 * n42 + n11 * n34 * n42 + n12 * n31 * n44 - n11 * n32 * n44) * idet;
				ret[1][3] = (n12 * n33 * n41 - n13 * n32 * n41 + n13 * n31 * n42 - n11 * n33 * n42 - n12 * n31 * n43 + n11 * n32 * n43) * idet;
				ret[2][0] = t13 * idet;
				ret[2][1] = (n14 * n23 * n41 - n13 * n24 * n41 - n14 * n21 * n43 + n11 * n24 * n43 + n13 * n21 * n44 - n11 * n23 * n44) * idet;
				ret[2][2] = (n12 * n24 * n41 - n14 * n22 * n41 + n14 * n21 * n42 - n11 * n24 * n42 - n12 * n21 * n44 + n11 * n22 * n44) * idet;
				ret[2][3] = (n13 * n22 * n41 - n12 * n23 * n41 - n13 * n21 * n42 + n11 * n23 * n42 + n12 * n21 * n43 - n11 * n22 * n43) * idet;
				ret[3][0] = t14 * idet;
				ret[3][1] = (n13 * n24 * n31 - n14 * n23 * n31 + n14 * n21 * n33 - n11 * n24 * n33 - n13 * n21 * n34 + n11 * n23 * n34) * idet;
				ret[3][2] = (n14 * n22 * n31 - n12 * n24 * n31 - n14 * n21 * n32 + n11 * n24 * n32 + n12 * n21 * n34 - n11 * n22 * n34) * idet;
				ret[3][3] = (n12 * n23 * n31 - n13 * n22 * n31 + n13 * n21 * n32 - n11 * n23 * n32 - n12 * n21 * n33 + n11 * n22 * n33) * idet;
				return ret;
			}

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex    = v.vertex;
#ifdef UNITY_REVERSED_Z
				o.vertex.z = 1.0;
#else
				o.vertex.z = UNITY_NEAR_CLIP_VALUE;
#endif
				o.vertexBis = v.vertex;
				o.invMVP    = mul(transpose(UNITY_MATRIX_IT_MV), unity_CameraInvProjection);
				o.uvDepth   = float2(o.vertex.x, o.vertex.y*_ProjectionParams.x)*0.5 + 0.5;

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
				if (nDir*nDir <= 1e-6)
					return false;

				t = dot(planeNormal, planePosition - rayOrigin) / nDir;
				return t >= 0.0;
			}

			/** \brief  Compute the ray-cube intersection
			 *
			 * \param rayOrigin the ray origin
			 * \param t[6] the t values (pos = rayOrigin +t*varyRayNormal)
			 * \param tValidity[6] the t validity (is t[i] a valid value?) */
			void computeRayCubeIntersection(in float3 rayOrigin, in float3 rayNormal, out float t[6], out bool tValidity[6])
			{
				//Left
				tValidity[0] = computeRayPlaneIntersection(rayOrigin, rayNormal, float3(-1, 0, 0),
					float3(-0.5, 0, 0), t[0]);
				//Right
				tValidity[1] = computeRayPlaneIntersection(rayOrigin, rayNormal, float3(1, 0, 0),
					float3(+0.5, 0, 0), t[1]);
				//Bottom
				tValidity[2] = computeRayPlaneIntersection(rayOrigin, rayNormal, float3(0, -1, 0),
					float3(0, -0.5, 0), t[2]);
				//tOP
				tValidity[3] = computeRayPlaneIntersection(rayOrigin, rayNormal, float3(0, 1, 0),
					float3(0, +0.5, 0), t[3]);
				//Front
				tValidity[4] = computeRayPlaneIntersection(rayOrigin, rayNormal, float3(0, 0, -1),
					float3(0, 0, -0.5), t[4]);
				//Back
				tValidity[5] = computeRayPlaneIntersection(rayOrigin, rayNormal, float3(0, 0, 1),
					float3(0, 0, +0.5), t[5]);

				//Test the limits
				for (int i = 0; i < 2; i++)
				{
					//Left / Right
					if (tValidity[i])
					{
						float3 p = t[i] * rayNormal + rayOrigin;
						if (p.y < -0.5 || p.y > +0.5 ||
							p.z < -0.5 || p.z > +0.5)
							tValidity[i] = false;
					}

					//Top / Bottom
					if (tValidity[i + 2])
					{
						float3 p = t[i + 2] * rayNormal + rayOrigin;
						if (p.x < -0.5 || p.x > +0.5 ||
							p.z < -0.5 || p.z > +0.5)
							tValidity[i + 2] = false;
					}

					//Front / Back
					if(tValidity[i + 4])
					{
						float3 p = t[i + 4] * rayNormal + rayOrigin;
						if (p.x < -0.5 || p.x > +0.5 ||
							p.y < -0.5 || p.y > +0.5)
							tValidity[i + 4] = false;
					}
				}
			}

			fixed4 frag(v2f input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				float4 endRayOrigin = mul(input.invMVP, float4(input.vertexBis.x, input.vertexBis.y*_ProjectionParams.x,  1.0,  1.0));
				float4 begRayOrigin = mul(input.invMVP, float4(input.vertexBis.x, input.vertexBis.y*_ProjectionParams.x, -1.0, 1.0));
				endRayOrigin /= endRayOrigin.w;
				begRayOrigin /= begRayOrigin.w;

				float3 rayOrigin = begRayOrigin.xyz;
				float3 rayNormal = normalize(endRayOrigin.xyz - begRayOrigin.xyz);

				//return fixed4(input.rayNormal, 1.0);
				float4 fragColor = float4(0, 0, 0, 0);

				//Compute ray - cube intersections
				float t[6];
				bool  tValidity[6];
				computeRayCubeIntersection(rayOrigin.xyz, rayNormal, t, tValidity);

				//Determine if the ray touched the cube or not
				int startValidity = 0;
				for (; !tValidity[startValidity] && startValidity < 6; startValidity++);

				if (startValidity == 6)
					discard;

				//If yes, look at the starting and end points
				float minT = t[startValidity];
				float maxT = minT;

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

				//compute step and maximum number of steps
				float rayStep = 1.0 / (3.0*_MaxDimension);
				float3 rayPos = rayOrigin.xyz + minT * rayNormal;

				//Determine max displacement (the displacement the ray can perform) regarding the depth
				float2 uvDepth = TransformStereoScreenSpaceTex(input.uvDepth, 1.0);
				float depthPos = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, uvDepth));
				
				//Reverse Z
#if defined(UNITY_REVERSED_Z)
				depthPos = 1.0 - depthPos;
#endif
				//Between -1 and 1.0
				depthPos = 2.0*depthPos-1.0;

				uvDepth = input.uvDepth;
				uvDepth.y *= _ProjectionParams.x;

				uvDepth = 2.0*uvDepth - 1.0;
				float4 endRayDepth = mul(input.invMVP, float4(uvDepth, depthPos, 1.0));
				endRayDepth /= endRayDepth.w;

				float maxDepthDisplacement = dot(rayNormal, endRayDepth.xyz - rayPos); 
				if(maxDepthDisplacement < 0.05) //Apply a small epsilon
					discard;
				rayPos += 0.5;

				//Ray marching algorithm
				for (; minT < maxT && maxDepthDisplacement > 0.0;
					minT += rayStep, maxDepthDisplacement -= rayStep,
					rayPos += rayNormal * rayStep)
				{
					float2 tfCoord = tex3Dlod(_TextureData, float4(rayPos.x, rayPos.y, rayPos.z, 0.0)).rg;
					float4 tfColor = tex2Dlod(_TFTexture,   float4(tfCoord, 0.0, 0.0));
					fragColor.xyz = fragColor.xyz + (1 - fragColor.a)*tfColor.a*tfColor.xyz;
					fragColor.a = fragColor.a + tfColor.a*(1 - fragColor.a);

					//If enough contribution
					if(fragColor.a > 0.95)
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
