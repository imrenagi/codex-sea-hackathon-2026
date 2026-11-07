Shader "JalanKita/Fountain"
{
    Properties { _BaseColor("Water",Color)=(.67,.86,.91,.7) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; float fog:TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor; float _WaterTime, _Droplet;
            CBUFFER_END
            V vert(A a) { V o; o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;o.color=a.color;o.fog=ComputeFogFactor(o.p.z);return o; }
            half4 frag(V i):SV_Target
            {
                float soft=pow(saturate(1-length(i.uv*2-1)),1.6);
                float flow=.58+.25*sin(i.uv.y*80-_WaterTime*17)+.15*sin(i.uv.y*193-_WaterTime*29);
                float alpha=lerp(flow,soft,_Droplet)*_BaseColor.a;
                return half4(MixFog(_BaseColor.rgb,i.fog),alpha);
            }
            ENDHLSL
        }
    }
}
