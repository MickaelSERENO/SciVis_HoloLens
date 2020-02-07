Shader "Sereno/StereoAppend" 
{
	Properties
	{
		_MainTex("", 2D) = "white" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"

	UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
	
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

	ENDCG

	SubShader{
		Lighting Off
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		Fog {Mode Off}

		//Blit Left
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
					   
			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.vertex.x = o.vertex.x*0.5 - 0.5;
				o.uv = v.uv;
				return o;
			}

			fixed4 frag(v2f input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				fixed4 c = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(input.uv));
				return c;
			}
			ENDCG
		}

		//Blit Right
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex   = UnityObjectToClipPos(v.vertex);
				o.vertex.x = o.vertex.x*0.5 + 0.5;
				o.uv       = v.uv;
				return o;
			}

			fixed4 frag(v2f input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				fixed4 c = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(input.uv));
				return c;
			}
			ENDCG
		}

	}
}