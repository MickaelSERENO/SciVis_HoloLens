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
			#include "RayCastVolumeRendering.cginc"

			#pragma vertex vert
			#pragma fragment frag

			ENDCG
		}
	}
}