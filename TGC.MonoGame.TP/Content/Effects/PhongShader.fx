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
	output.Position = mul(viewPosition, Projection);

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
	float3 clr = tex2D(textureSampler, input.TextureCoordinate.xy).rgb;
	float  u = input.LightmapCoordinate.x;
	float  v = input.LightmapCoordinate.y;
	float4 ls = tex2D(lightmapSampler, float2(v,-u));
	float3 kl = DecompressLightmapSample(ls);
	//return float4(DecompressLightmapSample(ls), 1);

	/*
	u = input.TextureCoordinate.x;
	v = input.TextureCoordinate.y;
	clr = tex2D(textureSampler, float2(-u,v)).rgb;
	*/

	//return float4(clr * kd, 1);
	return float4(clr*kd*kl, 1);
	//return float4(1, 0, 1, 1);

}


float4 DummyPS(VertexShaderOutput input) : COLOR
{
	// Get the texture texel textureSampler is the sampler, Texcoord is the interpolated coordinates
	float3 clr = tex2D(textureSampler, input.TextureCoordinate.xy).rgb;
	float3 lm = tex2D(lightmapSampler, input.LightmapCoordinate.xy).rgb;
	return float4(clr + lm, 1);
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



