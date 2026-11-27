Shader "Project/Environment"
{
    Properties
    {
        _MainTex ("Environment", 2D) = "white" {}
        [HideInInspector] _BubbleStencilTest ("Bubble stencil test", Float) = 8
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Stencil { Ref 128 ReadMask 128 Comp [_BubbleStencilTest] Pass Keep }
            Blend One OneMinusSrcAlpha
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float _Radius;
            float4 _Focus; // Viewport center, aspect ratio, radius in screen heights.
            float2 _NpcFocus;
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(float4 position : POSITION)
            {
                Varyings o;
                o.position = float4(position.xy * 2, 0, 1);
                o.uv = position.xy + 0.5;
                #if UNITY_UV_STARTS_AT_TOP
                o.uv.y = 1 - o.uv.y;
                #endif
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                // The composited texture UV matches the camera viewport here.
                float2 viewport = i.uv;
                float2 aspect = float2(_Focus.z * 0.76, 1);
                float2 fromPlayer = (viewport - _Focus.xy) * aspect;
                float2 betweenActors = (_NpcFocus - _Focus.xy) * aspect;
                float along = saturate(dot(fromPlayer, betweenActors) / max(dot(betweenActors, betweenActors), 0.00001));
                float2 offsetFromActors = fromPlayer - along * betweenActors;
                offsetFromActors.y *= offsetFromActors.y < 0 ? 0.65 : 1.0;
                float distanceToActors = length(offsetFromActors);
                float weight = 1 - smoothstep(_Focus.w * 0.10, _Focus.w * 0.55, distanceToActors);
                float radius = _Radius * lerp(0.06, 1.0, weight);
                if (radius < 0.05)
                    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                // Two staggered rings approximate a circular lens footprint.
                // Fixed directions keep fine linework stable as the camera moves.
                const float2 directions[6] = {
                    float2(1, 0), float2(0.5, 0.8660254), float2(-0.5, 0.8660254),
                    float2(-1, 0), float2(-0.5, -0.8660254), float2(0.5, -0.8660254)
                };
                float2 stepUV = abs(_MainTex_TexelSize.xy) * radius;
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * 0.16h;
                [unroll]
                for (int n = 0; n < 6; n++)
                {
                    float2 d = directions[n];
                    float2 rotated = float2(0.8660254 * d.x - 0.5 * d.y,
                                             0.5 * d.x + 0.8660254 * d.y);
                    color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + d * stepUV * 0.82) * 0.09h;
                    color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + rotated * stepUV * 1.72) * 0.05h;
                }
                return color;
            }
            ENDHLSL
        }
    }
}
