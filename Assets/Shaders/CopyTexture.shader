Shader "Sereno/CopyScreenSpaceTexture"
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
				fixed4 vertex : POSITION;
				fixed2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				fixed4 vertex : SV_POSITION;
				fixed2 uv : TEXCOORD0;
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

				o.vertex = fixed4(v.vertex.x, v.vertex.y * _ProjectionParams.x, v.vertex.zw);
				o.vertex.z = UNITY_NEAR_CLIP_VALUE;

				o.uv = v.uv;

				return o;
			}

			fixed4  frag(v2f input) : COLOR
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				//UNITY_SETUP_INSTANCE_ID(input);
				return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, UnityStereoTransformScreenSpaceTex(input.uv));
			}
			ENDCG
		}
	}
}