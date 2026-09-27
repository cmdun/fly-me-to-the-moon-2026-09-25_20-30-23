Shader "Moon/IllustratedSurface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _UVRect ("Atlas region", Vector) = (0,0,1,1)
        _Diameter ("Diameter", Float) = 10
        _Seed ("Seed", Float) = 1
        _Home ("Home", Float) = 0
        _Base ("Stone", Color) = (.2,.4,.6,1)
        _Light ("Lit stone", Color) = (.4,.7,.8,1)
        _Dark ("Shadow", Color) = (.1,.2,.4,1)
        _Rim ("Living rim", Color) = (.4,.8,.6,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
            CBUFFER_START(UnityPerMaterial)
            float4 _UVRect, _Base, _Light, _Dark, _Rim;
            float _Diameter, _Seed, _Home;
            CBUFFER_END
            Varyings vert(Attributes v) { Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=(v.uv-_UVRect.xy)/_UVRect.zw;o.color=v.color;return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7))+_Seed)*43758.5453); }
            half4 frag(Varyings i):SV_Target
            {
                // A shared 32 pixels/unit grid gives home and the smallest moon equally crisp surfaces.
                float2 p=(floor((i.uv-.5)*_Diameter*32)+.5)/32;
                float radius=_Diameter*.5, depth=radius-length(p);
                clip(depth);
                float light=dot(p/radius,normalize(float2(-.6,.8)));
                half3 c=light>.35?lerp(_Base.rgb,_Light.rgb,.3):light<-.3?lerp(_Base.rgb,_Dark.rgb,.35):_Base.rgb;
                // Deliberate broad facets and small paired flecks, not a stretched photographic texture.
                float2 tile=floor(p*2.4);float fleck=hash(tile);
                float2 cell=frac(p*2.4);
                if(fleck>.83 && cell.x<.4 && cell.y<.13) c=lerp(c,_Light.rgb,.22);
                if(fleck<.08 && cell.x>.6 && cell.y>.78) c=lerp(c,_Dark.rgb,.22);
                for(int k=0;k<7;k++)
                {
                    // Authored, separated crater groupings; rotate and gently vary them per moon.
                    float3 crater=k==0?float3(-.28,.36,.15):k==1?float3(.35,.20,.10):k==2?float3(-.15,-.1,.19):
                        k==3?float3(.35,-.35,.16):k==4?float3(-.52,-.35,.12):k==5?float3(.1,.62,.07):float3(-.62,.12,.06);
                    float angle=hash(float2(8,4))*6.283185;
                    float2 center=crater.xy+float2(hash(float2(k,3))-.5,hash(float2(k,7))-.5)*.035;
                    center=float2(center.x*cos(angle)-center.y*sin(angle),center.x*sin(angle)+center.y*cos(angle))*radius;
                    float cr=crater.z*radius*(.95+hash(float2(k,12))*.1);
                    float d=length(p-center);
                    if(d<cr+.065 && depth>.8)
                    {
                        if(d>cr) c=_Light.rgb;
                        else if(d>cr-.07) c=_Dark.rgb;
                        else c=lerp(_Dark.rgb,_Base.rgb, (p.y-center.y)<0?.45:.16);
                    }
                }
                if(_Home>.5 && depth<.7) c=depth<.24?_Rim.rgb:depth<.33?_Dark.rgb:half3(.52,.32,.18);
                if(_Home<.5 && depth<.23) c=depth<.11?_Rim.rgb:lerp(_Rim.rgb,_Dark.rgb,.32);
                if(depth<.032) c=lerp(_Rim.rgb,_Light.rgb,.5);
                return half4(c,1)*i.color;
            }
            ENDHLSL
        }
    }
}
