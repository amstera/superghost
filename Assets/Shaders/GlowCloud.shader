Shader "Custom/GlowCloud"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (0,1,0,1)
        _GlowPower ("Glow Power", Range(0.5, 5.0)) = 2.0
        _GlowSpread ("Glow Spread", Range(0.1, 10.0)) = 1.0
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveAmount ("Wave Amount", Float) = 0.1
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 100

        CGPROGRAM
        #pragma surface surf Lambert alpha:fade

        sampler2D _MainTex;
        fixed4 _GlowColor;
        float _GlowPower;
        float _GlowSpread;
        float _WaveSpeed;
        float _WaveAmount;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = _GlowColor.rgb;
            
            // Create wave effect
            float2 waveUV = IN.uv_MainTex;
            waveUV.x += sin(_Time.y * _WaveSpeed + waveUV.y * 10) * _WaveAmount;
            waveUV.y += cos(_Time.y * _WaveSpeed + waveUV.x * 10) * _WaveAmount;
            
            float2 uvDist = abs(waveUV - 0.5) * 2;
            float dist = length(uvDist);
            float glow = 1 - saturate(dist / _GlowSpread);
            glow = pow(glow, _GlowPower);
            
            o.Alpha = glow * _GlowColor.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}