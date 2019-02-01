﻿Shader "Playtime Painter/UI/RoundedBoxSingleColor"
{
	Properties{
		[PerRendererData]_MainTex("Albedo (RGB)", 2D) = "black" {}
	}
	Category{
		Tags{
			"Queue" = "Transparent-250"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		ColorMask RGB
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader{
			Pass{

				CGPROGRAM

				#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma target 3.0

				struct v2f {
					float4 pos : SV_POSITION;
					float4 texcoord : TEXCOORD0;
					float4 projPos : TEXCOORD1;
					float4 color: COLOR;
				};

			


				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord.xy = v.texcoord.xy;
					o.color = v.color;


					o.texcoord.zw = v.texcoord1.xy;
					o.texcoord.z = abs(o.texcoord.z);
					o.projPos.xy = v.normal.xy;
					o.projPos.zw = max(0, float2(v.normal.z, -v.normal.z));

					return o;
				}


				float4 frag(v2f i) : COLOR{

					float4 _ProjTexPos = i.projPos;
					float _Edge = i.texcoord.z;
					float _Courners = i.texcoord.w;
					
					float _Blur = (1 - i.color.a);
					float2 uv = abs(i.texcoord.xy - 0.5) * 2;
					uv = max(0, uv - _ProjTexPos.zw) / (1.0001 - _ProjTexPos.zw) - _Courners;
					float deCourners = 1.0001 - _Courners;
					uv = max(0, uv) / deCourners;
					uv *= uv;
					float clipp = max(0, (1 - uv.x - uv.y));

					float uvy = saturate(clipp * (4 - _Courners * 3)*(0.5 + _Edge * 0.5));

					i.color.a *= min(clipp* _Edge * (1 - _Blur)*deCourners * 30, 1);

					return i.color;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}
