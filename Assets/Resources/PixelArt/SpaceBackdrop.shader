Shader "Moon/SpaceBackdrop"
{
    Properties
    {
        _Nebula ("Nebula", 2D) = "black" {}
        _DeepStars ("Distant stars and galaxies", 2D) = "black" {}
        _MiddleStars ("Middle stars", 2D) = "black" {}
        _NearStars ("Near stars", 2D) = "black" {}
        _ViewPosition ("Camera", Vector) = (0,0,0,0)
        _Meteor ("Shooting star", Vector) = (0,0,0,0)
        _SkyTime ("Paused sky clock", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off Blend Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION;float2 world:TEXCOORD0; };
            TEXTURE2D(_Nebula);SAMPLER(sampler_Nebula);
            TEXTURE2D(_DeepStars);SAMPLER(sampler_DeepStars);
            TEXTURE2D(_MiddleStars);SAMPLER(sampler_MiddleStars);
            TEXTURE2D(_NearStars);SAMPLER(sampler_NearStars);
            CBUFFER_START(UnityPerMaterial)
            float4 _ViewPosition, _Meteor;
            float _SkyTime;
            CBUFFER_END
            Varyings vert(Attributes i)
            {
                Varyings o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);
                o.world=TransformObjectToWorld(i.positionOS.xyz).xy;return o;
            }
            float2 skyUV(float2 world,float depth,float span)
            { return (world-_ViewPosition.xy*(1-depth))/span+.5; }
            half4 frag(Varyings i):SV_Target
            {
                half3 color=SAMPLE_TEXTURE2D(_Nebula,sampler_Nebula,skyUV(i.world,.035,64)).rgb;
                color+=SAMPLE_TEXTURE2D(_DeepStars,sampler_DeepStars,skyUV(i.world,.06,51.2)).rgb;
                color+=SAMPLE_TEXTURE2D(_MiddleStars,sampler_MiddleStars,skyUV(i.world,.16,25.6)).rgb
                    *(.94+.06*sin(_SkyTime*.8));
                color+=SAMPLE_TEXTURE2D(_NearStars,sampler_NearStars,skyUV(i.world,.3,12.8)).rgb
                    *(.91+.09*sin(_SkyTime*1.1+2));
                // A quiet, pixel-stepped tail. Every part remains behind the playable world.
                float2 delta=(floor(i.world*40)+.5)/40-_Meteor.xy;
                float along=dot(delta,normalize(float2(3,-1)));
                float across=abs(dot(delta,normalize(float2(1,3))));
                float tail=saturate(1+along/1.6)*step(along,.035)*step(-1.6,along);
                float streak=step(across,.024)*tail*_Meteor.z;
                color+=half3(.13,.23,.29)*streak;
                color+=half3(.3,.38,.4)*step(length(delta),.042)*_Meteor.z;
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
