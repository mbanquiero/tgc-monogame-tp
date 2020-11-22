#if OPENGL
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


struct VertexShaderInput
{
	float4 Position : POSITION0;
	float2 TextureCoordinate : TEXCOORD0;
	float3 Normal: NORMAL0;
	float2 LightmapCoordinate : TEXCOORD1;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float3 Normal : TEXCOORD0;
	float2 TextureCoordinate : TEXCOORD1;
	float3 WorldPos: TEXCOORD2;
	float2 LightmapCoordinate: TEXCOORD3;
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

texture Lightmap;
sampler2D lightmapSampler = sampler_state
{
	Texture = (Lightmap);
	MagFilter = Linear;
	MinFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};


float Time = 0;


VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	float4 worldPosition = mul(input.Position, World);
	float4 viewPosition = mul(worldPosition, View);

	// Project position
	output.sPos = output.Position = mul(viewPosition, Projection);

	// Propagate texture coordinates
	output.TextureCoordinate = input.TextureCoordinate;
	output.LightmapCoordinate = input.LightmapCoordinate;

	output.Normal = input.Normal;

	output.WorldPos = worldPosition.xyz;


	return output;
}

float3 DecompressLightmapSample(float4 s)
{
	float exp = s.a * 255.0 - 128.0;
	return pow(s.rgb * pow(2.0, exp), float3(0.5, 0.5, 0.5));
}

float4 MainPS(VertexShaderOutput input) : COLOR
{

	float3 LightPos = float3(1000,5000,1000);
	float3 L = normalize(LightPos - input.WorldPos);
	float3 N = normalize(input.Normal);
	float kd = abs(dot(N, L)) + 0.4;
	float4 clr = tex2D(textureSampler, input.TextureCoordinate.xy);
	float  u = input.LightmapCoordinate.x;
	float  v = input.LightmapCoordinate.y;
	float4 ls = tex2D(lightmapSampler, float2(v,-u));
	float3 kl = DecompressLightmapSample(ls);
	return float4(clr.rgb*kd*kl, clr.a);
}


float4 DummyPS(VertexShaderOutput input) : COLOR
{
	// Get the texture texel textureSampler is the sampler, Texcoord is the interpolated coordinates
	float3 clr = tex2D(textureSampler, input.TextureCoordinate.xy).rgb;
	float3 lm = tex2D(lightmapSampler, input.LightmapCoordinate.xy).rgb;
	return float4(clr + lm, 1);
}


float4 DebugBBVS(in float4 Position:POSITION0) : SV_POSITION
{
	return  mul(mul(mul(Position, World), View), Projection);
}

float4 DebugBBPS() : COLOR
{
	return float4(0.1,0.1,0,0.5);
}



float4 BloodPS(VertexShaderOutput input) : COLOR
{
	float4 clr = tex2D(textureSampler, input.TextureCoordinate.xy);
	if (clr.r< clr.g+clr.b)
		discard;
	return clr;
}


float4 MapPS(VertexShaderOutput input) : COLOR
{
	float2 vPos = input.sPos.xy / input.sPos.w;
	float d = dot(vPos, vPos);
	if (d > 0.95)
		discard;

	float4 clr = tex2D(textureSampler, input.TextureCoordinate.xy);
	vPos = normalize(vPos);
	if (vPos.y > 0.7)
		clr *= 0.8;
	else
		clr *= 0.2;
	return clr;
}


VertexShaderOutput ClearVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	output.Position = input.Position;
	output.TextureCoordinate = input.TextureCoordinate;
	return output;
}


float4 ClearPS(VertexShaderOutput input) : COLOR
{
	float2 vPos = 2 * input.TextureCoordinate - 1;
	float d = dot(vPos, vPos);
	if (d > 1)
		discard;
	return d > 0.95 ? float4(0.5, 0.5, 0.5,1) : float4(0,0,0,1);
}

VertexShaderOutput DrawImageVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	output.Position = mul(input.Position, World);
	output.TextureCoordinate = input.TextureCoordinate;
	return output;
}

float4 DrawImagePS(VertexShaderOutput input) : COLOR
{
	return tex2D(textureSampler, input.TextureCoordinate.xy);
}



technique Phong
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};



technique Dummy
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL DummyPS();
	}
};


technique DebugBB
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL DebugBBVS();
		PixelShader = compile PS_SHADERMODEL DebugBBPS();
	}
};

technique Blood
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL BloodPS();
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



technique ClearScreen
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL ClearVS();
		PixelShader = compile PS_SHADERMODEL ClearPS();
	}
};

technique DrawImage
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL DrawImageVS();
		PixelShader = compile PS_SHADERMODEL DrawImagePS();
	}
};


