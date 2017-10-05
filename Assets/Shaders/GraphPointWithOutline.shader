﻿// A very simple shader that adds an outline to graphpoints.
// Outline code taken from VRTK_OutlineBasic.shader

Shader "Custom/GraphPointWithOutline" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" { }
        _Color ("Main Color", Color) = (0.5, 0.5, 0.5, 1)
        _OutlineColor("Outline Color", Color) = (1, 0, 0, 1)
        _Thickness("Thickness", float) = 0.0005
    }
    
    SubShader {
        Tags {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
            "IgnoreProjector" = "True"
            "ForceNoShadowCasting" = "True"
        }

		// Draw the actual graphpoint
        Pass {
        Tags { "LightMode" = "ForwardBase" }
            //Blend One One
            //Fog { Color (0,0,0,0) }
            ZWrite On
            ZTest LEqual
            CGPROGRAM
                #pragma target 3.0
                #pragma vertex vertBase
                #pragma fragment fragBase
                #include "UnityStandardCoreForward.cginc"
            ENDCG
        }

        Pass {
        Tags { "LightMode" = "ForwardAdd" }
            Blend One One
            Fog { Color (0,0,0,0) }
            ZWrite On
            ZTest LEqual
            CGPROGRAM
                #pragma target 3.0
                #pragma vertex vertAdd
                #pragma fragment fragAdd
                #include "UnityStandardCoreForward.cginc"
            ENDCG
        }

        // Fill the stencil buffer
        Pass {
            Stencil {
                Ref 1
                Comp Always
                Pass Replace
                ZFail Replace
            }
            ColorMask 0
        }
        // Draw the outline
        Pass {
            //Name "GRAPHPOINT_OUTLINE"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On // On (default) = Ignore lights etc. Should this be a property?
            Stencil
            {
                Ref 0
                Comp Equal
            }
            
            CGPROGRAM
                //#pragma multi_compile GRAPHPOINT_OUTLINE
                //#ifdef GRAPHPOINT_OUTLINE
                #pragma vertex vert
                #pragma geometry geom
                #pragma fragment frag
                #include "UnityCG.cginc"
                //#endif

                half4 _OutlineColor;
                float _Thickness;
                
                struct appdata
                {
                    float4 vertex : POSITION;
                };
                
                struct v2g
                {
                    float4 pos : SV_POSITION;
                };
                
                v2g vert(appdata IN)
                {
                    v2g OUT;
                    OUT.pos = UnityObjectToClipPos(IN.vertex);
                    return OUT;
                }
                
                void geom2(v2g start, v2g end, inout TriangleStream<v2g> triStream)
                {
                    float width = _Thickness;// / 100;
                    float4 parallel = (end.pos - start.pos) * width;
                    float4 perpendicular = normalize(float4(parallel.y, -parallel.x, 0, 0)) * width;
                    float4 v1 = start.pos - parallel;
                    float4 v2 = end.pos + parallel;
                    v2g OUT;
                    OUT.pos = v1 - perpendicular;
                    triStream.Append(OUT);
                    OUT.pos = v1 + perpendicular;
                    triStream.Append(OUT);
                    OUT.pos = v2 - perpendicular;
                    triStream.Append(OUT);
                    OUT.pos = v2 + perpendicular;
                    triStream.Append(OUT);
                }
                
                [maxvertexcount(8)]
                void geom(triangle v2g IN[3], inout TriangleStream<v2g> triStream)
                {
                    //#ifdef GRAPHPOINT_OUTLINE
                        geom2(IN[0], IN[1], triStream);
                        geom2(IN[1], IN[2], triStream);
                        geom2(IN[2], IN[0], triStream);
                    //#endif
                }
                
                half4 frag(v2g IN) : COLOR
                {
                    _OutlineColor.a = 1;
                    return _OutlineColor;
                }
                //#endif
            ENDCG
        }
    }
    Fallback "Diffuse"
}
