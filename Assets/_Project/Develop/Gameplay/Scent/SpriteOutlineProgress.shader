Shader "Game/Sprite Outline Progress"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Sprite Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,0.8,0,1)
        _OutlinePixels ("Outline Width (texture pixels)", Range(1,8)) = 1
        _PixelStep ("One Texture Pixel (U,V)", Vector) = (0.00390625,0.00390625,0,0)
        _Progress ("Outline Progress", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="False"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _OutlineColor;
                float4 _PixelStep;
                float _OutlinePixels;
                float _Progress;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            float SampleSpriteAlpha(float2 uv)
            {
                // Built-in sprites such as Square can use a fully opaque texture.
                // Treat UVs beyond the sprite texture as transparent so its outer edge is detectable.
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                    return 0.0;

                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS);
                output.color = input.color * _Color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                float2 stepUV = _PixelStep.xy * max(_OutlinePixels, 1.0);

                float a0 = SampleSpriteAlpha(input.uv + float2( stepUV.x, 0));
                float a1 = SampleSpriteAlpha(input.uv + float2(-stepUV.x, 0));
                float a2 = SampleSpriteAlpha(input.uv + float2(0,  stepUV.y));
                float a3 = SampleSpriteAlpha(input.uv + float2(0, -stepUV.y));
                float a4 = SampleSpriteAlpha(input.uv + float2( stepUV.x,  stepUV.y));
                float a5 = SampleSpriteAlpha(input.uv + float2(-stepUV.x,  stepUV.y));
                float a6 = SampleSpriteAlpha(input.uv + float2( stepUV.x, -stepUV.y));
                float a7 = SampleSpriteAlpha(input.uv + float2(-stepUV.x, -stepUV.y));

                float neighborMin = min(min(min(a0, a1), min(a2, a3)), min(min(a4, a5), min(a6, a7)));
                float edge = step(0.001, sprite.a) * (1.0 - step(0.99, neighborMin));

                float2 centeredUV = input.uv - 0.5;
                float angle01 = frac(atan2(centeredUV.y, centeredUV.x) / (2.0 * PI) + 1.0);
                float revealed = step(angle01, saturate(_Progress));
                float outlineMask = edge * revealed;

                float4 result = lerp(sprite, _OutlineColor * input.color, outlineMask);
                result.a = max(sprite.a, outlineMask * _OutlineColor.a * input.color.a);
                return result;
            }
            ENDHLSL
        }
    }
}
