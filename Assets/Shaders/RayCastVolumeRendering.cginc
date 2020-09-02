#include "UnityCG.cginc"

struct appdata
{
	float4 vertex : POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
	float4x4 invMVP       : TEXCOORD3;
	float2   uvDepth      : TEXCOORD2;
	float3   begRayOrigin : TEXCOORD1;  
	float4   endRayOrigin : TEXCOORD0;
	float4   vertex       : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

/** The Camera depth texture*/
UNITY_DECLARE_SCREENSPACE_TEXTURE(_LastCameraDepthTexture);

/** The volume data*/
sampler3D _TextureData;

/** The volume dimension along all axis*/
fixed3 _Dimensions = fixed3(128,128,128);

v2f vert(appdata v)
{
	v2f o;
	UNITY_SETUP_INSTANCE_ID(v);
	//UNITY_INITIALIZE_OUTPUT(v2f, o);
	UNITY_TRANSFER_INSTANCE_ID(v, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	
	o.vertex   = v.vertex;
	o.vertex.z = UNITY_NEAR_CLIP_VALUE;

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
		o.endRayOrigin = mul(o.invMVP, float4(v.vertex.x, v.vertex.y* _ProjectionParams.x, 1.0, 1.0));
	}

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
	//Top
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
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
	//UNITY_SETUP_INSTANCE_ID(input);

	fixed4  fragColor = fixed4(0, 0, 0, 0);

	//Optimization when in perspective mode
	const fixed3 rayNormal = normalize(input.endRayOrigin.xyz / input.endRayOrigin.w - input.begRayOrigin.xyz);

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
		
	fixed3 rayPos        = input.begRayOrigin.xyz + minT * rayNormal;
	const fixed rayStep  = 1.0/length(rayNormal*_Dimensions);
	const fixed3 rayStepNormal = rayStep*rayNormal;
	
	//Determine max displacement (the displacement the ray can perform) regarding the depth. Done here for optimization process
	fixed depthPos = UNITY_SAMPLE_DEPTH(UNITY_SAMPLE_SCREENSPACE_TEXTURE(_LastCameraDepthTexture, UnityStereoTransformScreenSpaceTex(input.uvDepth)));
	
	//Reverse Z
#if defined(UNITY_REVERSED_Z)
	depthPos = 1.0 - depthPos;
#endif
	
	//Between -1 and 1.0
	depthPos = 2.0*depthPos-1.0;
	const fixed2 uvDepth = 2.0*input.uvDepth -1.0;

	fixed4 endRayDepth = mul(input.invMVP, fixed4(uvDepth, depthPos, 1.0));
	endRayDepth /= endRayDepth.w;
	
	fixed maxDepthDisplacement = dot(rayNormal, endRayDepth.xyz - rayPos);
	rayPos += 0.5;
	
	//Ray marching algorithm
	const int count = int(min(maxDepthDisplacement, maxT - minT) / rayStep);
	
	for(int j = 0; j < count; j+=1)
	{
		fixed4 texPos  = fixed4(clamp(j * rayStepNormal + rayPos.xyz, fixed3(0, 0, 0), fixed3(1, 1, 1)), 0.0);
		fixed4 tfColor = tex3Dlod(_TextureData, texPos);
		fragColor = fragColor + ((1.0 - fragColor.a) * tfColor.a) * fixed4(tfColor.xyz, 1.0);

		//If enough contribution
		if (fragColor.a > 0.975)
		{
			fragColor.a = 1;
			return fragColor;
		}
	}

	return fragColor;
}