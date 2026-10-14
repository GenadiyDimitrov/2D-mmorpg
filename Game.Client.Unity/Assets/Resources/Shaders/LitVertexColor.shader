// The ground's shader (BL-344 step 3, LIT since step 5): opaque, colour straight from the mesh's vertex colours,
// times the main light (the day/night sun, DayNight.cs), its shadow, and the ambient probe. URP's own Lit ignores
// vertex colours, hence this. It lives under Resources/ so a player build always ships it (Shader.Find can only
// find what shipped — see UnlitMaterials).
//
// Deliberately Lambert and nothing else: no specular, no additional lights (fog since 0.259.0: DayNight's haze at the view range — a RING round the player since 0.260.1). The ground is a flat
// vertex-coloured map and the only thing lighting has to do to it is follow the sun and take a shadow.
//
// The vertex colours are already in the right space: BiomeGround writes Color.linear in a linear-space project,
// so nothing here converts.
//
// Step 4: also the placeholder props' shader, drawn with GPU instancing (PropScatter), hence the instancing
// variant — the ground itself draws without it. Placeholders cast no shadow (no ShadowCaster pass), by design.
Shader "L2Clone/LitVertexColor"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite On

        Pass
        {
            Name "Lit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            // Main-light shadows only. The phone asset has one cascade and no soft shadows; the PC asset has
            // both, so the editor keeps working. No screen-space variant: neither renderer uses it.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // The fog RING around the player (owner, 2026-10-14), not Unity's depth fog, which is a straight line
            // across the view. Globals set every frame by DayNight.FitFog: centre, (start, 1/(end-start), on), colour.
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
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : COLOR;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.color = i.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 n = normalize(i.normalWS);
                // The overload with a position applies the shadow-distance fade, so shadows thin out at the
                // edge of the shadow range instead of stopping on a line.
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS), i.positionWS, half4(1, 1, 1, 1));
                half3 light = sun.color * (saturate(dot(n, sun.direction)) * sun.shadowAttenuation) + SampleSH(n);
                return half4(RingFog(i.color.rgb * light, i.positionWS), 1);
            }
            ENDHLSL
        }
    }
}
