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
				float3   rayNormal  : TEXCOORD0;
				float4   rayOrigin  : TEXCOORD1;
				float2   uvDepth    : TEXCOORD2;
				float4x4 invMVP     : TEXCOORD3;
				float4   vertex     : SV_POSITION;
			};

			/** The Camera depth texture*/
			sampler2D _CameraDepthTexture;

			/** The Transfer function texture*/
            sampler2D _TFTexture;

			/** The volume data*/
            sampler3D _TextureData;

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
				o.invMVP      = inverse(UNITY_MATRIX_MVP);
				float4x4 invP = inverse(UNITY_MATRIX_P);
				o.vertex      = v.vertex;

				//OpenGL
				if(UNITY_NEAR_CLIP_VALUE == -1.0)
					o.vertex.z = -1.0;
				else
					o.vertex.z = 0.0;

				float4 n;
				float4 x1 = mul(invP, float4(o.vertex.xy,  1.0, 1.0));
				float4 x2 = mul(invP, float4(o.vertex.xyz, 1.0));
				n = x1/x1.w - x2/x2.w;

				n = mul(n, UNITY_MATRIX_MV);

				//o.rayOrigin = mul(x2, UNITY_MATRIX_IT_MV);
				o.rayOrigin = mul(o.invMVP, o.vertex);
				o.rayOrigin = o.rayOrigin/o.rayOrigin.w;
				
				o.rayNormal = normalize(n.xyz);
				o.uvDepth   = o.vertex.xy*0.5 + 0.5;
				if (_ProjectionParams.x < 0)
					o.uvDepth.y = 1.0 - o.uvDepth.y;
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
				if (nDir == 0.0)
					return false;

				t = dot(planeNormal, planePosition - rayOrigin) / nDir;
				return true;
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
					float3(-0.5, -0.5, -0.5), t[0]);
				//Right
				tValidity[1] = computeRayPlaneIntersection(rayOrigin, rayNormal, float3(1, 0, 0),
					float3(+0.5, -0.5, -0.5), t[1]);
				//Bottom
				tValidity[2] = computeRayPlaneIntersection(rayOrigin, rayNormal, float3(0, -1, 0),
					float3(-0.5, -0.5, -0.5), t[2]);
				//tOP
				tValidity[3] = computeRayPlaneIntersection(rayOrigin, rayNormal, float3(0, 1, 0),
					float3(-0.5, +0.5, -0.5), t[3]);
				//Front
				tValidity[4] = computeRayPlaneIntersection(rayOrigin, rayNormal, float3(0, 0, -1),
					float3(-0.5, -0.5, -0.5), t[4]);
				//Back
				tValidity[5] = computeRayPlaneIntersection(rayOrigin, rayNormal, float3(0, 0, 1),
					float3(-0.5, -0.5, +0.5), t[5]);

				//Test the limits
				for (int i = 0; i < 2; i++)
				{
					//Left / Right
					if (tValidity[i])
					{
						if(t[i] < 0.0)
							tValidity[i] = false;
						else
						{
							float3 p = t[i] * rayNormal + rayOrigin;
							if (p.y < -0.5 - 0.001 || p.y > +0.5 + 0.001 ||
								p.z < -0.5 - 0.001 || p.z > +0.5 + 0.001)
								tValidity[i] = false;
						}
					}

					//Top / Bottom
					if (tValidity[i + 2])
					{
						if(t[i + 2] < 0.0)
							tValidity[i + 2] = false;
						else
						{
							float3 p = t[i + 2] * rayNormal + rayOrigin;
							if (p.x < -0.5 - 0.001 || p.x > +0.5 + 0.001 ||
								p.z < -0.5 - 0.001 || p.z > +0.5 + 0.001)
								tValidity[i + 2] = false;
						}
					}

					//Front / Back
					if(tValidity[i + 4])
					{
						if(t[i + 4] < 0.0)
							tValidity[i + 4] = false;
						else
						{
							float3 p = t[i + 4] * rayNormal + rayOrigin;
							if (p.x < -0.5-0.001 || p.x > +0.5+0.001 ||
								p.y < -0.5-0.001 || p.y > +0.5+0.001)
								tValidity[i + 4] = false;
						}
					}
				}
			}

			float4 frag(v2f input) : SV_Target
			{
				float4 fragColor = float4(0, 0, 0, 0);

				//Compute ray - cube intersections
				float t[6];
				bool  tValidity[6];
				computeRayCubeIntersection(input.rayOrigin.xyz, input.rayNormal, t, tValidity);

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
						//break; //Maximum two points
					}
				}

				//If start == end -> only one point. Go from our position to the end!
				if(minT == maxT)
					minT = 0;

				//compute step and maximum number of steps
				//float rayStep = 1.0 / (max(max(uDimension.x, uDimension.y), uDimension.z)*4.0);
				float rayStep = 1.0 / (256.0*2.0);
				#ifdef UNITY_REVERSED_Z
					rayStep *= -1;
					float temp = minT;
					minT = maxT;
					maxT = temp;
				#endif

				float3 rayPos = input.rayOrigin.xyz + minT * input.rayNormal;


				//Determine max displacement (the displacement the ray can perform) regarding the depth
				float depthPos = Linear01Depth(tex2D(_CameraDepthTexture, input.uvDepth));
				#ifdef UNITY_REVERSED_Z
				depthPos = 1.0f - depthPos;
				#endif
				if (UNITY_NEAR_CLIP_VALUE == 1.0) //Transform [0, 1] to [-1, 1]
					depthPos = depthPos * 2.0 - 1.0;

				float4 endRayOrigin = mul(input.invMVP, float4(input.uvDepth*2.0-1.0, depthPos, 1.0));
				endRayOrigin /= endRayOrigin.w;
				
				float maxDepthDisplacement = -dot(input.rayNormal, rayPos - endRayOrigin.xyz)-0.0001; //-0.0001 == epsilon
				if(maxDepthDisplacement < 0)
					discard;

				rayPos += float3(0.5, 0.5, 0.5); //To read the 3D texture between 0.0 and 1.0

				//Ray marching algorithm
				#ifdef UNITY_REVERSED_Z //minT and maxT are reversed
				for (; minT > maxT && maxDepthDisplacement > 0; 
					minT += rayStep, maxDepthDisplacement -= rayStep,
					rayPos += input.rayNormal * rayStep)
				#else
				for(; minT < maxT && maxDepthDisplacement > 0*/; 
					minT += rayStep, maxDepthDisplacement -= rayStep,
					rayPos += input.rayNormal * rayStep)
				#endif
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
