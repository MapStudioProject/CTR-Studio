#version 330

uniform sampler2D textureInput;
uniform samplerCube textureCubeInput;
uniform sampler2DArray textureArrayInput;

in vec2 TexCoords;

uniform int textureType;
uniform int displayAlpha;
uniform int isBC5S;
uniform int isSRGB;

uniform float uBrightness;
uniform float uSaturation;
uniform float uHue;
uniform float uContrast;

uniform int currentMipLevel;
uniform int currentArrayLevel;

//Normal map conversion
uniform int ConvertToNormalMap;
uniform int HeightmapMode;
uniform vec2 heightmapSize;
uniform vec2 viewportSize;
uniform float normalStrength;


out vec4 fragOutput;

float ConvertHeightMap(vec3 color)
{
    switch (HeightmapMode)
    {
        //max color rgb
        case 0: return max(color.r, max(color.g, color.b));
        //screen blending
        case 1:  return 1.0 - (1.0-color.r)*(1.0-color.g)*(1.0-color.b);
        case 3: return color.r; //red
        case 4: return color.g; //green
        case 5: return color.b; //blue
    }
    return 0;
}

#define pixelToTexelRatio (viewportSize.xy/heightmapSize.xy)

vec3 ConvertNormalMap(vec3 color)
{
    float height = ConvertHeightMap(color);
    return vec3(-vec2(dFdx(height), dFdy(height)) * pixelToTexelRatio, 1.0);
}

#define textureOffsetValue 1.0

vec2 ConvertNormalMap2()
{
    vec2 s = 1.0 / heightmapSize.xy;
    
    float p = texture(textureInput, TexCoords).x;
    float h1 = texture(textureInput, TexCoords + s * vec2(textureOffsetValue,0)).x;
    float v1 = texture(textureInput, TexCoords + s * vec2(0,textureOffsetValue)).x;
       
   	return (p - vec2(h1, v1));
}


vec3 SetContrast(vec3 color, float contrast)
{
    return ((color.rgb - 0.5f) * max(contrast, 0)) + 0.5f;
}

vec3 SetBrightness(vec3 color, float amount)
{
    return color * vec3(amount);
}

vec3 SetSaturation(vec3 color, float adjustment)
{
    const vec3 W = vec3(0.2125, 0.7154, 0.0721);
    vec3 intensity = vec3(dot(color, W));
    return mix(intensity, color, adjustment);
}

//https://gist.github.com/mairod/a75e7b44f68110e1576d77419d608786
vec3 SetHueShift( vec3 color, float hueAdjust ){

    const vec3  kRGBToYPrime = vec3 (0.299, 0.587, 0.114);
    const vec3  kRGBToI      = vec3 (0.596, -0.275, -0.321);
    const vec3  kRGBToQ      = vec3 (0.212, -0.523, 0.311);

    const vec3  kYIQToR     = vec3 (1.0, 0.956, 0.621);
    const vec3  kYIQToG     = vec3 (1.0, -0.272, -0.647);
    const vec3  kYIQToB     = vec3 (1.0, -1.107, 1.704);

    float   YPrime  = dot (color, kRGBToYPrime);
    float   I       = dot (color, kRGBToI);
    float   Q       = dot (color, kRGBToQ);
    float   hue     = atan (Q, I);
    float   chroma  = sqrt (I * I + Q * Q);

    hue += hueAdjust;

    Q = chroma * sin (hue);
    I = chroma * cos (hue);

    vec3    yIQ   = vec3 (YPrime, I, Q);

    return vec3( dot (yIQ, kYIQToR), dot (yIQ, kYIQToG), dot (yIQ, kYIQToB) );
}

void main()
{  
    vec4 color = vec4(0);
    float alpha = 1.0;

    if (textureType == 0)
        color = textureLod(textureInput, TexCoords, currentMipLevel);
    if (textureType == 1)
        color = textureLod(textureArrayInput, vec3(TexCoords, currentArrayLevel), currentMipLevel);
    if (textureType == 2)
        color = textureLod(textureCubeInput, vec3(TexCoords, currentArrayLevel), currentMipLevel);

    alpha = color.a;

    if (isBC5S == 1) //BC5 Snorm conversion
    {
        color.rg = (color.rg * 2.0) - 1.0;
        color.b = color.b;
    }
    if (isSRGB == 1)
    {
        color.rgb = pow(color.rgb, vec3(1.0/2.2));
    }

    color.rgb = SetBrightness(color.rgb, uBrightness);
    color.rgb = SetSaturation(color.rgb, uSaturation);
    color.rgb = SetHueShift(color.rgb, uHue);
    color.rgb = SetContrast(color.rgb, uContrast);

    if (ConvertToNormalMap == 1)
    {
        color.rg = ConvertNormalMap2();
        color.b = 1.0;

        color.rg *= normalStrength;
        color.rg += 0.5;
    }
    fragOutput = vec4(color.rgb, alpha);
}  