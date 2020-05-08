Shader "Custom/SelectionToolHandler"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,0.5)
		_Emission("Emission", Range(0,1)) = 0.5
		_PulseSpeed("Pulse Speed", float) = 1
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_SelectionActive("SelectionActive", Range(0,1)) = 0.0
    }

    CGINCLUDE
        #define UNITY_SETUP_BRDF_INPUT MetallicSetup
    ENDCG

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        Blend SrcAlpha OneMinusSrcAlpha 
        ZWrite Off
        LOD 300
        
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows alpha
        // This line tells Unity to compile this pass for forward add, giving attenuation information for the light.
        
		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input
		{
			float2 uv_MainTex;
			float4 pos : SV_POSITION;
			float3 worldPos : TEXCOORD3;
		};

		half _Glossiness;
		half _Metallic;
		half _Emission;
		fixed4 _Color;
		float _PulseSpeed;
		float _SelectionActive;
        
		void surf (Input IN, inout SurfaceOutputStandard o)
		{
            // magic function i found on the internet
			o.Albedo = _Color; 
			o.Alpha = 0.1;
			
			if (_SelectionActive > 0)
			{
                float time = _Time.z * 2;
                float4 sinTime = _SinTime;
                float3 pos = IN.worldPos * 30;
                float shift = (sin(pos.y + time + sinTime.w)) * 2;// + sin(pos.y + time + sinTime.z)) * 2;// + sin(pos.z + time + sinTime.y));
                shift = (shift + 3) / 6;
                //float2 seed = pos.xy * pos.z;
                //float noise = frac(sin(dot(seed ,float2(12.9898,78.233))) * 43758.5453);
                //hue *= saturate(noise + 0.8);
			    o.Metallic = _Metallic * shift;
			    o.Smoothness = _Glossiness * shift;
			    o.Emission = _Emission * shift/15;
			}
			
			
			
			
		}
    ENDCG
    }
}
