Shader "UI/Surface Graphic"
{
 Properties {
  [PerRendererData] _MainTex ("Texture",2D)="white"{}
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
   Vary vert(App v) {
    Vary o; o.pos=UnityObjectToClipPos(v.vertex); o.color=v.color; o.uv=v.uv; o.clipPos=v.vertex.xy; return o;
   }
   float boxDistance(float2 p,float2 size){
    float r=min(_Shape.x,min(size.x,size.y));
    float2 q=abs(p)-size+r;
    return length(max(q,0))+min(max(q.x,q.y),0)-r;
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
