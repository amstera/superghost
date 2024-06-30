Shader "Custom/ImprovedSpiralTunnel" {
    Properties {
        _MainColor ("Main Color", Color) = (1,1,1,1)
        _BackgroundColor ("Background Color", Color) = (0,0,0,1)
        _Speed ("Speed", Float) = 1.0
        _Twist ("Twist", Float) = 10.0
        _LineWidth ("Line Width", Range(0, 1)) = 0.5
        _LineCount ("Line Count", Float) = 4.0
    }
    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 100

        CGPROGRAM
        #pragma surface surf Lambert alpha
        #include "UnityCG.cginc"

        fixed4 _MainColor;
        fixed4 _BackgroundColor;
        float _Speed;
        float _Twist;
        float _LineWidth;
        float _LineCount;

        struct Input {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutput o) {
            float2 uv = IN.uv_MainTex - 0.5;
            float r = length(uv);
            float angle = atan2(uv.y, uv.x);
            
            float t = _Time.y * _Speed;
            
            float spiral = frac((angle / (2.0 * UNITY_PI) + r * _Twist + t) * _LineCount);
            
            float lineValue = smoothstep(0.0, _LineWidth, spiral) - smoothstep(_LineWidth, 1.0, spiral);
            
            fixed4 c = lerp(_BackgroundColor, _MainColor, lineValue);
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}