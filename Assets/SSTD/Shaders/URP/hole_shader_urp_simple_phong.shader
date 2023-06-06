Shader "Custom/hole_shader_urp_simple_phong"
{
	Properties{
		[Header(Surface options)]
		[MainTexture] _MainTex("_MainTex", 2D) = "white" {}
		[MainColor] _Color("_Color", Color) = (1, 1, 1, 1)
		_Smoothness("_Smoothness", Float) = 5

		_Gid("_Gid", Int) = 1
	}
	// Subshaders allow for different behaviour and options for different pipelines and platforms
	SubShader{
		// These tags are shared by all passes in this sub shader
		Tags {"RenderPipeline" = "UniversalPipeline"}

		ColorMask RGB
		Cull Front
		ZTest Always

		Stencil {
			Ref[_Gid]
			Comp NotEqual
			Pass Keep
		}

		// Shaders can have several passes which are used to render different data about the material
		// Each pass has it's own vertex and fragment function and shader variant keywords
		Pass {
			Name "ForwardLit"

			// "UniversalForward" tells Unity this is the main lighting pass of this shader
			Tags{"LightMode" = "UniversalForward"}

			ColorMask RGB
			Cull Front
			ZTest Always

			Stencil {
				Ref[_Gid]
				Comp NotEqual
				Pass Keep
			}

			HLSLPROGRAM // Begin HLSL code

			#pragma multi_compile_instancing

			#define _SPECULAR_COLOR

			// Global URP keywords
			#pragma multi_compile_fragment _ _SHADOWS_SOFT

			// Register our programmable stage functions
			#pragma vertex Vertex
			#pragma fragment Fragment

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			// Textures
			TEXTURE2D(_MainTex); 
			SAMPLER(sampler_MainTex);

			float4 _MainTex_ST;
			float4 _Color;
			float _Smoothness;

			// This attributes struct receives data about the mesh we're currently rendering
			// Data is automatically placed in fields according to their semantic
			struct Attributes {
				float3 positionOS : POSITION; // Position in object space
				float3 normalOS : NORMAL; // Normal in object space
				float2 uv : TEXCOORD0; // Material texture UVs

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			// This struct is output by the vertex function and input to the fragment function.
			// Note that fields will be transformed by the intermediary rasterization stage
			struct Interpolators {
				// This value should contain the position in clip space (which is similar to a position on screen)
				// when output from the vertex function. It will be transformed into pixel position of the current
				// fragment on the screen when read from the fragment function
				float4 positionCS : SV_POSITION;

				// The following variables will retain their values from the vertex stage, except the
				// rasterizer will interpolate them between vertices
				float2 uv : TEXCOORD0; // Material texture UVs
				float3 positionWS : TEXCOORD1; // Position in world space
				float3 normalWS : TEXCOORD2; // Normal in world space

				UNITY_VERTEX_OUTPUT_STEREO
			};

			// The vertex function. This runs for each vertex on the mesh.
			// It must output the position on the screen each vertex should appear at,
			// as well as any data the fragment function will need
			Interpolators Vertex(Attributes input) {
				Interpolators output;

				UNITY_SETUP_INSTANCE_ID(input);
				output = (Interpolators)0;
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				// These helper functions, found in URP/ShaderLib/ShaderVariablesFunctions.hlsl
				// transform object space values into world and clip space
				VertexPositionInputs posnInputs = GetVertexPositionInputs(input.positionOS);
				VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

				// Pass position and orientation data to the fragment function
				output.positionCS = posnInputs.positionCS;
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);
				output.normalWS = normInputs.normalWS;
				output.positionWS = posnInputs.positionWS;

				return output;
			}

			// The fragment function. This runs once per fragment, which you can think of as a pixel on the screen
			// It must output the final color of this pixel
			float4 Fragment(Interpolators input) : SV_TARGET{
				float2 uv = input.uv;
				// Sample the color map
				float4 colorSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
				//float4 colorSample = _Color;

				// For lighting, create the InputData struct, which contains position and orientation data
				InputData lightingInput = (InputData)0; // Found in URP/ShaderLib/Input.hlsl
				lightingInput.positionWS = input.positionWS;
				lightingInput.normalWS = normalize(input.normalWS);
				lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS); // In ShaderVariablesFunctions.hlsl
				lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS); // In Shadows.hlsl

				// Calculate the surface data struct, which contains data from the material textures
				SurfaceData surfaceInput = (SurfaceData)0;
				surfaceInput.albedo = colorSample.rgb * _Color.rgb;
				surfaceInput.emission = colorSample.rgb * _Color.rgb;
				surfaceInput.alpha = colorSample.a * _Color.a;
				surfaceInput.specular = 1;
				surfaceInput.smoothness = _Smoothness;

			#if UNITY_VERSION >= 202120
				return UniversalFragmentBlinnPhong(lightingInput, surfaceInput);
			#else
				return UniversalFragmentBlinnPhong(lightingInput, surfaceInput.albedo, float4(surfaceInput.specular, 1), surfaceInput.smoothness, surfaceInput.emission, surfaceInput.alpha);
			#endif
			}

			ENDHLSL
		}

	}

}