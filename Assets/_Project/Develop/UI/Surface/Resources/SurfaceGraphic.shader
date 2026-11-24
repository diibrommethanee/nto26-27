Shader "UI/Surface Graphic"
{
 Properties {
  [PerRendererData] _MainTex ("Texture",2D)="white"{}
  _SurfaceTexture ("Surface Texture",2D)="white"{}
  _PatternTexture ("Pattern Texture",2D)="white"{}
  _StencilComp ("Stencil Comparison",Float)=8
  _Stencil ("Stencil ID",Float)=0
  _StencilOp ("Stencil Operation",Float)=0
  _StencilWriteMask ("Stencil Write Mask",Float)=255
  _StencilReadMask ("Stencil Read Mask",Float)=255
  _ColorMask ("Color Mask",Float)=15
  [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clip",Float)=0
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
  ColorMask [_ColorMask]
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct App { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
   struct Vary { float4 pos:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float2 clipPos:TEXCOORD1; };
   float4 _Rect,_Shape,_Fill,_Edge,_Accent,_AxisX[16],_AxisY[16],_ClipRect;
   float _Shimmer; int _Count;
   sampler2D _SurfaceTexture, _PatternTexture;
   float4 _TextureTint, _PatternTint, _TextureSettings, _PatternSettings;
   float _StaggerPattern;
   Vary vert(App v) {
    Vary o; o.pos=UnityObjectToClipPos(v.vertex); o.color=v.color; o.uv=v.uv; o.clipPos=v.vertex.xy; return o;
   }
   float boxDistance(float2 p,float2 size){
    float r=min(_Shape.x,min(size.x,size.y));
    float2 q=abs(p)-size+r;
    return length(max(q,0))+min(max(q.x,q.y),0)-r;
   }
   float hashCell(float2 cell) {
    return frac(sin(dot(cell, float2(127.1, 311.7))) * 43758.5453);
   }
   float surfaceNoise(float2 p) {
    float2 cell = floor(p);
    float2 blend = frac(p);
    blend = blend * blend * (3 - 2 * blend);
    return lerp(lerp(hashCell(cell), hashCell(cell + float2(1, 0)), blend.x),
                lerp(hashCell(cell + float2(0, 1)), hashCell(cell + 1), blend.x), blend.y);
   }
   float3 applyTexture(float3 body, float2 local, float2 uv) {
    float kind = _TextureSettings.x;
    float amount = _TextureSettings.y * _TextureTint.a;
    if (kind < .5 || amount <= 0) return body;

    float2 tile = local / _TextureSettings.z;
    if (kind > 3.5) {
     float2 sampleUV = _TextureSettings.w > .5 ? uv : tile;
     float2 mapped = _TextureSettings.w > .5 ? sampleUV : frac(sampleUV);
     float4 paint = tex2Dgrad(_SurfaceTexture, mapped, ddx(sampleUV), ddy(sampleUV));
     return lerp(body, paint.rgb * _TextureTint.rgb, amount * paint.a);
    }

    float detail;
    if (kind < 1.5) {
     float fibres = surfaceNoise(tile * float2(64, 21)) - .5;
     float pulp = surfaceNoise(tile * 28) - .5;
     detail = .65 * fibres + .35 * pulp;
    } else if (kind < 2.5) {
     float2 thread = tile * 30;
     float2 weave = .5 + .5 * cos(thread * 6.2831853);
     float over = fmod(floor(thread.x) + floor(thread.y), 2);
     detail = lerp(weave.x, weave.y, over) * .7 + min(weave.x, weave.y) * .3 - .5;
    } else {
     detail = surfaceNoise(tile * 74) - .5;
    }
    float footprint = max(length(ddx(tile)), length(ddy(tile)));
    detail *= 1 - smoothstep(.008, .028, footprint);
    return body * lerp(float3(1, 1, 1), _TextureTint.rgb * (1 + detail), amount);
   }
   float starDistance(float2 p) {
    const float stepAngle = .62831853;
    float sector = (atan2(p.y, p.x + 1e-6) + 3.14159265) / stepAngle;
    float phase = frac(sector) * stepAngle;
    float outerFirst = 1 - fmod(floor(sector), 2);
    float r0 = lerp(.44, 1, outerFirst);
    float r1 = lerp(1, .44, outerFirst);
    float radius = r0 * r1 * sin(stepAngle) / (r1 * sin(stepAngle - phase) + r0 * sin(phase));
    return length(p) - radius;
   }
   float4 patternColor(float2 centred) {
    float kind = _PatternSettings.x;
    if (kind < .5 || _PatternSettings.y <= 0) return 0;

    float2 cell = centred / _PatternSettings.z + .5;
    cell.x += fmod(abs(floor(cell.y)), 2) * .5 * _StaggerPattern;
    float2 p = (frac(cell) - .5) * (2 / max(.05, _PatternSettings.w));
    float aa = max(.012, length(fwidth(centred)) * 2 / (_PatternSettings.z * max(.05, _PatternSettings.w)));
    if (kind > 5.5) {
     float2 uv = p * .5 + .5;
     float coverage = 1 - smoothstep(1 - aa, 1, max(abs(p.x), abs(p.y)));
     float4 stamp = tex2D(_PatternTexture, saturate(uv)) * _PatternTint;
     stamp.a *= coverage * _PatternSettings.y;
     return stamp;
    }

    float d;
    if (kind < 1.5) d = length(p) - 1;
    else if (kind < 2.5) d = abs(length(p) - .77) - .16;
    else if (kind < 3.5) d = starDistance(p);
    else if (kind < 4.5) d = abs(p.x) + abs(p.y) - 1;
    else {
     float2 a = abs(p);
     d = min(max(a.x - .23, a.y - 1), max(a.y - .23, a.x - 1));
    }
    float coverage = 1 - smoothstep(-aa, aa, d);
    return float4(_PatternTint.rgb, coverage * _PatternTint.a * _PatternSettings.y);
   }
   float4 frag(Vary i):SV_Target {
    float3 p=float3(_Rect.xy+i.uv*_Rect.zw,1);
    float d=1e6;
    float k=max(0.001,_Shape.y);
    if(_Count==0) d=boxDistance((i.uv-.5)*_Rect.zw,_Rect.zw*.5);
    for(int n=0;n<_Count;n++){
     float b=boxDistance(float2(dot(_AxisX[n].xyz,p),dot(_AxisY[n].xyz,p)),float2(_AxisX[n].w,_AxisY[n].w));
     float h=max(k-abs(d-b),0)/k;
     d=min(d,b)-k*h*h*.25;
    }
    // Keep the body still. Only the thin outer lip ripples.
    float t = _Time.y;
    float wave = sin(p.x * .018 + p.y * .024 + t * .85)
               + .32 * sin(p.x * .047 - p.y * .031 - t * 1.15);
    float amplitude = min(_Shape.w, .035 * min(_Rect.z, _Rect.w));
    float rimDistance = d + amplitude * (.76 * wave + (_Count == 0 ? 1.01 : 0));
    float aa = max(fwidth(rimDistance), .01);
    float depth = max(-rimDistance, 0);
    float rimWidth = _Shape.z;
    float rimOn = step(.01, rimWidth);
    float2 centred = i.uv - .5;
    float2 radial = centred / max(length(centred), .001);
    // A narrow reflection stays on the upper shoulder of the frame.
    float angle = 2.16 + .055 * sin(t * .30);
    float2 orbit = float2(cos(angle), sin(angle));
    float gleam = pow(saturate(.5 + .5 * dot(radial, orbit)), 26) * _Shimmer;
    float pearl = smoothstep(-.35, .55, dot(centred, float2(-.35, .8)));
    float3 enamel = _Fill.rgb * lerp(.74, 1.12, i.uv.y);
    enamel += _Edge.rgb * (.012 + .035 * pearl) * _Shimmer;
    enamel = applyTexture(enamel, i.uv * _Rect.zw, i.uv);
    float4 pattern = patternColor(centred * _Rect.zw);
    enamel = lerp(enamel, pattern.rgb, pattern.a);
    float bevel = exp2(-depth / max(1, 1.8 * rimWidth)) * rimOn;
    float3 body = enamel + bevel * lerp(_Edge.rgb, _Accent.rgb, gleam) * (.13 + .32 * gleam);
    // A fine recessed line follows the stable inner silhouette.
    float inset = abs(d + 2.6 * rimWidth + _Shape.w);
    float fillet = (1 - smoothstep(.25, .25 + aa, inset)) * rimOn;
    body = lerp(body, _Edge.rgb, fillet * (.27 + .32 * gleam));
    float rim = smoothstep(-aa, 0, rimDistance + rimWidth) * rimOn;
    float facing = saturate(.5 - .48 * ddx(d) / max(fwidth(d), .01)
                               + .42 * ddy(d) / max(fwidth(d), .01));
    float3 metal = lerp(_Edge.rgb * .45, _Accent.rgb, saturate(.46 * facing + gleam));
    float hairline = 1 - smoothstep(0, aa + .5, abs(rimDistance + rimWidth * .52));
    metal += _Accent.rgb * hairline * (.12 + .45 * gleam);
    float4 c = float4(lerp(body, metal, rim), lerp(_Fill.a, _Edge.a, rim)) * i.color;
    c.a *= 1 - smoothstep(-aa, 0, rimDistance);
    #ifdef UNITY_UI_CLIP_RECT
    c.a*=UnityGet2DClipping(i.clipPos,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
    clip(c.a-.001);
    #endif
    return c;
   }
   ENDCG
  }
 }
}
