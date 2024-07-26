Shader "Unlit/GrassInstance"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			HLSLPROGRAM

			#pragma enable_d3d11_debug_symbols
            #pragma multi_compile_instancing
			
			#pragma vertex vert
			#pragma fragment frag
			
			#include  "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)

			float4 _Color;
			StructuredBuffer<float4x4> _TRSBuffer;
			
			CBUFFER_END

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};
			
			v2f vert (appdata v, uint instanceID : SV_InstanceID)
			{
				v2f o;
				float4 worldPos = mul(_TRSBuffer[instanceID], v.vertex);
				o.vertex = TransformWorldToHClip(worldPos.xyz);
				// o.vertex = TransformObjectToHClip(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				return 1;
			}
			
			ENDHLSL
		}
	}
}