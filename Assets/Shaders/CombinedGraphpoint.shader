// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Shader used by the combined graphpoints.
// The idea is to encode rendering information in the main texture.
// The red channel chooses the gene color of the graphpoint, the values [0-x)
// (x is the number of available colors) chooses a color from the
// _ExpressionColors array. The value 255 is reserved for white.
// The green channel values determines the following;
// g == 0: no outline
// 0   < g <= 0.1: outline
// 0.1 < g <= 0.2: thicker outline
// 0.2 < g <= 0.5: not used
// 0.5 < g <= 0.7 not used
// 0.7 < g <= 0.9 transparancy
// 0.9 < g <= 1  : party
// The blue channel values determines the following;
// b == 0: not used
// 0   < b <= 0.1: culling
// 0.1 < b <= 0.2: not used
// 0.2 < b <= 0.5: not used
// 0.5 < b <= 0.7 not used
// 0.7 < b <= 0.9 not used
// 0.9 < b <= 1  : not used

Shader "Custom/CombinedGraphpoint"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _GraphpointColorTex("Graphpoint Colors", 2D) = "white" {}
        _OutlineThickness("Thickness", float) = 0.005
		_ThickerOutline("ThicknessMultiplier", float) = 4
        _TestPar("test", float) = 0
        _Transparancy("Transparancy", Range(0.0, 1.0)) = 0.1
        _Culling("Culling", float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        Blend SrcAlpha OneMinusSrcAlpha 
        // Cull Off
        ZWrite Off
        LOD 100
        // graphpoint pass forward base
        // draws the graphpoint mesh lit by directional light
        Pass 
        {
            Tags
            {
               "LightMode" = "ForwardBase"
            }
               CGPROGRAM
               #pragma vertex vert 
               #pragma fragment frag 
               #pragma multi_compile_fwdbase                       // This line tells Unity to compile this pass for forward base.
               
               #include "UnityCG.cginc"
               #include "AutoLight.cginc"
              	struct vertex_input
              	{
              		float4 vertex : POSITION;
              		float3 normal : NORMAL;
              		float2 texcoord : TEXCOORD0;
              	};
               
               struct vertex_output
               {
                    float4  pos         : SV_POSITION;
                    float2  uv          : TEXCOORD0;
                    float3  lightDir    : TEXCOORD1;
                    float3  normal		: TEXCOORD2;
                    float3  worldPos     : TEXCOORD3;
                //    LIGHTING_COORDS(3,4)                            // Macro to send shadow & attenuation to the vertex shader.
               };
            //    struct v2f 
            //    {
            //        half4 pos : POSITION;
            //        half2 uv : TEXCOORD0;

            //    };

            //    v2f vert(appdata_img v)
            //    {
            //        v2f o;
            //        o.pos = UnityObjectToClipPos (v.vertex);
            //        half2 uv = MultiplyUV( UNITY_MATRIX_TEXTURE0, v.texcoord );
            //        o.uv = uv;
            //        return o;
			//    }


                sampler2D _MainTex;
                sampler2D _GraphpointColorTex;
                float4 _MainTex_ST;
                fixed4 _LightColor0;
                float _Transparancy;
                float _Cutoff;
                float4 _PlanePos;
                uniform float4x4 _BoxMatrix;
                uniform float4x4 _BoxMatrix2;
                float _Culling;



                vertex_output vert (vertex_input v)
                {
                    vertex_output o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = v.texcoord.xy;
                    o.lightDir = ObjSpaceLightDir(v.vertex);
                    o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                    o.normal = v.normal;
                    
                   // TRANSFER_VERTEX_TO_FRAGMENT(o);                 // Macro to send shadow & attenuation to the fragment shader.
                   
		           // #ifdef VERTEXLIGHT_ON
  				   // float3 worldN = mul((float3x3)unity_ObjectToWorld, SCALED_NORMAL);
		           // float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
		            
		           // for (int index = 0; index < 4; index++)
		           // {    
                   //     float4 lightPosition = float4(unity_4LightPosX0[index], 
                   //         unity_4LightPosY0[index], 
                   //         unity_4LightPosZ0[index], 1.0);
                   
                   //     float3 vertexToLightSource = float3(lightPosition.xyz - worldPos);     
                        
                   //     float3 lightDirection = normalize(vertexToLightSource);
                   
                   //     float squaredDistance = dot(vertexToLightSource, vertexToLightSource);
                   
                   //     // float attenuation = 1.0 / (1.0  + unity_4LightAtten0[index] * squaredDistance);
                   //     float attenuation = 1.0;
                   
                   //     float3 diffuseReflection = attenuation * float3(unity_LightColor[index].xyz) * max(0.0, dot(worldN, lightDirection));
                   
                   //     o.vertexLighting = o.vertexLighting + diffuseReflection * 2;
		           // }
		           // #endif
                   return o;
               }
                float isInsideBox(float4 pos)
                {
                    if (pos.x < -.5 || pos.x > .5)
                        return 1;
                    if (pos.y < -.5 || pos.y > .5)
                        return 1;
                    if (pos.z < -.5 || pos.z > .5)
                        return 1;
                    return -1;
                }

                float clip_fragment(float inside_first, float inside_second, float blue_channel)
                {
                    if ((inside_first <= 0 || inside_second <= 0) && !(blue_channel > 0 && blue_channel < 0.1))
                        return -1;
                    return 1;
                }
                

                fixed4 frag(vertex_output i) : COLOR
                {
                    i.lightDir = normalize(i.lightDir);
                    fixed atten = LIGHT_ATTENUATION(i); // Macro to get you the combined shadow & attenuation value.

                    // float3 expressionColorData = (tex2D(_MainTex, i.uv));
                    float3 expressionColorData = tex2D(_MainTex, i.uv);
                    // the 255.0 / 256.0 is there to shift the x-coordinate slightly to the left, otherwise the rightmost pixel (which in the _MainTex is red = 255) rolls over to the leftmost
                    float2 colorTexUV = float2(expressionColorData.x * 255.0/256.0, 0.5);

                    float4 color = tex2D(_GraphpointColorTex, colorTexUV);
                    //color *= fixed4(i.vertexLighting, 1.0);
                    fixed diff = saturate(dot(i.normal, i.lightDir));

                    float4 wpos = float4(i.worldPos.x, i.worldPos.y, i.worldPos.z, 1);
                    float4 relpos_box1 = mul(_BoxMatrix, wpos);
                    float4 relpos_box2 = mul(_BoxMatrix2, wpos);
                    float do_clip = clip_fragment(isInsideBox(relpos_box1), isInsideBox(relpos_box2), expressionColorData.b);
                    clip(do_clip*_Culling);    
                //    int col = expressionColorData.g;
                   

                //    color.a = 0.5;
                //    if (expressionColorData.r == 254.0/255.0)
                   if (expressionColorData.g > 0.7 && expressionColorData.g < 0.9) //(colorTexUV.x == 254.0/255.0)
                   {
                       color.rgb = (UNITY_LIGHTMODEL_AMBIENT.rgb * 2 * color.rgb);         // Ambient term. Only do this in Forward Base. It only needs calculating once.
                       color.rgb += (color.rgb * _LightColor0.rgb * diff) /** (atten * 2)*/; // Diffuse and specular.
                       color.a = _Transparancy;
                   }
                   else
                   {
                       color.a = 1;
                   }
                    // color.rgb = (UNITY_LIGHTMODEL_AMBIENT.rgb * 2 * color.rgb);         // Ambient term. Only do this in Forward Base. It only needs calculating once.
                   // color.rgb += (color.rgb * _LightColor0.rgb * diff) /** (atten * 2)*/; // Diffuse and specular.
                //    color.a = 1;// color.a + _LightColor0.a * atten;
                   return color;
               }


           ENDCG
        }
 
        // graphpoint pass forward add
        // draw the graphpoint mesh lit by point and spot light
        Pass {
            Tags
            {
                "LightMode" = "ForwardAdd"
            }                       // Again, this pass tag is important otherwise Unity may not give the correct light information.
            // Blend One One   
            CGPROGRAM
                #pragma vertex vert 
                #pragma fragment frag
                #pragma multi_compile_fwdadd                        // This line tells Unity to compile this pass for forward add, giving attenuation information for the light.
                
                #include "UnityCG.cginc"
                #include "AutoLight.cginc"
                
                float4 _PlanePos;
                uniform float4x4 _BoxMatrix;
                uniform float4x4 _BoxMatrix2;
                float _Culling;

                struct v2f
                {
                    float4  pos         : SV_POSITION;
                    float2  uv          : TEXCOORD0;
                    float3  normal		: TEXCOORD1;
                    float3  lightDir    : TEXCOORD2;
                    // LIGHTING_COORDS(3,4)                            // Macro to send shadow & attenuation to the vertex shader.
                    float3  worldPos    : TEXCOORD3;
                };
 
                v2f vert (appdata_tan v)
                {
                    v2f o;
                    
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                    o.uv = v.texcoord.xy;
                   	
					o.lightDir = ObjSpaceLightDir(v.vertex);
					
					o.normal =  v.normal;
                    // TRANSFER_VERTEX_TO_FRAGMENT(o);                 // Macro to send shadow & attenuation to the fragment shader.
                    return o;
                }
 
                sampler2D _MainTex;
                sampler2D _GraphpointColorTex;
    
                // fixed4 _LightColor0; // Colour of the light used in this pass.

                float3 hsv_to_rgb(float3 HSV)
                {
                    float3 RGB = HSV.z;
            
                    float var_h = HSV.x * 6;
                    float var_i = floor(var_h);
                    float var_1 = HSV.z * (1.0 - HSV.y);
                    float var_2 = HSV.z * (1.0 - HSV.y * (var_h-var_i));
                    float var_3 = HSV.z * (1.0 - HSV.y * (1-(var_h-var_i)));
                    if      (var_i == 0) { RGB = float3(HSV.z, var_3, var_1); }
                    else if (var_i == 1) { RGB = float3(var_2, HSV.z, var_1); }
                    else if (var_i == 2) { RGB = float3(var_1, HSV.z, var_3); }
                    else if (var_i == 3) { RGB = float3(var_1, var_2, HSV.z); }
                    else if (var_i == 4) { RGB = float3(var_3, var_1, HSV.z); }
                    else                 { RGB = float3(HSV.z, var_1, var_2); }
                  
                    return (RGB);
                }

                float isInsideBox(float4 pos)
                {
                    if (pos.x < -.5 || pos.x > .5)
                        return 1;
                    if (pos.y < -.5 || pos.y > .5)
                        return 1;
                    if (pos.z < -.5 || pos.z > .5)
                        return 1;
                    return -1;
                }
                float clip_fragment(float inside_first, float inside_second, float blue_channel)
                {
                    if ((inside_first <= 0 || inside_second <= 0 ) && !(blue_channel > 0 && blue_channel < 0.1))
                        return -1;
                    return 1;
                    
                }
                fixed4 frag(v2f i) : COLOR
                {
                    
                    float3 expressionColorData = tex2D(_MainTex, i.uv);
                    float4 wpos = float4(i.worldPos.x, i.worldPos.y, i.worldPos.z, 1);
                    float4 relpos_box1 = mul(_BoxMatrix, wpos);
                    float4 relpos_box2 = mul(_BoxMatrix2, wpos);
                    float do_clip = clip_fragment(isInsideBox(relpos_box1), isInsideBox(relpos_box2), expressionColorData.b);
                    clip(do_clip*_Culling);          
                    
                    if (expressionColorData.g > 0.9) {
                        // party
                        float time = _Time.z * 2;
                        float4 sinTime = _SinTime;
                        float3 pos = i.worldPos * 30;
                        float2 seed = pos.xy * pos.z;
                        // magic function i found on the internet
                        float noise = frac(sin(dot(seed ,float2(12.9898,78.233))) * 43758.5453);
                        float hue = (sin(pos.x + time + sinTime.w) + sin(pos.y + time + sinTime.z) + sin(pos.z + time + sinTime.y));
                        // hue = (hue + 6) / 12;
                        hue = (hue + 3) / 6;
                        hue *= saturate(noise + 0.5);
                        return float4(hsv_to_rgb(float3(hue, 1.0, 1.0)).rgb, 1.0);
                    } else {
                        // normal
                        i.lightDir = normalize(i.lightDir);
						// float2 colorTexUV = float2((round((expressionColorData.x) * 256))/ 256, 0.5);
                        float2 colorTexUV = float2(expressionColorData.r * 255.0/256.0, 0.5);
						float4 color = tex2D(_GraphpointColorTex, colorTexUV);
                        fixed3 normal = i.normal;                    
                        fixed diff = saturate(dot(normal, i.lightDir));
                        
                        fixed4 c;
                        c.rgb = (color.rgb * diff); // Diffuse and specular.
                        // c.a = 1;
                        c.a = 0.1;
                        return c;
                    }
                }
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

        // outline pass
        // original outline shader code taken from VRTK_OutlineBasic.shader
        Pass {

            // Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On // On (default) = Ignore lights etc. Should this be a property?
            Stencil
            {
                Ref 0
                Comp Equal
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _OutlineThickness;
            float _ThickerOutline;
            float _MovingOutlineOuterRadius;
            float _MovingOutlineInnerRadius;
            sampler2D_float _MainTex;
            sampler2D _GraphpointColorTex;
            float _TestPar;
            float4 _PlanePos;
            uniform float4x4 _BoxMatrix;
            float _Culling;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD;
            };

            struct v2g
            {
                float4 pos : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float4 radius : TEXCOORD2;
                float3 color : COLOR;
                //float3 worldPos : TEXCOORD5;
            };

            v2g vert(in appdata_base IN)
            {
                v2g OUT;
                OUT.pos = UnityObjectToClipPos(IN.vertex + normalize(IN.normal) * _TestPar);
                float4 uvAndMip = float4(IN.texcoord.x, IN.texcoord.y, 0, 0);
                OUT.color = tex2Dlod(_MainTex, uvAndMip);
                OUT.texcoord = IN.texcoord;
                OUT.radius = float4(0,0,0,0);
                OUT.viewDir = ObjSpaceViewDir(IN.vertex);
                return OUT;
            }

            // creates an outline around a cell
            void outline(v2g start, v2g end, inout TriangleStream<v2g> triStream, bool thickerOutline)
            {
				float thicknessFactor = 1;
				if (thickerOutline)
				{
					thicknessFactor = _ThickerOutline;
				}
                float width = _OutlineThickness * thicknessFactor; // / 100;
                float4 parallel = (end.pos - start.pos) * width;
                float4 perpendicular = normalize(float4(parallel.y, -parallel.x, 0, 0)) * width;
                float4 v1 = start.pos - parallel;
                float4 v2 = end.pos + parallel;
                v2g OUT;
                float3 expressionColorData = start.color;
                float4 uvAndMip = float4(expressionColorData.x + 1/512, 0.5, 0, 0);
                float3 color = tex2Dlod(_GraphpointColorTex, uvAndMip);
                OUT.color = (float3(1, 1, 1) - (color)) / 4 + color;
                OUT.viewDir = start.viewDir;
                OUT.texcoord = start.texcoord;
                OUT.radius = float4(0,0,0,0);
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
                float3 color = IN[0].color;
                // green channel values determines the following;
                // g == 0: no outline
                // 0   < g <= 0.1: outline
                // 0.1 < g <= 0.2: thicker outline
                // 0.2 < g <= 0.9: not used
                // 0.9 < g <= 1  : party
                if (color.g == 0)
                {
                    return;
                }
                else if (color.g <= 0.1)
                {
                    outline(IN[0], IN[1], triStream, false);
                    outline(IN[1], IN[2], triStream, false);
                    outline(IN[2], IN[0], triStream, false);
                }
				else if (color.g <= 0.2)
				{
					outline(IN[0], IN[1], triStream, true);
					outline(IN[1], IN[2], triStream, true);
					outline(IN[2], IN[0], triStream, true);
				}

            }

            fixed4 frag(v2g i) : COLOR
            {
                return fixed4(i.color, 1);
            }

            ENDCG
        }
    }
    Fallback "Diffuse"
}
