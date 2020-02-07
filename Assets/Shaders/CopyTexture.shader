Shader "Sereno/CopyTexture"
{
	Properties
	{
		_MainTex("Main Texture", any) = "" {}
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
				float2 uv     : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv     : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			/** The texture data*/
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
			uniform float4 _MainTex_ST; //Scale--translation of that texture

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				//UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
							   
				o.vertex   = float4(v.vertex.x, v.vertex.y*_ProjectionParams.x, v.vertex.zw);
				o.vertex.z = UNITY_NEAR_CLIP_VALUE;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
							   				
				return o;
			}

			fixed4  frag(v2f input) : COLOR
			{
				//UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				UNITY_SETUP_INSTANCE_ID(input);
				fixed4 color = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, UnityStereoTransformScreenSpaceTex(input.uv));
				return color;
			}
			ENDCG
		}
	}
}