// The ground's shader (BL-344 step 3): unlit, opaque, colour straight from the mesh's vertex colours.
// URP's own Unlit ignores vertex colours, hence this. It lives under Resources/ so a player build always
// ships it (Shader.Find can only find what shipped — see UnlitMaterials). Lighting arrives in step 5.
Shader "L2Clone/UnlitVertexColor"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite On

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.color = i.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return half4(i.color.rgb, 1); }
            ENDHLSL
        }
    }
}
