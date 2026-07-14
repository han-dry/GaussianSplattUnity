// SPDX-License-Identifier: MIT
Shader "Gaussian Splatting/Render Splats"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            ZWrite Off
            Blend OneMinusDstAlpha One
            Cull Off
            
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma require compute
#pragma use_dxc

#include "GaussianSplatting.hlsl"

StructuredBuffer<uint> _OrderBuffer;

struct v2f
{
    half4 col : COLOR0;
    float2 pos : TEXCOORD0;
    float4 vertex : SV_POSITION;
};

StructuredBuffer<SplatViewData> _SplatViewData;
ByteAddressBuffer _SplatSelectedBits;
uint _SplatBitsValid;

// Wind Paramters
float _TimeGlobal;
float _WindSpeed;
float _WindIntensity;
float4 _WindDirection;
float _WindBending;
float _WindEdgeCutoff;
float _WindTurbulence;
float _WindActive; // 0 = spento, 1 = acceso

/*
ORIGINAL VERTEX FUNCTION
v2f vert (uint vtxID : SV_VertexID, uint instID : SV_InstanceID)
{
    v2f o = (v2f)0;
    instID = _OrderBuffer[instID];
	SplatViewData view = _SplatViewData[instID];
	float4 centerClipPos = view.pos;
	bool behindCam = centerClipPos.w <= 0;
	if (behindCam)
	{
		o.vertex = asfloat(0x7fc00000); // NaN discards the primitive
	}
	else
	{
		o.col.r = f16tof32(view.color.x >> 16);
		o.col.g = f16tof32(view.color.x);
		o.col.b = f16tof32(view.color.y >> 16);
		o.col.a = f16tof32(view.color.y);

		uint idx = vtxID;
		float2 quadPos = float2(idx&1, (idx>>1)&1) * 2.0 - 1.0;
		quadPos *= 2;

		o.pos = quadPos;

		float2 deltaScreenPos = (quadPos.x * view.axis1 + quadPos.y * view.axis2) * 2 / _ScreenParams.xy;
		o.vertex = centerClipPos;
		o.vertex.xy += deltaScreenPos * centerClipPos.w;

		// is this splat selected?
		if (_SplatBitsValid)
		{
			uint wordIdx = instID / 32;
			uint bitIdx = instID & 31;
			uint selVal = _SplatSelectedBits.Load(wordIdx * 4);
			if (selVal & (1 << bitIdx))
			{
				o.col.a = -1;				
			}
		}
	}
	FlipProjectionIfBackbuffer(o.vertex);
    return o;
}
*/

// VERTEX FUNCTION WITH WIND EFFECT
v2f vert (uint vtxID : SV_VertexID, uint instID : SV_InstanceID)
{
    v2f o = (v2f)0;
    instID = _OrderBuffer[instID];
    SplatViewData view = _SplatViewData[instID];
    float4 centerClipPos = view.pos;
    bool behindCam = centerClipPos.w <= 0;
    if (behindCam)
    {
        o.vertex = asfloat(0x7fc00000); // NaN discards the primitive
    }
    else
    {
        // 1. CODICE ORIGINALE PER I COLORI
        o.col.r = f16tof32(view.color.x >> 16);
        o.col.g = f16tof32(view.color.x);
        o.col.b = f16tof32(view.color.y >> 16);
        o.col.a = f16tof32(view.color.y);

        // 2. LOGICA VENTO E FILTRO ERBA
        float isGreen = saturate(o.col.g - max(o.col.r, o.col.b));
        
        float4 clipWindOffset = float4(0, 0, 0, 0);
        float2x2 rotationMatrix = float2x2(1, 0, 0, 1); // Matrice d'identità di default (nessuna rotazione)
        
        // Il blocco si attiva SOLO se lo script imposta _WindActive a 1.0
        if (_WindActive > 0.5 && isGreen > _WindEdgeCutoff)
        {
            float phaseShift = instID * 0.05;
            
            // Onda principale + micro-turbolenza ad alta frequenza per un effetto fogliame realistico
            float baseWave = sin(_TimeGlobal * _WindSpeed + phaseShift);
            float turbulence = sin(_TimeGlobal * _WindTurbulence + phaseShift * 2.0) * 0.2;
            float finalWave = baseWave + turbulence;
            
            // Spostamento 3D del centro (già configurato)
            float3 viewOffset = _WindDirection.xyz * (finalWave * _WindIntensity * isGreen);
            clipWindOffset.xy = viewOffset.xy;
            clipWindOffset.z = viewOffset.z * UNITY_MATRIX_P._m22;

            // --- NUOVA LOGICA: ROTAZIONE ATTORNO AL CENTRO (BENDING) ---
            // Calcoliamo l'angolo di piegamento in base alla forza del vento e all'intensità del verde
            // Più lo splat è "foglia/erba", più l'angolo sarà accentuato
            // Curvatura dello splat scalata tramite il parametro _WindBending dell'Inspector
            float angle = finalWave * _WindIntensity * isGreen * _WindDirection.x * _WindBending;

            // Costruiamo la matrice di rotazione 2D per lo schermo
            float cosAngle = cos(angle);
            float sinAngle = sin(angle);
            rotationMatrix = float2x2(cosAngle, -sinAngle, sinAngle, cosAngle);
        }

        // Applichiamo lo spostamento al centro dello splat
        // Se _WindActive è 0, clipWindOffset rimarrà (0,0,0,0) e rotationMatrix sarà l'identità
        centerClipPos += clipWindOffset;

        // 3. GENERAZIONE GEOMETRICA E APPLICAZIONE DELLA ROTAZIONE
        uint idx = vtxID;
        float2 quadPos = float2(idx&1, (idx>>1)&1) * 2.0 - 1.0;
        quadPos *= 2;

        o.pos = quadPos;

        // Ruotiamo gli assi nativi di proiezione (axis1 e axis2) usando la matrice calcolata
        float2 rotatedAxis1 = mul(rotationMatrix, view.axis1);
        float2 rotatedAxis2 = mul(rotationMatrix, view.axis2);

        // Generiamo il quad usando gli assi ruotati dal vento invece di quelli rigidi originari
        float2 deltaScreenPos = (quadPos.x * rotatedAxis1 + quadPos.y * rotatedAxis2) * 2 / _ScreenParams.xy;
        o.vertex = centerClipPos;
        o.vertex.xy += deltaScreenPos * centerClipPos.w;

        // 4. CODICE SELEZIONE ORIGINALE
        if (_SplatBitsValid)
        {
            uint wordIdx = instID / 32;
            uint bitIdx = instID & 31;
            uint selVal = _SplatSelectedBits.Load(wordIdx * 4);
            if (selVal & (1 << bitIdx))
            {
                o.col.a = -1;
            }
        }
    }
    FlipProjectionIfBackbuffer(o.vertex);
    return o;
}

half4 frag (v2f i) : SV_Target
{
	float power = -dot(i.pos, i.pos);
	half alpha = exp(power);
	if (i.col.a >= 0)
	{
		alpha = saturate(alpha * i.col.a);
	}
	else
	{
		// "selected" splat: magenta outline, increase opacity, magenta tint
		half3 selectedColor = half3(1,0,1);
		if (alpha > 7.0/255.0)
		{
			if (alpha < 10.0/255.0)
			{
				alpha = 1;
				i.col.rgb = selectedColor;
			}
			alpha = saturate(alpha + 0.3);
		}
		i.col.rgb = lerp(i.col.rgb, selectedColor, 0.5);
	}
	
    if (alpha < 1.0/255.0)
        discard;

    half4 res = half4(i.col.rgb * alpha, alpha);
    return res;
}
ENDCG
        }
    }
}
