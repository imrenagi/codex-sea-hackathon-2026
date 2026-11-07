Shader "JalanKita/Reflective Water"
{
    Properties { _DeepColor("Deep water", Color)=(.018,.13,.15,1) _ShallowColor("Shallow water",Color)=(.07,.29,.28,1) _ReflectionTex("Reflection",2D)="black"{} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_ReflectionTex); SAMPLER(sampler_ReflectionTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _DeepColor, _ShallowColor;
            float4x4 _ReflectionVP;
            float4 _Pool;
            float _WaterTime, _HasReflection, _ImpactRadius;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float fog:TEXCOORD1; };
            Varyings vert(Attributes v) { Varyings o; o.world=TransformObjectToWorld(v.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world); o.fog=ComputeFogFactor(o.positionCS.z); return o; }
            half4 frag(Varyings i):SV_Target
            {
                float2 p=i.world.xz; float t=_WaterTime;
                float2 d=float2(sin(dot(p,float2(.8,1.3))+t*1.2)+.35*sin(dot(p,float2(4.1,-2.6))-t*2.4),cos(dot(p,float2(-1.2,.7))-t*.9)+.3*cos(dot(p,float2(3.2,4.7))+t*2));
                float radius=distance(p,_Pool.xy);
                float impact=exp(-abs(radius-_ImpactRadius)*1.4);
                float ripple=sin(abs(radius-_ImpactRadius)*9-t*4.5)*impact;
                float3 n=normalize(float3(d.x*.085+ripple*.03,1,d.y*.085+ripple*.03));
                float3 v=normalize(GetCameraPositionWS()-i.world);
                float fresnel=.06+.78*pow(1-saturate(dot(n,v)),4);
                float4 clip=mul(_ReflectionVP,float4(i.world,1));
                float2 uv=clip.xy/max(clip.w,.01)*.5+.5;
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y=1-uv.y;
                #endif
                uv+=d*.0016;
                float3 reflection=SAMPLE_TEXTURE2D(_ReflectionTex,sampler_ReflectionTex,saturate(uv)).rgb;
                float3 sky=lerp(float3(.35,.53,.63),float3(.7,.79,.82),saturate(v.y));
                reflection=lerp(sky,reflection,_HasReflection);
                float depth=saturate((_Pool.z-radius)*.4);
                float caustic=pow(saturate(sin(p.x*2.8+t*.6)*sin(p.y*3.4-t*.5)),8)*.035;
                float3 base=lerp(_ShallowColor.rgb,_DeepColor.rgb,depth)+caustic;
                Light sun=GetMainLight(); float3 h=normalize(sun.direction+v);
                float glint=pow(saturate(dot(n,h)),240)*1.5;
                float foam=impact*pow(saturate(sin(radius*24-t*8)+sin(p.x*16+p.y*13+t*3)*.3),6)*.18;
                foam+=exp(-max(0,_Pool.z-radius)*4)*.045;
                float3 color=lerp(base,reflection,fresnel)+sun.color*glint+foam;
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}
