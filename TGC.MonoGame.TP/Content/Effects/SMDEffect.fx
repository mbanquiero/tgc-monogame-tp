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
float4x4 bonesMatWorldArray[100];

struct VertexShaderInput
{
    float4 Position : SV_POSITION;
    float3 Normal : NORMAL0;
    float2 Uv : TEXCOORD0;
    float4 blendIndices : BLENDINDICES;
    float4 blendWeights : BLENDWEIGHT;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 Uv : TEXCOORD0;
    float3 WorldPos: TEXCOORD1;
    float3 Normal : TEXCOORD2;
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

float Time = 0;

VertexShaderOutput StaticMeshVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
    output.Normal = mul(input.Normal, World);
    output.Uv = input.Uv;
    output.WorldPos = worldPosition.xyz;
    return output;
}


VertexShaderOutput SkinnedMeshVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;


    float4 skinnedPosition = float4(0.0, 0.0, 0.0, 0.0);
    float4 skinnedNormal = float4(0.0, 0.0, 0.0, 0.0);
    int index = input.blendIndices[0];
    skinnedPosition += mul(input.Position, bonesMatWorldArray[index]) * input.blendWeights[0];

    float4 worldPosition = mul(skinnedPosition, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    output.Normal = mul(input.Normal, World);
    output.Uv = input.Uv;
    output.WorldPos = worldPosition.xyz;

    return output;
}


float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 clr = tex2D(textureSampler, input.Uv);
    if (clr.a < 0.05)
        discard;
    float3 LightPos = float3(1000, 5000, 1000);
    float3 L = normalize(LightPos - input.WorldPos);
    float3 N = normalize(input.Normal);
    float kd = abs(dot(N, L)) * 0.6 + 0.3;
    clr.rgb *= kd;
    return clr;
}

technique StaticMesh
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL StaticMeshVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};

technique SkinnedMesh
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL SkinnedMeshVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};

