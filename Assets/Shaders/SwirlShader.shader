Shader "Custom/SwirlingBackground"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.8, 0.1, 0.1, 1)
        _HighlightColor ("Highlight Color", Color) = (0.1, 0.4, 0.8, 1)
        _SwirlSpeed ("Swirl Speed", Float) = 2
        _SwirlScale ("Swirl Scale", Float) = 0.4
        _NoiseScale ("Noise Scale", Float) = 6.0
    }
    SubShader
    {
        Tags {"Queue"="Background" "RenderType"="Opaque"}
        LOD 100

        CGPROGRAM
        #pragma surface surf Lambert

        fixed4 _BaseColor;
        fixed4 _HighlightColor;
        float _SwirlSpeed;
        float _SwirlScale;
        float _NoiseScale;

        struct Input
        {
            float2 uv_MainTex;
        };

        float2 hash2(float2 p)
        {
            p = float2(dot(p, float2(127.1, 311.7)),
                       dot(p, float2(269.5, 183.3)));
            return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
        }

        float noise(float2 p)
        {
            const float K1 = 0.366025404;
            const float K2 = 0.211324865;

            float2 i = floor(p + (p.x + p.y) * K1);
            float2 a = p - i + (i.x + i.y) * K2;
            float2 o = (a.x > a.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
            float2 b = a - o + K2;
            float2 c = a - 1.0 + 2.0 * K2;

            float3 h = max(0.5 - float3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
            float3 n = h * h * h * h * float3(dot(a, hash2(i + 0.0)), dot(b, hash2(i + o)), dot(c, hash2(i + 1.0)));

            return dot(n, float3(70.0, 70.0, 70.0));
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            float2 uv = IN.uv_MainTex * _SwirlScale;
            float2 p = uv;
            float t = _Time.y * _SwirlSpeed;
            
            for (int i = 1; i < 5; i++)
            {
                p += float2(noise(uv - float2(t * 0.1, 0)), noise(uv + float2(0, t * 0.1))) * 0.5;
                uv *= 1.9;
            }
            
            float n = noise(p * _NoiseScale);
            fixed4 c = lerp(_BaseColor, _HighlightColor, n * 0.5 + 0.5);
            
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}