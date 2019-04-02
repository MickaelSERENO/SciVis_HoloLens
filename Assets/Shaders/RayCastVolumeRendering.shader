 Shader "Sereno/RayCastVolumeRendering"
{
    Properties
    {
		_TFTexture  ("TFTexture", 2D)   = "defaulttexture" {}
		_TextureData("TextureData", 3D) = "defaulttexture" {}
    }

    SubShader
    {
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		
		LOD 100
		Lighting Off
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

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
				float4   vertex       : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			/** The Camera depth texture*/
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_CameraDepthTexture);

			/** The Transfer function texture*/
            sampler2D _TFTexture;

			/** The volume data*/
            sampler3D _TextureData;

			/** The maximum sampling dimension along all axis*/
			float _MaxDimension;

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

				if(unity_OrthoParams.w == 0.0)
				{
					float4 begRayOrigin = mul(float4(0, 0, 0, 1.0), UNITY_MATRIX_IT_MV);
					o.begRayOrigin  = begRayOrigin.xyz / begRayOrigin.w;
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
				if (nDir == 0)
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

			float4 frag(v2f input) : COLOR
			{
				UNITY_SETUP_INSTANCE_ID(input);
				float4 fragColor = float4(0, 0, 0, 0);

				//Determine ray origin and normal
				float3 rayOrigin;

				//Define start and end point 
				float4 endRayOrigin = mul(input.invMVP, float4(input.vertexBis.x, input.vertexBis.y*_ProjectionParams.x,  1.0, 1.0));
				endRayOrigin /= endRayOrigin.w;

				//Optimization when in perspective mode
				if(unity_OrthoParams.w == 0.0)
					rayOrigin = input.begRayOrigin.xyz;
				else
				{
					float4 begRayOrigin = mul(input.invMVP, float4(input.vertexBis.x, input.vertexBis.y*_ProjectionParams.x, -1.0, 1.0));
					rayOrigin = begRayOrigin.xyz / begRayOrigin.w;
				}
				float3 rayNormal = normalize(endRayOrigin.xyz - rayOrigin.xyz);

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
				float  rayStep = 1.0 / (1.5*_MaxDimension);
				float3 rayPos  = rayOrigin.xyz + minT * rayNormal;

				float2 uvDepth = input.uvDepth;
				//Determine max displacement (the displacement the ray can perform) regarding the depth
				float depthPos = UNITY_SAMPLE_DEPTH(UNITY_SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, uvDepth));
				
				//Reverse Z
#if defined(UNITY_REVERSED_Z)
				depthPos = 1.0 - depthPos;
#endif
				//Between -1 and 1.0
				depthPos = 2.0*depthPos-1.0;
				
				uvDepth.y *= _ProjectionParams.x;

				uvDepth = 2.0*uvDepth - 1.0;
				float4 endRayDepth = mul(input.invMVP, float4(uvDepth, depthPos, 1.0));
				endRayDepth /= endRayDepth.w;

				float maxDepthDisplacement = dot(rayNormal, endRayDepth.xyz - rayPos); 
				if(maxDepthDisplacement < 0) //Apply a small epsilon
					discard;
				rayPos += 0.5;

				//Ray marching algorithm
				for(float i = min(maxDepthDisplacement, maxT-minT); i >= 0 && maxDepthDisplacement > 0.0;
					i -= rayStep, rayPos += rayNormal * rayStep)
				{
					float2 tfCoord = tex3Dlod(_TextureData, float4(rayPos.x, rayPos.y, rayPos.z, 0.0)).rg;
					float4 tfColor = tex2Dlod(_TFTexture,   float4(tfCoord, 0.0, 0.2));

					//Contribution only if needed
					if (tfColor.a >= 0.005)
					{
						fragColor.xyz = fragColor.xyz + (1 - fragColor.a)*tfColor.a*tfColor.xyz;
						fragColor.a = fragColor.a + (1 - fragColor.a)*tfColor.a;

						//If enough contribution
						if (fragColor.a > 0.85)
						{
							fragColor.a = 1.0;
							return fragColor;
						}
					}
				}

				return fragColor;
			}
			ENDCG
        }
    }
}
