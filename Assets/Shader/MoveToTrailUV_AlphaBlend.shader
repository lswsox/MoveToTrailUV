/*
Trail 컴포넌트가 Tiling 으로 설정되어야함. (그래야 스크롤을 위한 UV가 Trail 세그먼트 크기와 상관 없이 일정하게 나옴)
*/

Shader "MoveToTrailUV/MoveToTrailUV_AlphaBLend"
{
	Properties
	{
		_MainTex("Main Texture (RGBA)", 2D) = "white" {}
		_MainTexAPow("MainTex AlphaGamma", Float) = 1
		_SoftAlpha("Soft Alpha", Range(0, 1)) = 1
		_TintTex("Tint Texture (RGB)", 2D) = "white" {}
		_MainScrollSpeedU("Main Scroll U Speed", Float) = 10
		_MainScrollSpeedV("Main Scroll V Speed", Float) = 0
	}
		SubShader
		{
			Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent"}
			Blend SrcAlpha OneMinusSrcAlpha // Traditional transparency
			ZWrite Off

			Pass
			{
				HLSLPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

				struct Attributes
				{
					float4 positionOS : POSITION;
					float2 uv : TEXCOORD0;
					half4 color : COLOR;
				};

				struct Varyings
				{
					float4 positionHCS : SV_POSITION;
					float2 uv : TEXCOORD0;
					half4 color : COLOR;
				};

				sampler2D _MainTex;
				sampler2D _TintTex;

				CBUFFER_START(UnityPerMaterial)
					half4 _MainTex_ST;
					half _MainTexAPow;
					half _SoftAlpha;
					half _MainScrollSpeedU;
					half _MainScrollSpeedV;
				
					// MoveToMaterialUV 스크립트에서 전달받는 UV 스크롤 값.
					// 프로퍼티에는 일부러 넣지 않음. 프로퍼티에 넣을 경우 에디터에서 미리보기로 전달되는 값들이 계속 재질 버전 변경으로 인식되어서 프로퍼티 없이 작동하는 방식으로 제작
					half _MoveToMaterialUV;
				CBUFFER_END


				Varyings vert(Attributes IN)
				{
					Varyings o;
					o.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
					o.uv = TRANSFORM_TEX(IN.uv, _MainTex);
					o.uv.x -= frac(_Time.x * _MainScrollSpeedU) + _MoveToMaterialUV;
					o.uv.y -= frac(_Time.x * _MainScrollSpeedV);
					o.color = IN.color;
					return o;
				}

				half4 frag(Varyings IN) : SV_Target
				{
					half4 mainTex = tex2D(_MainTex, IN.uv);

					// 메인 텍스쳐 가공
					mainTex.a = pow(abs(mainTex.a), _MainTexAPow);
					half toonAlpha = saturate((mainTex.a - (1 - IN.color.a)) / _SoftAlpha);
					half alpha = mainTex.a * IN.color.a;
					half alphaMix = lerp(alpha, toonAlpha, IN.color.a);

					half4 tintCol = tex2D(_TintTex, half2(alphaMix, 0.5));
					
					half4 col;
					col.rgb = lerp(tintCol.rgb * mainTex.rgb, tintCol.rgb, IN.color.a);
					col.rgb *= IN.color.rgb;
					col.a = alphaMix;

					return col;
				}
				ENDHLSL
			}
		}
}
