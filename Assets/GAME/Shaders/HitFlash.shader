Shader "Custom/HitFlash"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0, 0, 1)
        _Alpha ("Alpha Multiplier", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Overlay" 
            "RenderType"="Transparent" 
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True" 
        }
        LOD 100
        
        ZWrite Off
        ZTest Always // Ensure it renders over everything
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off // Double face render

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Alpha;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 col = _Color;
                col.a *= _Alpha;
                return col;
            }
            ENDHLSL
        }
    }
}
