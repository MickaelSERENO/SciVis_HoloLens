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

        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                fixed4 vertex : POSITION;
            };

            struct v2f
            {
				fixed3 rayNormal : TEXCOORD0;
				fixed4 rayOrigin : TEXCOORD1;
                fixed4 vertex    : SV_POSITION;
            };

            sampler2D _TFTexture;
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
				o.vertex = v.vertex;

				fixed3 n;

				//Orthographique
				if (unity_OrthoParams.w == 1.0)
				{
					n = mul(float3(0, 0, 1), (float3x3)UNITY_MATRIX_MVP);
					o.rayOrigin = mul(inverse(UNITY_MATRIX_MVP), fixed4(v.vertex.xy, -1.0, 1));
					o.rayOrigin = o.rayOrigin / o.rayOrigin.w;
				}

				//Projection
				else
				{
					fixed4 x = mul(inverse(UNITY_MATRIX_P), fixed4(v.vertex.xy, -1.0, 1));
					n = (float3)mul(x, UNITY_MATRIX_MV);
					//o.rayOrigin = mul(x, UNITY_MATRIX_IT_MV);
					o.rayOrigin = mul(inverse(UNITY_MATRIX_MVP), fixed4(v.vertex.xy, 1.0, 1));
					o.rayOrigin = o.rayOrigin / o.rayOrigin.w;
				}
				o.rayNormal = normalize(n.xyz);
				return o;
			}

			/** \brief  Compute the intersection between a ray and a plane
			 * \param rayOrigin the ray origin
			 * \param planeNormal the plane normal
			 * \param planePosition the plane position
			 * \param t[out] the parameter t of the ray equation
			 * \return   true if intersection, false otherwise */
			bool computeRayPlaneIntersection(in fixed3 rayOrigin, in fixed3 rayNormal, in fixed3 planeNormal, in fixed3 planePosition, out fixed t)
			{
				fixed nDir = dot(planeNormal, rayNormal);
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
			void computeRayCubeIntersection(in fixed3 rayOrigin, in fixed3 rayNormal, out fixed t[6], out bool tValidity[6])
			{
				//Left
				tValidity[0] = computeRayPlaneIntersection(rayOrigin, rayNormal, fixed3(-1, 0, 0),
					fixed3(0.0, 0.0, 0.0), t[0]);
				//Right
				tValidity[1] = computeRayPlaneIntersection(rayOrigin, rayNormal, fixed3(1, 0, 0),
					fixed3(1.0, 0.0, 0.0), t[1]);
				//Top
				tValidity[2] = computeRayPlaneIntersection(rayOrigin, rayNormal, fixed3(0, -1, 0),
					fixed3(0.0, 0.0, 0.0), t[2]);
				//Bottom
				tValidity[3] = computeRayPlaneIntersection(rayOrigin, rayNormal, fixed3(0, 1, 0),
					fixed3(0.0, 1.0, 0.0), t[3]);
				//Front
				tValidity[4] = computeRayPlaneIntersection(rayOrigin, rayNormal, fixed3(0, 0, -1),
					fixed3(0.0, 0.0, 0.0), t[4]);
				//Back
				tValidity[5] = computeRayPlaneIntersection(rayOrigin, rayNormal, fixed3(0, 0, 1),
					fixed3(0.0, 0.0, 1.0), t[5]);

				//Test the limits
				for (int i = 0; i < 2; i++)
				{
					//Left / Right
					if (tValidity[i])
					{
						fixed3 p = t[i] * rayNormal + rayOrigin;
						if (p.y <= 0.0 || p.y >= 1.0 ||
							p.z <= 0.0 || p.z >= 1.0)
							tValidity[i] = false;
					}

					//Top / Bottom
					if (tValidity[i + 2])
					{
						fixed3 p = t[i + 2] * rayNormal + rayOrigin;
						if (p.x <= 0.0 || p.x >= 1.0 ||
							p.z <= 0.0 || p.z >= 1.0)
							tValidity[i + 2] = false;
					}

					//Front / Back
					if (tValidity[i + 4])
					{
						fixed3 p = t[i + 4] * rayNormal + rayOrigin;
						if (p.x <= 0.0 || p.x >= 1.0 ||
							p.y <= 0.0 || p.y >= 1.0)
							tValidity[i + 4] = false;
					}
				}
			}

			fixed4 frag(v2f input) : SV_Target
			{
				fixed4 fragColor = fixed4(0, 0, 0, 0);

				//Compute ray - cube intersections
				fixed t[6];
				bool  tValidity[6];
				computeRayCubeIntersection(input.rayOrigin.xyz, input.rayNormal, t, tValidity);

				//Determine if the ray touched the cube or not
				int startValidity = 0;
				for (; !tValidity[startValidity] && startValidity < 6; startValidity++);

				if (startValidity >=5)
					discard;
				//If yes, look at the starting and end points
				fixed minT = t[startValidity];
				fixed maxT = minT;

				for (int i = startValidity + 1; i < 6; i++)
				{
					if (tValidity[i])
					{
						minT = min(minT, t[i]);
						maxT = max(maxT, t[i]);
					}
				}

				//Handle ray not in at the beginning
				if (minT < 0)
					minT = 0.0;
				if (maxT < 0)
					discard;
				//compute step and maximum number of steps
				//fixed rayStep = 1.0 / (max(max(uDimension.x, uDimension.y), uDimension.z)*4.0);
				fixed rayStep = 1.0 / 128.0f;
				fixed3 rayPos = input.rayOrigin.xyz + minT * input.rayNormal;

				//Ray marching algorithm
				for (; minT < maxT; minT += rayStep, rayPos += input.rayNormal * rayStep)
				{
					fixed2 tfCoord = tex3Dlod(_TextureData, fixed4(rayPos, 0.0)).rg;
					fixed4 tfColor = tex2Dlod(_TFTexture, fixed4(tfCoord, 0.0, 0.0));
					fragColor = (1.0 - fragColor.a)*tfColor + fragColor;
				}

				//At t=maxT
				fixed2 tfCoord = tex3D(_TextureData, input.rayOrigin.xyz + maxT * input.rayNormal).rg;
				fixed4 tfColor = tex2D(_TFTexture, tfCoord);
				
				return (1.0 - fragColor.a)*tfColor + fragColor;
			}
			ENDCG
        }
    }
}
