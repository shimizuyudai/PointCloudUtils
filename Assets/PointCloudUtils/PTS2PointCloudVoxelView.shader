Shader "Custom/PTS2PointCloudVoxelView" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM

		#pragma surface surf Standard addshadow fullforwardshadows vertex:vert
		#pragma multi_compile_instancing
		#pragma instancing_options procedural:setup

		#pragma target 5.0
		#include "UnityCG.cginc"


		sampler2D _MainTex;
		float _PointSize;
		float3 _Center;
		float4x4 _TRS;
		float _ColorCoefficient;
		struct PointCloudPoint
		{
			float3 position;
			float4 color;
		};
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
		StructuredBuffer<PointCloudPoint> PointCloudPoints;
#endif
		

		struct Input {
			float2 uv_MainTex;
			float4 color;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		struct appdata_custom {
			float4 vertex : POSITION;
			float4 tangent : TANGENT;
			float3 normal : NORMAL;
			float4 texcoord : TEXCOORD0;
			float4 texcoord1 : TEXCOORD1;
			float4 texcoord2 : TEXCOORD2;
			float4 texcoord3 : TEXCOORD3;
			fixed4 color : COLOR;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		void setup()
		{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

#endif
		}

		void vert(inout appdata_custom v, out Input IN) {
			UNITY_INITIALIZE_OUTPUT(Input, IN);
			UNITY_SETUP_INSTANCE_ID(v);
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			uint id = unity_InstanceID;
			float4 pos = float4(PointCloudPoints[id].position - _Center, 1);
			float4 col = float4(PointCloudPoints[id].color);
			col.rgb *= _ColorCoefficient;
			pos = mul(_TRS, pos);


			v.vertex.xyz *= _PointSize;
			v.vertex += pos;
			
			IN.color = col;
#endif
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = IN.color;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
