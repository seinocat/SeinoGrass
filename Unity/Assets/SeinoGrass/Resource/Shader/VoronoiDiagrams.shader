Shader "Algorithm/VoronoiDiagrams"
{
	Properties
	{

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

            #include  "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			#pragma vertex vert
            #pragma fragment frag

			CBUFFER_START(UnityPerMaterial)
			int _SeedCount;
			int _Width;
			int _Height;
			uniform float4 _SeedArray[512];
			uniform float4 _ColorArray[512];
			CBUFFER_END

			struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
			
			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
	            o.uv = v.uv;
	            return o;
			}

			float4 frag(v2f f) : SV_Target
			{
				float4 color = _ColorArray[0];
				float minLen = 1;
				for (int i = 0; i < _SeedCount; i++)
				{
					float len = length(f.uv - _SeedArray[i].xy);
					if (len < minLen)
					{
						minLen = len;
						color = _ColorArray[i];
					}
				}
				
				return color/255;
			}

			ENDHLSL
		}
	}
}