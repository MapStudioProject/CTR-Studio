#version 330

uniform sampler2D textureInput;
uniform sampler2D backgroundTexture;

in vec2 TexCoords;

uniform int isSRGB;
uniform int hasTexture;
uniform int displayAlpha;
uniform int isBC5S;

uniform int backgroundMode;
uniform vec4 backgroundColor;

uniform float width;
uniform float height;
uniform int currentMipLevel;

uniform int channelSelector;

out vec4 fragOutput;

void main()
{  
    vec4 color = vec4(0);
    float alpha = 1.0;

    if (hasTexture == 1)
    {
        color = textureLod(textureInput, TexCoords, currentMipLevel);
        alpha = color.a;

        if (isSRGB == 1)
            color.rgb = pow(color.rgb, vec3(1.0/2.2));
        if (isBC5S == 1) //BC5 Snorm conversion
        {
           color.rg = (color.rg + 1.0) / 2.0;
           color.b = color.b;
        }
    }
    else
    {
        if (backgroundMode == 0)
            color = texture(backgroundTexture, TexCoords);
        else if (backgroundMode == 1)
           color = vec4(0, 0, 0, 1);
        else if (backgroundMode == 2)
           color = vec4(1, 1, 1, 1);
        else if (backgroundMode == 3)
           color = backgroundColor;
    }

    if (displayAlpha == 0)
       alpha = 1.0;

    fragOutput = vec4(color.rgb, alpha);
    if (channelSelector != -1)
    {
        if (channelSelector == 0) fragOutput.rgb = fragOutput.rrr;
        if (channelSelector == 1) fragOutput.rgb = fragOutput.ggg;
        if (channelSelector == 2) fragOutput.rgb = fragOutput.bbb;
        if (channelSelector == 3) fragOutput.rgb = fragOutput.aaa;
        fragOutput.a = 1.0;
    }
}  