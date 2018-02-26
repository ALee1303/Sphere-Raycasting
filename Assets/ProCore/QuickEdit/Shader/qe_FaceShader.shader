Shader "Hidden/QuickEdit/FaceShader"
{
	Properties {}

	SubShader
	{
		Tags { "IgnoreProjector"="True" "RenderType"="Geometry" }
		Lighting Off
		ZTest LEqual
		ZWrite On
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			AlphaTest Greater .25

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 col : COLOR;
			};

			v2f vert (appdata v)
			{
				v2f o;

				o.pos = float4(UnityObjectToViewPos(v.vertex.xyz).xyz, 1);
				o.pos.xyz *= .99;
				o.pos = mul(UNITY_MATRIX_P, o.pos);

				o.col = v.color;

				return o;
			}

			half4 frag (v2f i) : COLOR
			{
				return i.col;
			}

			ENDCG
		}
	}
}
