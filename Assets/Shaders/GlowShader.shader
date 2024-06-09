Shader "Custom/IntenseGlowShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 5
        _Opacity ("Opacity", Range(0, 1)) = 1.0
        _WaveFrequency ("Wave Frequency", Range(0, 10)) = 1.0
        _WaveAmplitude ("Wave Amplitude", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _GlowColor;
            float _GlowIntensity;
            float _Opacity;
            float _WaveFrequency;
            float _WaveAmplitude;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 texColor = tex2D(_MainTex, i.uv);
                float wave = sin(i.uv.x * _WaveFrequency + _Time.y * _WaveFrequency) * _WaveAmplitude;
                half4 glowColor = _GlowColor * (_GlowIntensity + wave);

                // Apply opacity to both the texture and the glow
                texColor.a *= _Opacity;
                glowColor.a *= _Opacity;

                half4 result = texColor;
                result.rgb += glowColor.rgb * glowColor.a;
                result.a = texColor.a;

                return result;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Cutout/VertexLit"
}