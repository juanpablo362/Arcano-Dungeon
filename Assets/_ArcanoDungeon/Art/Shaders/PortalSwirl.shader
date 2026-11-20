Shader "Arcano/PortalSwirl"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.55, 0.95, 1, 1)
        _Spin ("Spin", Float) = 0.6
        _Power ("Power", Float) = 2.2
        _Mode ("Mode", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Spin;
                float _Power;
                float _Mode;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float mask = 1;
                if (_Mode < 0.5)
                {
                    float2 p = uv - 0.5;
                    float dist = length(p);
                    float s = sin(_Time.y * _Spin);
                    float c = cos(_Time.y * _Spin);
                    float2 q = float2(c * p.x - s * p.y, s * p.x + c * p.y);
                    uv = q + 0.5;
                    float disc = 1 - smoothstep(0.45, 0.5, dist);
                    float rim = smoothstep(0.30, 0.47, dist) * disc;
                    mask = disc * (0.35 + rim);
                }
                else
                {
                    uv.x += _Time.y * _Spin;
                }

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half3 color = tex.rgb * _Color.rgb * _Power;
                return half4(color, saturate(tex.a * mask * _Color.a));
            }
            ENDHLSL
        }
    }
}
