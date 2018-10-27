Shader "Custom/VTFPointCloudView"{
	SubShader{
		ZWrite On
		Tags{ "RenderType" = "Opaque" "Queue" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 100 Cull Off
		Pass{
		CGPROGRAM

	#pragma target 5.0

	#pragma vertex vert
	#pragma geometry geom
	#pragma fragment frag

	#include "UnityCG.cginc"

	sampler2D _MainTex;
	Texture2D<float4> _ColorTex;
	Texture2D<float4> _PositionTex;
	float4 _ColorTex_TexelSize;
	float4 _PositionTex_TexelSize;

	float _PointSize;
	float3 _Center;
	float4x4 _TRS;


	struct varing {
		float4 pos : SV_POSITION;
		float2 tex : TEXCOORD0;
		float4 col : COLOR;
	};

	varing vert(uint id : SV_VertexID)
	{
		varing output;
		float2 uv = float2((id%_ColorTex_TexelSize.z), (id / _ColorTex_TexelSize.z));
		float4 pos = float4(_PositionTex[uv].rgb - _Center, 1);
		float4 col = float4(_ColorTex[uv].rgb,1);
		pos = mul(_TRS, pos);
		output.pos = pos;
		output.col = col;
		output.tex = float2(0, 0);
		return output;
	}

	[maxvertexcount(4)]
	void geom(point varing input[1], inout TriangleStream<varing> outStream)
	{
		varing output;

		float4 pos = input[0].pos;
		float4 col = input[0].col;

		for (int x = 0; x < 2; x++)
		{
			for (int y = 0; y < 2; y++)
			{
				float4x4 billboardMatrix = UNITY_MATRIX_V;
				billboardMatrix._m03 =
					billboardMatrix._m13 =
					billboardMatrix._m23 =
					billboardMatrix._m33 = 0;

				float2 tex = float2(x, y);
				output.tex = tex;

				output.pos = pos + mul(float4((tex - float2(0.5,0.5)) * _PointSize, 0, 1), billboardMatrix);
				output.pos = mul(UNITY_MATRIX_VP, output.pos);

				output.col = col;
				outStream.Append(output);
			}
		}
		outStream.RestartStrip();
	}

	fixed4 frag(varing i) : COLOR
	{
		float4 col = tex2D(_MainTex, i.tex)*i.col;
		if (col.a < 0.5) discard;
		return col;
	}

	ENDCG
	}
	}
}