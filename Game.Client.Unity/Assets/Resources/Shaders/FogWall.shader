// The fog WALL (0.264.0): a cylinder standing on the fog ring, drawn by DayNight. Owner: *"if the fog is around
// extending upwards like a wall not only on ground this will fix the 'me seeing the edge of the world as a line inside
// the thin white fog'"*. It is the ring's own colour (_RingFogColor, the same global the ground and props fog to),
// fully opaque from the ground to just above the camera — so no line of sight down to ground BEYOND the ring can get
// past it — then fading out into the sky. The opacity is the mesh's vertex alpha.
//
// Transparent queue, no depth write: everything inside the ring is in front of it and depth-tests it away; the ground
// beyond is drawn first and covered.
Shader "L2Clone/FogWall"
{
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FogWall"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            half4 _RingFogColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half alpha : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.alpha = v.color.a;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(_RingFogColor.rgb, i.alpha);
            }
            ENDHLSL
        }
    }
}
