// The scenery props' shader (0.262.0): trees, rocks, grass and flowers, drawn instanced by PropScatter. Until now they
// wore URP Lit, whose fog is Unity's DEPTH fog — a flat line across the view — so with the ground fogging as a RING
// round the player (LitVertexColor, 0.261.0) the trees stood bright inside the haze to the sides. This one fogs by
// the same ring, off the same globals DayNight.FitFog sets.
//
// It reads the property names URP Lit used (_BaseMap, _BaseColor, _Cutoff, _AlphaClip, _Cull), so the nature
// materials only swap shaders. Lambert + ambient and nothing else, as the Lit setup already had specular and
// reflections off; props receive no shadows (PropScatter draws them with receiveShadows off). They do CAST, on the
// "characters + trees" setting, hence the ShadowCaster pass.
Shader "L2Clone/PropLit"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
        _AlphaClip ("Clip (0/1)", Float) = 0
        _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Cull [_Cull]
        ZWrite On

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            half _AlphaClip;
        CBUFFER_END
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        // A clipped material drops texels below the cutoff; an opaque one (_AlphaClip 0) compares against 0 and
        // keeps everything. No keyword, so no variants to strip.
        void ClipAlpha(float2 uv)
        {
            clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a * _BaseColor.a - _Cutoff * _AlphaClip);
        }
        ENDHLSL

        Pass
        {
            Name "Lit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // The fog RING — the same function as LitVertexColor's; keep the two alike.
            float4 _RingFogCenter;
            float4 _RingFogParams;
            half4 _RingFogColor;
            half3 RingFog(half3 c, float3 positionWS)
            {
                float d = distance(positionWS.xz, _RingFogCenter.xz);
                half f = saturate((d - _RingFogParams.x) * _RingFogParams.y) * _RingFogParams.z;
                return lerp(c, _RingFogColor.rgb, f);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                clip(tex.a - _Cutoff * _AlphaClip);
                half3 n = normalize(i.normalWS);
                Light sun = GetMainLight();
                half3 light = sun.color * saturate(dot(n, sun.direction)) + SampleSH(n);
                return half4(RingFog(tex.rgb * light, i.positionWS), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;   // set by URP for the directional shadow pass

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                float3 positionWS = TransformObjectToWorld(i.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(i.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                o.positionCS = positionCS;
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                ClipAlpha(i.uv);
                return 0;
            }
            ENDHLSL
        }
    }
}
