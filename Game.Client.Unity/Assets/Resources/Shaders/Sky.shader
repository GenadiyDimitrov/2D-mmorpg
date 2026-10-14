// The sky (0.259.0): a two-colour gradient, horizon to zenith, plus a soft glow round the sun (or moon). DayNight
// sets the three colours by the hour, and gives the fog the HORIZON colour, so the ground fades into the sky at the
// view range instead of ending on an edge. Everything below the horizon is the horizon colour for the same reason:
// past the map's border there is only fog. Lives under Resources/ so the build ships it (see UnlitMaterials).
Shader "L2Clone/Sky"
{
    Properties
    {
        _Zenith ("Zenith", Color) = (0.28, 0.52, 0.88, 1)
        _Horizon ("Horizon", Color) = (0.70, 0.82, 0.93, 1)
        _Glow ("Sun glow", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith;
                half4 _Horizon;
                half4 _Glow;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.dir = i.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                half t = pow(saturate(d.y / 0.6), 0.7);
                half3 c = lerp(_Horizon.rgb, _Zenith.rgb, t);
                c += _Glow.rgb * pow(saturate(dot(d, _MainLightPosition.xyz)), 48.0);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
