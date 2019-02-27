// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Sereno/DefaultColor"
{
	Properties
	{
		 _PlanePosition ("Position of the Clipping Plane",  Vector) = (0, 0, 0)
		 _PlaneNormal   ("Normal of the Clipping Plane",    Vector) = (1, 0, 0)
		 _SpherePosition("Position of the Clipping Sphere", Vector) = (0, 0, 0)
		 _SphereRadius  ("Radius of the Clipping Sphere",   Float)  = 1.0
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 50

		Pass
		{
			Lighting Off
			CGPROGRAM
			#pragma vertex   vert
			#pragma fragment frag
			#pragma multi_compile TEXCOORD0_ON TEXCOORD1_ON TEXCOORD2_ON TEXCOORD3_ON TEXCOORD4_ON TEXCOORD5_ON TEXCOORD6_ON TEXCOORD7_ON TEXCOORD8_ON
			#pragma shader_feature SPHERE_ON
			#pragma shader_feature PLANE_ON

			#include "UnityCG.cginc"
				
			struct appdata
			{
				float4 vertex : POSITION;
				#if   defined(TEXCOORD0_ON)
                float4 color  : TEXCOORD0;
				#endif
				#if defined(TEXCOORD1_ON)
				float4 color  : TEXCOORD1;
				#endif
				#if defined(TEXCOORD2_ON)
				float4 color  : TEXCOORD2;
				#endif
				#if   defined(TEXCOORD3_ON)
                float4 color  : TEXCOORD3;
				#endif
				#if defined(TEXCOORD4_ON)
				float4 color  : TEXCOORD4;
				#endif
				#if defined(TEXCOORD5_ON)
				float4 color  : TEXCOORD5;
				#endif
				#if   defined(TEXCOORD6_ON)
                float4 color  : TEXCOORD6;
				#endif
				#if defined(TEXCOORD7_ON)
				float4 color  : TEXCOORD7;
				#endif
				#if defined(TEXCOORD8_ON)
				float4 color  : TEXCOORD8;
				#endif
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
                float4 color    : COLOR;
#ifdef PLANE_ON
				float dotPlane  : TEXCOORD2;
#endif
#ifdef SPHERE_ON
				float spherePos : TEXCOORD3;
#endif
			};

#ifdef PLANE_ON
			float3 _PlaneNormal;
			float3 _PlanePosition;
#endif

#ifdef SPHERE_ON
			float3 _SpherePosition;
			float  _SphereRadius;
#endif
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color  = v.color;

#if defined(PLANE_ON) || defined(SPHERE_ON) 
				float4 pMV = mul(unity_ObjectToWorld, v.vertex);
				float3 pos = pMV.xyz / pMV.w;
#endif

#ifdef PLANE_ON
				o.dotPlane = dot((pos - _PlanePosition), _PlaneNormal);
#endif
#ifdef SPHERE_ON
				o.spherePos = length(pos - _SpherePosition);
#endif	
				return o;
			}
			
			void frag (v2f i, out float4 color:COLOR)
			{
#ifdef PLANE_ON
				if(i.dotPlane < 0.0)
					discard;
#endif
#ifdef SPHERE_ON
				if(i.spherePos > _SphereRadius)
					discard;
#endif
				if (i.color.a < 0.999f)
					discard;

				color = i.color;
			}
			
			ENDCG
		}
	}
}
