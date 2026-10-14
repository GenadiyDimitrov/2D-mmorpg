// The SELF body's see-through look (BL-82: 0.7 stealthed, 0.4 invisible). EntityView swaps every renderer of the
// model onto a copy of this while you are faded, and puts the imported materials back when you are solid.
//
// Why not just flip URP Lit's own materials to Transparent: that needs Lit's _SURFACE_TYPE_TRANSPARENT variant,
// and a player build strips any variant no SHIPPED material uses — nothing ships one, so the fade would work in
// the Editor and silently draw solid on the phone (the same trap as UnlitMaterials and PropScatter's instancing).
// This shader has no local keywords at all, lives under Resources/, and so always ships whole.
//
// Lambert + ambient, like LitVertexColor: the fade is a cue for the player, not a material. Two passes — a
// depth-only prime first, so a faded body draws as ONE shell (no far arm showing through the chest), then the
// blended colour on top of it. No ShadowCaster: a hidden character casting a solid shadow reads wrong.
Shader "L2Clone/FadeLit"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base", 2D) = "white" {}
        [MainColor] _BaseColor ("Colour", Color) = (1, 1, 1, 1)
        _Alpha ("Alpha", Range(0, 1)) = 0.4
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "DepthPrime"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 vert(float4 positionOS : POSITION) : SV_POSITION { return TransformObjectToHClip(positionOS.xyz); }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "Fade"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fog : TEXCOORD3;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                half3 n = normalize(i.normalWS);
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS), i.positionWS, half4(1, 1, 1, 1));
                half3 light = sun.color * (saturate(dot(n, sun.direction)) * sun.shadowAttenuation) + SampleSH(n);
                return half4(MixFog(albedo.rgb * light, i.fog), albedo.a * _Alpha);
            }
            ENDHLSL
        }
    }
}
