#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

float  Time;
float2 Resolution;
float  RainSpeed;
float  RainDensity;
float  RainAngle;
float  DropLength;
float  DropWidth;
float  RainOpacity;
float  RippleStrength;

struct VertexShaderOutput
{
    float4 Position           : SV_POSITION;
    float4 Color              : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

// ── Vertex Shader (required for the shader to work) ──
VertexShaderOutput MainVS(float4 position : POSITION0, float4 color : COLOR0, float2 texCoord : TEXCOORD0)
{
    VertexShaderOutput output;
    output.Position = position;
    output.Color = color;
    output.TextureCoordinates = texCoord;
    return output;
}

// ── Helpers ───────────────────────────────────────────────────────────────────

float rand(float2 co)
{
    return frac(sin(dot(co, float2(127.1, 311.7))) * 43758.5453);
}

// Draws one layer of rain streaks
float RainLayer(float2 uv, float seed, float layerScale)
{
    // Apply wind slant
    float2 slantedUV = uv;
    slantedUV.x += slantedUV.y * tan(RainAngle);

    // Aspect-correct grid so drops are same width on any resolution
    float aspect = Resolution.x / Resolution.y;
    float2 scale = float2(RainDensity * layerScale * aspect,
                          RainDensity * layerScale);

    float2 cell   = floor(slantedUV * scale);
    float2 cellUV = frac(slantedUV * scale);

    // Per-cell random values
    float rndOffset = rand(cell + seed);
    float rndX      = rand(cell + seed + 17.3);
    float rndSpeed  = 0.7 + rndOffset * 0.6;

    // Falling position: 0 = top of cell, 1 = bottom
    float fall = frac(Time * RainSpeed * rndSpeed + rndOffset);

    // Horizontal centre of this drop within its cell
    float dropX = 0.2 + rndX * 0.6;

    // ── Streak shape ─────────────────────────────────────────────────────────
    // Horizontal: sharp falloff around dropX
    float dx = abs(cellUV.x - dropX);
    float width = 1.0 - smoothstep(0.0, 0.15 / layerScale, dx);

    // Longer streaks
    float streakLength = DropLength * (0.8 + rndOffset * 0.7);
    
    // Vertical: streak occupies [fall - streakLength .. fall]
    float dy   = cellUV.y - fall;
    float head = smoothstep(0.0, -0.02, dy);
    float tail = smoothstep(-streakLength, 0.0, dy);

    float streak = width * head * tail;
    
    // Boost brightness
    streak = pow(streak, 0.7) * 1.5;

    return clamp(streak, 0.0, 1.0);
}

// ── Main ──────────────────────────────────────────────────────────────────────

// ... keep the top part the same until the MainPS function ...

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TextureCoordinates;
    
    // Get the original scene
    float4 scene = tex2D(SpriteTextureSampler, uv);
    
    // SIMPLE VISIBLE RAIN PATTERN FOR TESTING
    // This creates obvious moving white streaks
    
    // Create a grid of rain
    float2 rainUV = uv;
    rainUV.x += uv.y * 0.5; // Slant
    
    // Create vertical stripes that move down
    float stripes = frac(rainUV.y * 30 - Time * 3.0);
    stripes = smoothstep(0.7, 0.95, stripes);
    
    // Add horizontal variation
    float xPattern = sin(rainUV.x * 50) * 0.5 + 0.5;
    stripes *= xPattern;
    
    // Increase visibility
    stripes = clamp(stripes * 2.0, 0.0, 1.0);
    
    // Composite: bright white rain
    float4 rainColor = float4(1.0, 1.0, 1.0, 1.0);
    float4 result = lerp(scene, rainColor, stripes * 0.8);
    
    return result * input.Color;
}

technique Rain
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}