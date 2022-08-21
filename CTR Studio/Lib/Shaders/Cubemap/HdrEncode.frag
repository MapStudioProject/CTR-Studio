#version 330 core

out vec4 FragColor;

in vec3 WorldPos;

uniform float mipLevel;
uniform float scale;
uniform float range;
uniform int srgbToLinear;
uniform int flipY;

uniform samplerCube cubemapTexture;

void main()
{		
    vec3 pos = WorldPos;
    if (flipY == 1)
       pos = pos * vec3(1, -1, 1);

    vec3 N = normalize(pos);
    vec3 color = textureLod(cubemapTexture, N, mipLevel).rgb;
    //Gamma lower since the engine gamma corrects
    if (srgbToLinear == 1)
        color = pow(color, vec3(2.2));

    //HDR scaling
    //Get max color brightness
    float maxComp = max(color.r, max(color.g, color.b));
    float scaled = max(maxComp + fract(1.0 - (maxComp * 4.0)) * 0.25, 0.0390625);
        
    //Calculate alpha with pow scale and range
    float alpha = clamp(scaled / range, 0.0, 1.0);
	alpha = pow( alpha, 1.0 / scale );

    FragColor.rgb = color / scaled;
    FragColor.a = alpha;
}