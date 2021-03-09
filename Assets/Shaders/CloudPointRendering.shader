Shader "Custom/CloudPointRendering"
{
    Properties
    {
        _PointSize("Point Size", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Lighting Off

        Pass
        {
            CGPROGRAM
            
            #include "UnityCG.cginc"

            #pragma vertex   vert
            #pragma fragment frag
            #pragma geometry geom

            sampler2D _MainTex;

            struct appdata
            {
                float3 vertex : POSITION;
                float4 color  : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2g
            {
                float4 vertex   : SV_POSITION;
                float4 color    : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct g2f
            {
                float4 vertex   : SV_POSITION;
                float4 localPos : TEXCOORD0;
                float3 normal   : TEXCOORD1;
                float4 color    : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _PointSize;

            v2g vert(appdata v)
            {
                v2g o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = mul(UNITY_MATRIX_MVP, float4(v.vertex.xyz, 1.0));// UNITY_SHADER_NO_UPGRADE
                o.color  = v.color;

                return o;
            }
                
            [maxvertexcount(24)]
            void geom(point v2g input[1], inout TriangleStream<g2f> tristream)
            {
                UNITY_SETUP_INSTANCE_ID(input[0]);

                //Discard hidden pixels
                if(input[0].color.a == 0.0)
                    return;

                const float f = _PointSize / 2; //half size
                float4 vc[8] = { float4(-f, -f, -f, 0.0f),  //0
                                 float4(-f, -f, +f, 0.0f),  //1
                                 float4(-f, +f, -f, 0.0f),  //2
                                 float4(-f, +f, +f, 0.0f),  //3
                                 float4(+f, -f, -f, 0.0f),  //4
                                 float4(+f, -f, +f, 0.0f),  //5
                                 float4(+f, +f, -f, 0.0f),  //6
                                 float4(+f, +f, +f, 0.0f) };//7

                const float3 n[6]  = { float3(-1.0,  0.0,  0.0), //left
                                       float3( 0.0,  0.0, -1.0), //front
                                       float3( 1.0,  0.0,  0.0), //right
                                       float3( 0.0,  0.0,  1.0), //back
                                       float3( 0.0,  1.0,  0.0), //top
                                       float3( 0.0, -1.0,  0.0) }; //bottom

                const int VERT_ORDER[24] = { 0,1,2,3, // left
                                             0,2,4,6, // front  
                                             4,6,5,7, // right
                                             7,3,5,1, // back
                                             2,3,6,7, // top
                                             0,4,1,5 }; // bottom

                for(int j = 0; j < 8; j++)
                    vc[j] = mul(UNITY_MATRIX_MVP, vc[j]);// UNITY_SHADER_NO_UPGRADE
                                        

                g2f v[24]; //for CUBE

                // Assign new vertices positions (24 new tile vertices, forming CUBE)
                for (int i = 0; i < 24; i++) 
                {
                    v[i] = (g2f)(0);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(v[i]);
                    v[i].color  = input[0].color;  
                    v[i].vertex = input[0].vertex + vc[VERT_ORDER[i]]; 
                }

                // Build the CUBE tile by submitting triangle strip vertices
                for (int k = 0; k < 6; k++)
                {
                    v[k * 4 + 0].normal = n[k];
                    tristream.Append(v[k*4 + 0]);

                    v[k * 4 + 1].normal = n[k];
                    tristream.Append(v[k*4 + 1]);

                    v[k * 4 + 2].normal = n[k];
                    tristream.Append(v[k * 4 + 2]);

                    v[k * 4 + 3].normal = n[k];
                    tristream.Append(v[k*4 + 3]);
                    
                    tristream.RestartStrip();
                }
            }

            fixed4 frag(g2f input) : COLOR
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return input.color;
            }

            ENDCG
        }
    }
}
