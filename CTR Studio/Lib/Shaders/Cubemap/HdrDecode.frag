#version 330 core

out vec4 FragColor;

in vec3 WorldPos;

uniform float mipLevel;
uniform float gamma;
uniform float scale;
uniform float range;

uniform samplerCube cubemapTexture;

void main()
{		
    vec3 N = normalize(WorldPos);
    vec4 envTexture = textureLod(cubemapTexture, N, mipLevel);
    vec3 envColor = envTexture.rgb;

    envColor *= pow(envTexture.a, scale) * range;
    envColor = envColor / (envColor + vec3(1.0));
    envColor = pow(envColor, vec3(1.0/gamma)); 

    FragColor = vec4(envColor, 1.0);
}