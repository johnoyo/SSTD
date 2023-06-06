Shader "Custom/hole_prepare_front_urp_shader"
{
    Properties
    {
        _Gid ("_Gid", Int) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" "RenderPipeline" = "UniversalPipeline" }

        ColorMask 0
        ZWrite off
        ZTest off

        Stencil {
            Ref [_Gid]
            Comp Always
            Pass Replace
        }
    
        Pass {
            Cull Front
            ZTest Less

            HLSLPROGRAM
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            ENDHLSL
        }
    }
}
