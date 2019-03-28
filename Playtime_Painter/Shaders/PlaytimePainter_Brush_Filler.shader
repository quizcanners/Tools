﻿Shader "Playtime Painter/Editor/Brush/Spread" {

	Category{
		Tags{ "Queue" = "Transparent"}

		ColorMask RGBA
		Cull off
		ZTest off
		ZWrite off

		SubShader{
			Pass{

				CGPROGRAM

				#include "PlaytimePainter_cg.cginc"

				#pragma multi_compile  _FILL_TRANSPARENT  _FILL_NON_INKED 

				#pragma multi_compile  ____ TARGET_TRANSPARENT_LAYER

				#pragma vertex vert
				#pragma fragment frag

				struct v2f {
					float4 pos : POSITION;
					float4 texcoord : TEXCOORD0;
				};


				v2f vert(appdata_full v) {
					v2f o;

					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord = brushTexcoord(v.texcoord.xy, v.vertex);

					return o;
				}


				inline void DistAndInc(inout float2 dni, float2 uv, float3 brushColor) {
					//Distance and ink
					float4 col = tex2Dlod(_DestBuffer, float4(uv, 0, 0));

					#if  TARGET_TRANSPARENT_LAYER
					float2 tmp;
					tmp.x = min(dni.x, length(col.rgb - brushColor.rgb));
					tmp.y = min(dni.y, length(col.rgb));

					dni = tmp * col.a + dni * (1 - col.a);

					#else
					dni.x = min(dni.x, length(col.rgb - brushColor.rgb));
					dni.y = min(dni.y, length(col.rgb));
					#endif

				}

				float4 frag(v2f o) : COLOR{

					float blurAmount = _brushForm.w;

					float a = alphaFromUV(o.texcoord);

					clip(a);

					float mask = getMaskedAlpha(o.texcoord.xy);

					float2 uv = o.texcoord.xy;

					float2 d = _DestBuffer_TexelSize.xy;

					float2 dniDX = 3;

					DistAndInc(dniDX, uv + float2(-3 * d.x, 0), _brushColor.rgb);
					DistAndInc(dniDX, uv + float2(-2 * d.x, 0), _brushColor.rgb);
					DistAndInc(dniDX, uv + float2(-1 * d.x, 0), _brushColor.rgb);
			
					float2 dniX = 3;

					DistAndInc(dniX, uv + float2(3 * d.x, 0), _brushColor.rgb);
					DistAndInc(dniX, uv + float2(2 * d.x, 0), _brushColor.rgb);
					DistAndInc(dniX, uv + float2(1 * d.x, 0), _brushColor.rgb);

					float2 dniDY = 3;

					DistAndInc(dniDY, uv + float2(0, -3 * d.y), _brushColor.rgb);
					DistAndInc(dniDY, uv + float2(0, -2 * d.y), _brushColor.rgb);
					DistAndInc(dniDY, uv + float2(0, -1 * d.y), _brushColor.rgb);

					float2 dniY = 3;

					DistAndInc(dniY, uv + float2(0, 3 * d.y), _brushColor.rgb);
					DistAndInc(dniY, uv + float2(0, 2 * d.y), _brushColor.rgb);
					DistAndInc(dniY, uv + float2(0, 1 * d.y), _brushColor.rgb);

					#define GETDIST(cid) cid.x + max(0, 0.1 - cid.y) * 10

					float dist = min(GETDIST(dniX), GETDIST(dniDX));
					dist = min(dist, GETDIST(dniY));
					dist = min(dist, GETDIST(dniDY));

					float alpha = min(1,saturate((a - 0.95) * 20 + saturate(a * (1 - dist*4)*blurAmount)));

					#if  TARGET_TRANSPARENT_LAYER
						return AlphaBlitTransparent(alpha, _brushColor,  o.texcoord.xy);
					#else
						return AlphaBlitOpaque(alpha, _brushColor,  o.texcoord.xy);
					#endif

				}
				ENDCG
			}
		}
	}
}