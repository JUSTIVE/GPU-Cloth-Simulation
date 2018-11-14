// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/NormalShader" {
	Properties
	{
	}
	SubShader{
		Pass{
			LOD 100
			CGPROGRAM
			#pragma exclude_renderers d3d11
			#pragma multi_compile_fwdbase
			//#pragma exclude_renderers d3d11_9x d3d11 xbox360
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
			struct v2f {
				float4 pos : SV_POSITION;
				float4 col : COLOR0;
				float2 uv : TEXCOORD0;
				int id : VERTID;
				float4 normal: NORMAL;
				float3 FragColor :COLOR1;
			};
		
			RWStructuredBuffer<int>Trimap;
			RWStructuredBuffer<float4>Position;
			RWStructuredBuffer<float4>Velocity;
			RWStructuredBuffer<float2>TC;
			RWStructuredBuffer<float4>Nor;

			//vertexsize;
			int vertn;
			int vertm;

			//for light
			float4 Light_P;
			float3 Light_I = float3(10.0f,0,0);
			float3 Kd = float3(255,0,0);
			float3 Ka = float3(2,0,0);
			float3 Ks = float3(2,0,0);
			float Shininess = 100.0f;

			v2f vert(uint id : SV_VertexID) {
				v2f o;
				float4 worldPos = Position[Trimap[id]];
				o.pos = UnityObjectToClipPos(worldPos);
				o.normal= normalize(Nor[Trimap[id]]);
				return o;
			}
			
			float4 frag(v2f i) : SV_Target{
                i.normal.x = abs(i.normal.x);
                i.normal.y = abs(i.normal.y);
                i.normal.z = abs(i.normal.z);
				return i.normal;
			}
			ENDCG
			Lighting On
			CULL OFF
		}
	}
	Fallback "Diffuse"
}
