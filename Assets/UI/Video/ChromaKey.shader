Shader "Unlit/ChromaKey"
{
    Properties
    {
        _MainTex ("Video", 2D) = "white" {}

        // 綠幕顏色
        _KeyColor   ("Key Color", Color) = (0,1,0,1)

        // 距離 KeyColor 多近算綠幕
        _Tolerance  ("Green Tolerance", Range(0,1)) = 0.4

        // 邊緣軟硬程度（越大越柔）
        _EdgeSoftness ("Edge Softness", Range(0,0.3)) = 0.06

        // 去綠溢色強度（只對邊緣／有綠光處用力）
        _Despill ("Despill", Range(0,1)) = 1.0

        // 下面這些只是保留舊欄位，避免材質報錯，不實際使用
        _Similarity ("Similarity", Range(0,1)) = 0.3
        _Smoothness ("Smoothness", Range(0,1)) = 0.1
        _AlphaCutoff ("Alpha Cutoff", Range(0,1)) = 0.02
        _FillColor ("Fill Color", Color) = (0.20,0.55,1.0,1)
        _Shaded ("Use Luma Shading", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _KeyColor;
            float  _Tolerance;
            float  _EdgeSoftness;
            float  _Despill;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 src = tex2D(_MainTex, i.uv);

                // -------------------------
                // 1) 計算跟綠幕顏色的距離
                // -------------------------
                float3 diff = src.rgb - _KeyColor.rgb;
                float  dist = length(diff);   // 越接近 KeyColor → dist 越小

                // -------------------------
                // 2) 用 soft edge 算 alpha
                //    dist <= Tolerance - EdgeSoftness → alpha = 0 (全透明)
                //    dist >= Tolerance + EdgeSoftness → alpha = 1 (全不透明)
                //    中間區域則漸變，當作邊緣
                // -------------------------
                float e = max(_EdgeSoftness, 1e-4);
                float alpha = saturate((dist - (_Tolerance - e)) / (2.0 * e));

                // 完全是綠幕 → 不畫
                if (alpha <= 0.0001)
                {
                    return float4(0,0,0,0);
                }

                // -------------------------
                // 3) 只在「邊緣 + 有明顯綠溢色」的地方做 Despill
                // -------------------------
                float  maxRB      = max(src.r, src.b);
                float  greenExtra = max(src.g - maxRB, 0);   // G 比 R/B 多多少
                float  spillMask  = saturate(greenExtra * 5); // 有綠光才 >0

                // 邊緣越接近透明 (alpha 越小) → despill 越強
                float  edgeFactor = 1.0 - alpha;
                float  despillAmount = _Despill * spillMask * edgeFactor;

                float3 despilled = src.rgb;
                despilled.g = lerp(src.g, maxRB, despillAmount);

                return float4(despilled, alpha);
            }
            ENDCG
        }
    }
}
