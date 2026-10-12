// The skill effects' shader (BL-344 step 6, FxPlayer.cs): additive, unlit, the particle's own vertex colour times a
// soft round falloff computed from the quad's UV. No texture at all: one less asset to ship and to strip, and a
// stretched billboard (arrows, the slash) gets a soft streak from the same maths for free.
//
// Additive because light only ADDS: a glow over a dark corpse or a night field still reads, and overlapping sparks
// brighten instead of sorting against each other, so the particles never need back-to-front sorting.
// ⚠ A debuff therefore cannot be DARK — its motes are a dim purple, which additive renders as a dusky haze.
//
// Lives under Resources/ so a player build always ships it (Shader.Find only finds what shipped; see UnlitMaterials).
Shader "L2Clone/FxAdditive"
{
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FxAdditive"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 1 at the centre, 0 at the rim; squared for a hot core and a soft edge.
                float d = saturate(1.0 - length(i.uv * 2.0 - 1.0));
                half a = (half)(d * d) * i.color.a;
                return half4(i.color.rgb * a, 1.0h);
            }
            ENDHLSL
        }
    }
}
