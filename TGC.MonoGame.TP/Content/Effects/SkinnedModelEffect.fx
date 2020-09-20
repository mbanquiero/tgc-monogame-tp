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
float4x4 gBonesOffsets[100];

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
    float3 Normal : NORMAL0;
    float2 Uv : TEXCOORD0;
    float4 Color : COLOR0;
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

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;


    float4 skinnedPosition = float4(0.0, 0.0, 0.0, 0.0);
    float4 skinnedNormal = float4(0.0, 0.0, 0.0, 0.0);
    
    int index = input.blendIndices[0];
    skinnedPosition += mul(input.Position, gBonesOffsets[index]) * input.blendWeights[0];
    index = input.blendIndices[1];
    skinnedPosition += mul(input.Position, gBonesOffsets[index]) * input.blendWeights[1];
    index = input.blendIndices[2];
    skinnedPosition += mul(input.Position, gBonesOffsets[index]) * input.blendWeights[2];
    index = input.blendIndices[3];
    skinnedPosition += mul(input.Position, gBonesOffsets[index]) * input.blendWeights[3];

    // Project position
    float4 worldPosition = mul(skinnedPosition, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    // Propagate texture coordinates
    output.Normal = mul(input.Normal, World);
    output.Uv = float2(input.Uv.x , -input.Uv.y);

    return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 textureColor = tex2D(textureSampler, input.Uv);
    return textureColor;
}

technique BasicColorDrawing
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
