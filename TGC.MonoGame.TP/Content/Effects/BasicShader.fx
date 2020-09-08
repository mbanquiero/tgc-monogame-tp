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
	float4 Normal: NORMAL0;
	float2 TextureCoordinate : TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float2 TextureCoordinate : TEXCOORD1;
	float3 WorldPos: TEXCOORD2;
	float3 Normal: TEXCOORD3;
};




texture ModelTexture;
sampler2D textureSampler = sampler_state
{
	Texture = (ModelTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	AddressU = Mirror;
	AddressV = Mirror;
};

float Time = 0;


VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;
	float4 worldPosition = mul(input.Position, World);
	float4 viewPosition = mul(worldPosition, View);
	// Project position
	output.Position = mul(viewPosition, Projection);

	// Normal
	output.Normal = mul ( (float3x3)World, input.Normal );

	// Propagate texture coordinates
	output.TextureCoordinate = input.TextureCoordinate;

	// Pos en world
	output.WorldPos = worldPosition.xyz;

	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float3 LightPos = float3(1000, 5000, 1000);
	float3 L = normalize(LightPos - input.WorldPos);
	float3 N = normalize(input.Normal);
	float kd = abs(dot(N, L)) + 0.1;
	float u = input.TextureCoordinate.x;
	float v = input.TextureCoordinate.y;
	float4 clr = tex2D(textureSampler, float2(u,v));
	clr.rgb *= kd;
	return clr ;
}


technique TextureDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};


