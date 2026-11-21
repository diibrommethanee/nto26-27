Shader "Scent/Ribbon"
{
    Properties
    {
        _FlowTexture ("Flow Texture", 2D) = "white" {}
        _ScentColor ("Scent Color", Color) = (1, 0.4, 0.05, 1)
        _FlowSpeed ("Flow Speed", Float) = 0.5
        _WaveAmplitude ("Wave Amplitude", Float) = 0.1
        _RibbonWidth ("Ribbon Width", Float) = 0.3
        _Intensity ("Intensity", Range(0, 1)) = 1
        _Start ("Start", Vector) = (0, 0, 0, 0)
        _End ("End", Vector) = (2, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _FlowTexture;
            float4 _FlowTexture_ST;
            float4 _ScentColor;
            float4 _Start;
            float4 _End;
            float _FlowSpeed;
            float _WaveAmplitude;
            float _RibbonWidth;
            float _Intensity;

            struct AppData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexToFragment
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            VertexToFragment Vert(AppData input)
            {
                VertexToFragment output;
                float t = saturate(input.uv.x);
                float side = input.uv.y * 2.0 - 1.0;

                float3 direction = _End.xyz - _Start.xyz;
                float directionLength = max(length(direction.xy), 0.0001);
                float2 normal = float2(-direction.y, direction.x) / directionLength;
                float3 center = lerp(_Start.xyz, _End.xyz, t);

                float wave = sin(t * 18.0 - _Time.y * (1.0 + _FlowSpeed * 4.0)) *
                             _WaveAmplitude * sin(t * 3.14159265);
                float width = lerp(_RibbonWidth, _RibbonWidth * 0.15, t);
                float curve = directionLength * 0.12 * sin(t * 3.14159265);
                center.xy += normal * (side * width * 0.5 + curve + wave);

                output.position = UnityObjectToClipPos(float4(center, 1.0));
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 samplePosition)
            {
                samplePosition = frac(samplePosition * float2(123.34, 456.21));
                samplePosition += dot(samplePosition, samplePosition + 45.32);
                return frac(samplePosition.x * samplePosition.y);
            }

            float Noise(float2 samplePosition)
            {
                float2 cell = floor(samplePosition);
                float2 blend = frac(samplePosition);
                blend = blend * blend * (3.0 - 2.0 * blend);

                float bottom = lerp(
                    Hash(cell),
                    Hash(cell + float2(1.0, 0.0)),
                    blend.x);
                float top = lerp(
                    Hash(cell + float2(0.0, 1.0)),
                    Hash(cell + float2(1.0, 1.0)),
                    blend.x);

                return lerp(bottom, top, blend.y);
            }

            fixed4 Frag(VertexToFragment input) : SV_Target
            {
                float2 flowUv = input.uv;
                flowUv.x -= _Time.y * _FlowSpeed;

                float broadNoise = Noise(float2(
                    flowUv.x * 18.0,
                    flowUv.y * 7.0));
                float fineNoise = Noise(float2(
                    flowUv.x * 37.0 + 4.2,
                    flowUv.y * 13.0));
                float textureNoise = tex2D(_FlowTexture, flowUv * float2(2.0, 3.0)).r;
                float density = broadNoise * 0.55 + fineNoise * 0.25 + textureNoise * 0.2;
                density = smoothstep(0.22, 0.78, density);

                float distanceFromCenter = abs(input.uv.y * 2.0 - 1.0);
                float softEdge = 1.0 - smoothstep(0.62, 1.0, distanceFromCenter);
                float endFade = 1.0 - smoothstep(0.78, 1.0, input.uv.x);
                float alpha = density * softEdge * endFade *
                              _Intensity * _ScentColor.a;

                return fixed4(_ScentColor.rgb, alpha);
            }
            ENDCG
        }
    }
}
