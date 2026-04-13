Shader "Custom/LaserGlow"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color", Color) = (2.0, 2.0, 2.0, 1.0)
        [HDR] _MidColor ("Mid Color", Color) = (2.0, 0.4, 0.0, 0.6)
        [HDR] _EdgeColor ("Edge Color", Color) = (1.0, 0.0, 0.0, 0.0)
        _RimPower ("Rim Power", Range(0.1, 5.0)) = 1.0
    }
    SubShader
    {
        // Transparent Queue, don't write to Z-buffer
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            float4 _CoreColor;
            float4 _MidColor;
            float4 _EdgeColor;
            float _RimPower;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // UnityObjectToWorldNormal handles non-uniform scaling correctly
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = _WorldSpaceCameraPos - i.worldPos;
                
                // The native Unity cylinder primitive is aligned along the local Y axis.
                // We extract the local Y axis direction in world space.
                float3 L = normalize(mul((float3x3)unity_ObjectToWorld, float3(0, 1, 0)));
                
                // Project the camera's view direction onto the 2D plane that slices radially through the cylinder.
                // This eliminates the "far end" fading out problem!
                float3 vProj = viewDir - (dot(viewDir, L) * L);
                
                // To avoid division by zero if looking perfectly down the barrel
                if (length(vProj) < 0.001)
                    vProj = normal; // Fallback so the math doesn't explode
                    
                float3 vRadial = normalize(vProj);
                
                // Now calculate our dot product using the purely radial view vector
                float NdotV = saturate(dot(normal, vRadial));
                float rim = saturate(1.0 - NdotV);
                rim = pow(rim, _RimPower);
                
                // Interpolate colors based on rim
                float4 finalColor;
                if (rim < 0.5)
                {
                    // Inner half: Blend between Core (White) and Mid (Orange)
                    float t = rim / 0.5;
                    finalColor = lerp(_CoreColor, _MidColor, t);
                }
                else
                {
                    // Outer half: Blend between Mid (Orange) and Edge (Red/Transparent)
                    float t = (rim - 0.5) / 0.5;
                    finalColor = lerp(_MidColor, _EdgeColor, t);
                }

                return finalColor;
            }
            ENDCG
        }
    }
}
