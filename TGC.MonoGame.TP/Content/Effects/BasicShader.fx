﻿#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 World;
float4x4 View;
float4x4 Projection;

float renderamt = 1;
float3 rendercolor = float3(1, 1, 1);
float AlphaTestReference = 1.0f;

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Normal: NORMAL0;
	float2 TextureCoordinate : TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float2 TextureCoordinate : TEXCOORD1;
	float3 WorldPos: TEXCOORD2;
	float3 Normal: TEXCOORD3;
	float4 sPos: TEXCOORD4;
};




texture ModelTexture;
sampler2D textureSampler = sampler_state
{
	Texture = (ModelTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

texture SkyboxTexture;
sampler2D skyboxSampler = sampler_state
{
	Texture = (SkyboxTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
};

float Time = 0;


VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	float4 worldPosition = mul(input.Position, World);
	float4 viewPosition = mul(worldPosition, View);
	// Project position
	output.sPos = output.Position = mul(viewPosition, Projection);

	// Normal
	output.Normal = mul ( (float3x3)World, input.Normal );

	// Propagate texture coordinates
	output.TextureCoordinate = float2(input.TextureCoordinate.x, input.TextureCoordinate.y);

	// Pos en world
	output.WorldPos = worldPosition.xyz;

	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float u = input.TextureCoordinate.x;
	float v = input.TextureCoordinate.y;
	float4 clr = tex2D(textureSampler, float2(u,v));
	if (clr.a < AlphaTestReference)
		discard;
	float3 LightPos = float3(1000, 5000, 1000);
	float3 L = normalize(LightPos - input.WorldPos);
	float3 N = normalize(input.Normal);
	float kd = abs(dot(N, L))*0.6 + 0.3;
	clr.rgb *= kd;
	return clr ;
	//return float4(N, 1);
}

float4 SpritePS(VertexShaderOutput input) : COLOR
{
	float4 clr = tex2D(textureSampler, input.TextureCoordinate);
	return float4(clr.rgb * rendercolor * renderamt, 1);	// float4(1, 0, 1, 1);
}

float4 ColorPS(VertexShaderOutput input) : COLOR
{
	return float4(1, 0, 1, 1);
}


float4 SkyboxPS(VertexShaderOutput input) : COLOR
{
	return tex2D(skyboxSampler, input.TextureCoordinate);
}

// para dibujar el mapa

float4 MapPS(VertexShaderOutput input) : COLOR
{
	float2 vPos = input.sPos.xy / input.sPos.w;
	float d = dot(vPos, vPos);
	if (d > 0.95)
		discard;

	float u = input.TextureCoordinate.x;
	float v = input.TextureCoordinate.y;
	float4 clr = tex2D(textureSampler, float2(u, v));
	if (clr.a < AlphaTestReference)
		discard;
	float3 LightPos = float3(1000, 5000, 1000);
	float3 L = normalize(LightPos - input.WorldPos);
	float3 N = normalize(input.Normal);
	float kd = abs(dot(N, L)) * 0.6 + 0.3;
	clr.rgb *= kd;
	vPos = normalize(vPos);
	if (vPos.y > 0.707)
		clr *= 0.8;
	else
		clr *= 0.2;
	return clr;
}



technique TextureDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};


technique SpriteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL SpritePS();
	}
};

technique ColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL ColorPS();
	}
};

technique SkyboxDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL SkyboxPS();
	}
};

technique Map
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MapPS();
	}
};
