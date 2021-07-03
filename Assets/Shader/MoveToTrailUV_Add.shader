/*
Trail 컴포넌트가 Tiling 으로 설정되어야함. (그래야 스크롤을 위한 UV가 Trail 세그먼트 크기와 상관 없이 일정하게 나옴)
*/

Shader "MoveToTrailUV/MoveToTrailUV_Add"
{
	Properties
	{
		_MainTex("Main Texture (RGB)", 2D) = "white" {}
		_MainTexVFade("MainTex V Fade", Range(0, 1)) = 0
		_MainTexVFadePow("MainTex V Fade Pow", Float) = 1
		_MainTexPow("Main Texture Gamma", Float) = 1
		_MainTexMultiplier("Main Texture Multiplier", Float) = 1
		_TintTex("Tint Texture (RGB)", 2D) = "white" {}
		_Multiplier("Multiplier", Float) = 1
		_MainScrollSpeedU("Main Scroll U Speed", Float) = 10
		_MainScrollSpeedV("Main Scroll V Speed", Float) = 0
	}
		SubShader
		{
			Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent"}
			Blend One One // Additive
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
					float2 uv : TEXCOORD0;
					float2 uvOrigin : TEXCOORD1; // 원래 UV
					float4 positionHCS : SV_POSITION;
					half4 color : COLOR;
				};

				sampler2D _MainTex;
				sampler2D _TintTex;

				CBUFFER_START(UnityPerMaterial)
					half4 _MainTex_ST;
					half _MainTexVFade;
					half _MainTexVFadePow;
					half _MainTexPow;
					half _MainTexMultiplier;
					half _Multiplier;
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
					o.uvOrigin = IN.uv;
					o.color = IN.color;
					return o;
				}

				half4 frag(Varyings IN) : SV_Target
				{
					half4 mainTex = tex2D(_MainTex, IN.uv);

					// 메인 텍스쳐 가공
					half vFade = 1 - abs(IN.uvOrigin.y - 0.5) * 2; // 세로 uv 기준으로 A 그래프 생성
					vFade = pow(abs(vFade), _MainTexVFadePow); // A 가운데를 좀더 뾰족하게 혹은 둥글게
					vFade = lerp(1, vFade, _MainTexVFade);
					mainTex.rgb *= vFade; // 일단 텍스쳐에 세로 페이드아웃부터 적용
					mainTex.rgb = pow(abs(mainTex.rgb), _MainTexPow) * _MainTexMultiplier; // 메인 텍스쳐 1차 가공
					
					// 버택스 알파와 _Multiplier로 이원화된 값을 하나로 통일
					half intensity = _Multiplier * IN.color.a;

					// Tint
					half avr = mainTex.r * 0.3333 + mainTex.g * 0.3334 + mainTex.b * 0.3333;
					avr = saturate(avr * intensity); // intensity 1이 넘는 영역은 일단 1로 샘플링
					half4 col = tex2D(_TintTex, half2(avr, 0.5));

					half intensityHigh = max(1, intensity); // 1보다 작으면 1로 되고 1보다 크면 intensity 값을 사용 (1보다 큰 값은 HDR로 뻥튀기)
					col.rgb *= intensityHigh * IN.color.rgb;
					return col;
				}
				ENDHLSL
			}
		}
}
