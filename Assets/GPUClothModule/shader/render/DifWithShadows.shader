Shader "Custom/DifWithShadows"
{
	Properties
	{
		[NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Pass
		{
			Tags {"LightMode"="ForwardBase"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #include "AutoLight.cginc"
			
            struct v2f
            {
                float2 uv : TEXCOORD0;
                SHADOW_COORDS(1) // put shadows data into TEXCOORD1
                float3 diff : COLOR0;
                float3 ambient : COLOR1;
                float4 pos : SV_POSITION;
            };

            RWStructuredBuffer<int> Trimap;
            RWStructuredBuffer<float4> Position;
            RWStructuredBuffer<float4> Velocity;
            RWStructuredBuffer<float2> TC;
            RWStructuredBuffer<float4> Nor;
            int vertn;
            int vertm;
			float4 _MainTex_ST;
			
            v2f vert(uint id : SV_VertexID)
            {
                v2f o;
                int triID = Trimap[id];
                float4 worldPos = Position[triID];
                o.pos = UnityObjectToClipPos(worldPos);
                o.uv = TC[Trimap[id]];

                //float3 worldNormal = UnityObjectToWorldNormal(Nor[triID].xyz);
                float nl = max(0, dot(Nor[triID].xyz, _WorldSpaceLightPos0.xyz));
                o.diff = nl * _LightColor0.rgb;
                o.ambient = ShadeSH9(float4(Nor[triID].xyz, 1));
                TRANSFER_SHADOW(o)

                return o;
            }
            sampler2D _MainTex;
			
            float4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                // compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
                float shadow = SHADOW_ATTENUATION(i);
                // darken light's illumination with shadow, keep ambient intact
                float3 lighting = abs((i.diff * shadow + i.ambient - float3(0.5f, 0.5f, 0.5f)));
                col.rgb *= abs(lighting);
                return col;
            }
            ENDCG
            CULL OFF
		}
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}
