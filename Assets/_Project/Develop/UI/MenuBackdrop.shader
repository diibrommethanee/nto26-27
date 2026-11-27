Shader "Lumimoth/Menu Backdrop"
{
    Properties { [NoScaleOffset] _StarTex ("Star Sprite Sheet", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Vertex { float4 position : POSITION; fixed4 color : COLOR; };
            struct Pixel { float4 position : SV_POSITION; float4 screen : TEXCOORD0; fixed4 tint : COLOR; };
            float _IntroProgress, _Focus, _Reveal, _Clock, _StarIntensity, _StarAspect;
            sampler2D _StarTex;
            float4 _StarUV, _StarTex_TexelSize;
            float4 _BookSize;

            Pixel vert(Vertex v)
            {
                Pixel o;
                o.position = UnityObjectToClipPos(v.position);
                o.screen = ComputeScreenPos(o.position);
                o.tint = v.color;
                return o;
            }

            float hash(float2 p)
            {
                float3 v = frac(p.xyx * .1031);
                v += dot(v, v.yzx + 33.33);
                return frac((v.x + v.y) * v.z);
            }

            float3 stars(float2 p, float density, float layerPhase)
            {
                p *= density;
                float2 cell = floor(p);
                float seed = hash(cell);
                float2 center = .22 + .56 * float2(seed, hash(cell + 19));
                float phase = seed * 6.283185 + layerPhase;
                float beat = _Clock * lerp(.78, 1.25, seed) + phase;
                center += lerp(.62, 1.0, seed) * float2(.096 * sin(beat), .072 * cos(beat + .35 * sin(phase)));
                float height = lerp(.085, .17, seed);
                float2 local = (frac(p) - center) / (height * float2(_StarAspect, 1)) + .5;
                float2 inset = .5 * _StarTex_TexelSize.xy;
                float2 uv = lerp(_StarUV.xy + inset, _StarUV.zw - inset, saturate(local));
                fixed4 star = tex2D(_StarTex, uv);
                float inBounds = all(local >= 0) && all(local <= 1);
                float glint = smoothstep(.55, 1, sin(beat));
                float shimmer = .72 + .28 * glint;
                return star.rgb * star.a * inBounds * shimmer * smoothstep(.64, .9, seed);
            }

            fixed4 frag(Pixel i) : SV_Target
            {
                float2 uv = i.screen.xy / i.screen.w;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 p = (uv - .5) * float2(aspect, 1);
                float beamAxis = p.x + p.y * .62 + .18 + .02 * sin(_Clock * .17);
                float beam = exp(-beamAxis * beamAxis * 4) * saturate(.72 + p.y);
                float vignette = exp(-dot(p,p) * 2.6);
                float3 color = lerp(float3(.061,.065,.085), float3(.25,.205,.205), vignette);
                color += float3(.17,.124,.075) * beam;
                float2 bookDistance = abs(p - float2(.008,-.018)) - _BookSize.xy * float2(1 + _Focus,1) * .48;
                color += (hash(floor(i.position.xy)) - .5) * .018 * (1 - .8 * _Focus);
                float3 starlight = stars(p, 7, 0) + .48 * stars(p + 11, 10, 2.4);
                color += starlight * (.3 + .7 * beam) * _StarIntensity;
                float2 fadeDistance = max(bookDistance, 0);
                fadeDistance.x = max(fadeDistance.x - _BookSize.y * .85, 0) * (.24 / .28);
                float gameVisibility = lerp(.07, .40, smoothstep(0, .24, length(fadeDistance)));
                return fixed4(lerp(i.tint.rgb, color, _IntroProgress),
                    i.tint.a * (1 - gameVisibility * _IntroProgress) * (1 - _Reveal));
            }
            ENDCG
        }
    }
}
