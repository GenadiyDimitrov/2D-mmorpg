// The ground's shader (BL-344 step 3): unlit, opaque, colour straight from the mesh's vertex colours.
// URP's own Unlit ignores vertex colours, hence this. It lives under Resources/ so a player build always
// ships it (Shader.Find can only find what shipped — see UnlitMaterials). Lighting arrives in step 5.
// Step 4: also the placeholder props' shader, drawn with GPU instancing (PropScatter), hence the
// instancing variant — the ground itself draws without it.
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
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.color = i.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return half4(i.color.rgb, 1); }
            ENDHLSL
        }
    }
}
