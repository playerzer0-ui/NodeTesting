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
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
};

// ── Rain parameters you control from C# ──────────────────────────────────────
float Time; // gameTime.TotalGameTime.TotalSeconds
float2 Resolution; // screen size in pixels (1280, 720)
float RainSpeed; // speed of rainfall (0.5 - 3.0)
float RainDensity; // number of drops (5 - 50)
float RainAngle; // angle of rain in radians (0.0 = vertical, 0.3 = slanted)
float DropLength; // length of raindrops (0.05 - 0.3)
float DropWidth; // width of raindrops (0.01 - 0.05)
float RainOpacity; // opacity of rain (0.0 - 1.0)
float RippleStrength; // distortion effect on underlying scene (0.0 - 1.0)

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

// ── Random number generator ──────────────────────────────────────────────────
float hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
}

// ── 2D noise for rain patterns ──────────────────────────────────────────────
float noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    
    float a = hash(i);
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));
    
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// ── Main pixel shader ─────────────────────────────────────────────────────────
float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TextureCoordinates;
    float2 screenUV = uv * Resolution;
    
    // ── 1. Sample the underlying scene ──────────────────────────────────────
    float4 sceneColor = tex2D(SpriteTextureSampler, uv);
    
    // ── 2. Raindrop generation ──────────────────────────────────────────────
    // Create multiple layers of rain for depth effect
    float rainMask = 0.0;
    float rippleDistortion = 0.0;
    
    for (int i = 0; i < 3; i++)
    {
        // Different layers with varying properties
        float layerScale = 1.0 + i * 0.5;
        float layerSpeed = RainSpeed * (1.0 + i * 0.3);
        float layerDensity = RainDensity * (1.0 + i * 0.2);
        float layerOpacity = RainOpacity * (1.0 - i * 0.2);
        
        // Calculate rain position with angle
        float2 rainDir = float2(sin(RainAngle), -cos(RainAngle));
        float2 rainOffset = rainDir * Time * layerSpeed * 100.0;
        
        // Scale and offset for this layer
        float2 rainUV = screenUV / layerScale + rainOffset;
        
        // Calculate drop position
        float2 dropPos = floor(rainUV / layerDensity);
        float2 localPos = frac(rainUV / layerDensity);
        
        // Randomize drop position slightly for natural look
        float2 dropOffset = float2(
            hash(dropPos + float2(0.0, 0.0)),
            hash(dropPos + float2(100.0, 0.0))
        ) * 0.8 - 0.4;
        
        float2 finalPos = localPos + dropOffset;
        
        // Create the raindrop shape
        float drop = 0.0;
        float dropHeight = DropLength * (0.5 + hash(dropPos + float2(50.0, 0.0)) * 0.5);
        float dropWidth = DropWidth * (0.5 + hash(dropPos + float2(0.0, 50.0)) * 0.5);
        
        // Main drop shape - elongated ellipse
        float2 dropUV = finalPos;
        dropUV.x -= 0.5; // Center horizontally
        dropUV.y -= 0.5 + dropHeight * 0.5; // Center vertically with offset for fall
        
        // Add blur to the drop based on speed
        float speedBlur = 0.5 + layerSpeed * 0.5;
        
        // Drop body (elongated)
        float dropBody = 1.0 - abs(dropUV.y / (dropHeight * 0.5));
        dropBody = pow(dropBody, 2.0);
        
        float dropWidthFactor = 1.0 - abs(dropUV.x / (dropWidth * 0.5));
        drop = dropBody * dropWidthFactor;
        drop = max(0.0, drop);
        drop = pow(drop, 1.5);
        
        // Add a bright head at the top of the drop
        float head = 1.0 - abs(dropUV.y / (dropHeight * 0.2));
        head = pow(max(0.0, head), 4.0);
        head *= exp(-abs(dropUV.x / (dropWidth * 0.3)));
        head *= 0.5;
        
        drop += head;
        
        // Apply to rain mask with layer opacity
        float layerMask = drop * layerOpacity * 0.5;
        rainMask = max(rainMask, layerMask);
        
        // Calculate ripple distortion for this layer
        if (drop > 0.1)
        {
            rippleDistortion += drop * RippleStrength * 0.02 * (1.0 - i * 0.2);
        }
    }
    
    // ── 3. Apply rain to the scene ──────────────────────────────────────────
    // Add subtle ripple distortion to the scene under rain
    float2 rippleUV = uv;
    if (rippleDistortion > 0.01)
    {
        // Randomize ripple direction
        float rippleAngle = hash(float2(uv.x * 100.0 + Time * 10.0, uv.y * 100.0)) * 6.28318;
        float2 rippleDir = float2(cos(rippleAngle), sin(rippleAngle));
        rippleUV += rippleDir * rippleDistortion * 0.5;
        
        // Clamp UV to prevent edge artifacts
        rippleUV = clamp(rippleUV, 0.001, 0.999);
        
        // Resample scene with ripple
        float4 rippleScene = tex2D(SpriteTextureSampler, rippleUV);
        sceneColor = lerp(sceneColor, rippleScene, 0.5);
    }
    
    // ── 4. Apply rain on top of scene ──────────────────────────────────────
    // Rain drops appear white/bright against the scene
    float3 rainColor = float3(0.9, 0.95, 1.0); // Slightly blue-white
    
    // Mix rain over the scene
    float3 finalColor = lerp(sceneColor.rgb, rainColor, rainMask);
    
    // Add subtle screen-wide rain streaks for atmosphere
    float streakNoise = noise(float2(uv.x * 500.0 + Time * RainSpeed * 50.0, uv.y * 200.0 - Time * RainSpeed * 100.0));
    float streak = pow(streakNoise, 10.0) * 0.3 * RainOpacity;
    finalColor += streak * 0.1;
    
    // Add misty haze at the bottom for atmosphere
    float mist = pow(1.0 - uv.y, 3.0) * 0.1 * RainOpacity;
    finalColor = lerp(finalColor, float3(0.8, 0.85, 0.9), mist);
    
    return float4(finalColor, sceneColor.a);
}

technique Rain
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}

//rain.Parameters["Time"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);
//rain.Parameters["Resolution"].SetValue(new Vector2(1280, 720));
//rain.Parameters["RainSpeed"].SetValue(3.0f);     // Much faster! Try 2.0-5.0
//rain.Parameters["RainDensity"].SetValue(25f);
            
//rain.Parameters["DropLength"].SetValue(0.2f);    // Longer drops
//rain.Parameters["DropWidth"].SetValue(0.02f);
//rain.Parameters["RainOpacity"].SetValue(0.8f);
//rain.Parameters["RippleStrength"].SetValue(0.3f);