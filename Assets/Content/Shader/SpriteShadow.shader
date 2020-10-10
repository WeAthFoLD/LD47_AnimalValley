Shader "Custom/SpriteShadow" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		[PerRendererData]_MainTex ("Sprite Texture", 2D) = "white" {}
		_Cutoff("Shadow alpha cutoff", Range(0,1)) = 0.5
	}
	SubShader {
		Tags 
		{ 
			"Queue"="Geometry"
			"RenderType"="TransparentCutout"
		}
		LOD 200

		Cull Off

		CGPROGRAM
		// Lambert lighting model, and enable shadows on all light types
		#pragma surface surf NoLighting addshadow fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		fixed4 _Color;
		fixed _Cutoff;

		struct Input
		{
			float2 uv_MainTex;
		};

inline fixed4 UnityNoLightingLight (SurfaceOutput s, UnityLight light)
{
    // fixed diff = max (0, dot (s.Normal, light.dir));

    fixed4 c;
    c.rgb = s.Albedo * light.color * 0.5;
    c.a = s.Alpha;
    return c;
}

inline fixed4 LightingNoLighting (SurfaceOutput s, UnityGI gi)
{
    fixed4 c;
    c = UnityNoLightingLight (s, gi.light);

    #ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
        c.rgb += s.Albedo * gi.indirect.diffuse;
    #endif

    return c;
}

inline half4 LightingNoLighting_Deferred (SurfaceOutput s, UnityGI gi, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{
    UnityStandardData data;
    data.diffuseColor   = s.Albedo;
    data.occlusion      = 1;
    data.specularColor  = 0;
    data.smoothness     = 0;
    data.normalWorld    = s.Normal;

    UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

    half4 emission = half4(s.Emission, 1);

    #ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
        emission.rgb += s.Albedo * gi.indirect.diffuse;
    #endif

    return emission;
}

inline void LightingNoLighting_GI (
    SurfaceOutput s,
    UnityGIInput data,
    inout UnityGI gi)
{
    gi = UnityGlobalIllumination (data, 1.0, s.Normal);
}

inline fixed4 LightingNoLighting_PrePass (SurfaceOutput s, half4 light)
{
    fixed4 c;
    c.rgb = s.Albedo * light.rgb;
    c.a = s.Alpha;
    return c;
}

		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
			o.Normal = float3(0, 1, 0);
			clip(o.Alpha - _Cutoff);
		}
		ENDCG
	}
	FallBack "Diffuse"
}
