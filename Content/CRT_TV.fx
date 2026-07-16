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

// ── Uniforms you control from C# ──────────────────────────────────────────────
float Time;               // pass gameTime.TotalGameTime.TotalSeconds
float2 Resolution;        // screen size in pixels e.g. (1280, 720)
float CurvatureAmount;    // 0.0 = flat, 0.08 = noticeable, 0.15 = strong
float ScanlineStrength;   // 0.0 = none, 0.5 = subtle, 1.0 = heavy
float VignetteStrength;   // 0.0 = none, 1.0 = very dark corners
float AberrationAmount;   // 0.0 = none, 0.003 = subtle, 0.008 = strong
float NoiseStrength;      // 0.0 = none, 0.05 = subtle grain

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

// ── Helpers ───────────────────────────────────────────────────────────────────

// Cheap hash noise — no texture needed
float rand(float2 co)
{
    return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453);
}

// Barrel distortion — bends UV coords like CRT glass
float2 CurveUV(float2 uv)
{
    uv = uv * 2.0 - 1.0;           // remap 0..1 to -1..1
    float2 offset = abs(uv.yx) / float2(6.0, 4.0);
    uv = uv + uv * offset * offset * CurvatureAmount * 10.0;
    uv = uv * 0.5 + 0.5;           // remap back to 0..1
    return uv;
}

// ── Main pixel shader ─────────────────────────────────────────────────────────
float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TextureCoordinates;

    // ── 1. Curvature ──────────────────────────────────────────────────────────
    float2 curvedUV = CurveUV(uv);

    // Kill pixels that bent outside the 0..1 range (the black border)
    if (curvedUV.x < 0.0 || curvedUV.x > 1.0 ||
        curvedUV.y < 0.0 || curvedUV.y > 1.0)
        return float4(0, 0, 0, 1);

    // ── 2. Chromatic aberration ───────────────────────────────────────────────
    // Each channel samples from a slightly different UV
    float2 dir = curvedUV - 0.5;
    float r = tex2D(SpriteTextureSampler, curvedUV - dir * AberrationAmount).r;
    float g = tex2D(SpriteTextureSampler, curvedUV).g;
    float b = tex2D(SpriteTextureSampler, curvedUV + dir * AberrationAmount).b;
    float a = tex2D(SpriteTextureSampler, curvedUV).a;

    float4 color = float4(r, g, b, a);

    // ── 3. Scanlines ──────────────────────────────────────────────────────────
    float scanline = sin(curvedUV.y * Resolution.y * 3.14159) * 0.5 + 0.5;
    scanline = pow(scanline, 1.2);
    color.rgb *= lerp(1.0, scanline, ScanlineStrength);

    // ── 4. Vignette ───────────────────────────────────────────────────────────
    float2 vigUV = curvedUV * (1.0 - curvedUV.yx);
    float vignette = pow(vigUV.x * vigUV.y * 15.0, VignetteStrength);
    color.rgb *= clamp(vignette, 0.0, 1.0);

    // ── 5. Noise/grain ────────────────────────────────────────────────────────
    float noise = rand(curvedUV + frac(Time * 0.1));
    color.rgb += (noise - 0.5) * NoiseStrength;

    return color * input.Color;
}

technique CRT
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}

//CRT.Parameters["Time"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);
//CRT.Parameters["Resolution"].SetValue(
//new Vector2(1280, 720));
//CRT.Parameters["CurvatureAmount"].SetValue(0.5f);
//CRT.Parameters["ScanlineStrength"].SetValue(0.2f);
//CRT.Parameters["VignetteStrength"].SetValue(0.5f);
//CRT.Parameters["AberrationAmount"].SetValue(0.004f);
//CRT.Parameters["NoiseStrength"].SetValue(0.03f);
//canvas.Draw(_spriteBatch, CRT);