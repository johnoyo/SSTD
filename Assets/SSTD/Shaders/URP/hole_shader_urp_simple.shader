Shader "Custom/hole_shader_urp_simple"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_Gid("_Gid", Int) = 1
	}

	SubShader
	{
		Tags{"RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalRenderPipeline" "IgnoreProjector" = "True"}
		LOD 300

		ColorMask RGB
		Cull Front
		ZTest Always

		Stencil {
			Ref[_Gid]
			Comp NotEqual
			Pass Keep
		}

		Pass
		{
			Name "StandardLit"
			Tags{"LightMode" = "UniversalForward"}

			ColorMask RGB
			Cull Front
			ZTest Always

			Stencil {
				Ref[_Gid]
				Comp NotEqual
				Pass Keep
			}

			HLSLPROGRAM

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma glsl
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			#pragma multi_compile_instancing

			//#include "HLSLSupport.cginc"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal     : NORMAL;
				float4 tangent    : TANGENT;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2g
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 normal : POSITION1;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			struct g2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 bary : TEXCOORD1;
				float3 normal : POSITION1;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;

			v2g vert(appdata v)
			{
				v2g o;

				UNITY_SETUP_INSTANCE_ID(v);
				o = (v2g)0;
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
				VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(v.normal, v.tangent);

				o.vertex = vertexInput.positionCS;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.normal = vertexNormalInput.normalWS.xyz;

				return o;
			}

			[maxvertexcount(3)]
			void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream) {
				float3 param = float3(0.0, 0.0, 0.0);

				g2f o;

				o.vertex = IN[0].vertex;
				o.normal = IN[0].normal;
				o.uv = IN[0].uv;
				o.bary = float3(1.0, 0.0, 0.0) + param;
				triStream.Append(o);

				o.vertex = IN[1].vertex;
				o.normal = IN[1].normal;
				o.uv = IN[1].uv;
				o.bary = float3(0.0, 0.0, 1.0) + param;
				triStream.Append(o);

				o.vertex = IN[2].vertex;
				o.normal = IN[2].normal;
				o.uv = IN[2].uv;
				o.bary = float3(0.0, 1.0, 0.0) + param;
				triStream.Append(o);
			}

			half4 frag(g2f i) : SV_Target
			{
				half4 col = tex2D(_MainTex, i.uv) * _Color;

				Light mainLight = GetMainLight();

				// dot product between normal and light direction for standard diffuse (Lambert) lighting
				half nl = max(0.3, dot(i.normal, mainLight.direction));

				// factor in the light color
				half4 diff = nl * half4(mainLight.color, 1);

				return col * diff;
			}

			ENDHLSL
		}

	}

}