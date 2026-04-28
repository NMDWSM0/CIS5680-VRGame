Shader "Custom/ThreatRing"
{
    Properties
    {
        _RingColor ("Ring Color", Color) = (0.5, 0.5, 0.5, 1)
        _ThreatColor ("Threat Color", Color) = (1, 0, 0, 1)
        _PlayerColor ("Player Color", Color) = (1, 1, 0, 1)
        _InnerRadius ("Inner Radius", Range(0, 0.5)) = 0.3
        _OuterRadius ("Outer Radius", Range(0, 0.5)) = 0.45
        _Spread ("Threat Spread", Range(0.01, 1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _RingColor;
            float4 _ThreatColor;
            float4 _PlayerColor;
            float _InnerRadius;
            float _OuterRadius;
            float _Spread;

            int _EnemyCount;
            float4 _EnemyDirs[10]; 
            float4 _PlayerDir;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 centeredUV = i.uv - 0.5;
                float dist = length(centeredUV);

                float ringMask = smoothstep(_InnerRadius - 0.02, _InnerRadius, dist) 
                               - smoothstep(_OuterRadius, _OuterRadius + 0.02, dist);

                if (ringMask <= 0) return fixed4(0, 0, 0, 0);

                float2 uvDir = normalize(centeredUV);
                float totalThreat = 0;
                float inFront = 0;
                float2 playerDir2D = normalize(_PlayerDir.xy);

                float maxDot = -2.0;
                float2 closestEnemyDir = float2(0, 1);

                for(int j = 0; j < _EnemyCount; j++)
                {
                    float eDotP = dot(_EnemyDirs[j].xy, playerDir2D);
                    if (eDotP > maxDot)
                    {
                        maxDot = eDotP;
                        closestEnemyDir = _EnemyDirs[j].xy;
                    }

                    float dotProd = dot(uvDir, _EnemyDirs[j].xy);
                    float intensity = smoothstep(1.0 - _Spread, 1.0, dotProd);
                    totalThreat += intensity;

                    // cos(60 deg) = 0.5. Dot > 0.5 means angle is within [-60, +60] degrees (total 120 deg range)
                    inFront = smoothstep(0.5, 0.7, eDotP);
                }

                totalThreat = saturate(totalThreat);

                fixed4 col = lerp(_RingColor, _ThreatColor, totalThreat);

                if (inFront < 1.0f)
                {
                    if (_EnemyCount > 0)
                    {
                        // Tangential arrow at playerDir2D pointing left/right towards closest enemy
                        float2 rightDir = float2(-playerDir2D.y, playerDir2D.x);
                        float enemySign = sign(dot(closestEnemyDir, rightDir));
                        if (enemySign == 0.0) enemySign = 1.0;

                        float angleDiff = acos(clamp(dot(uvDir, playerDir2D), -1.0, 1.0));
                        float pixelSign = sign(dot(uvDir, rightDir));
                        
                        float tangential = angleDiff * pixelSign * dist;
                        float localY = -tangential * enemySign; // Flipped direction to point towards enemy
                        float middleRadius = (_InnerRadius + _OuterRadius) * 0.5;
                        float localX = dist - middleRadius;

                        // Shape function for tangential arrow (1.0 for flatter angle)
                        float shape = localY - abs(localX) * 1.0; 
                        
                        // Wider stripes: lower frequency (15.0) and lower step threshold (0.4)
                        // + _Time.y ensures it animates in the direction it points
                        float band = frac(shape * 15.0 + _Time.y * 2.0);
                        float cMask = step(0.4, band);

                        float playerDot = dot(uvDir, playerDir2D);
                        float angleMask = smoothstep(1.0 - _Spread, 1.0, playerDot);
                        float chevronMask = cMask * angleMask;

                        chevronMask *= (1.0f - inFront);

                        if (chevronMask > 0.0)
                        {
                            col = lerp(col, _PlayerColor, chevronMask);
                        }
                    }
                }

                col.a *= ringMask;

                return col;
            }
            ENDCG
        }
    }
}
