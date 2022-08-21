#version 330

uniform sampler2DShadow shadowMap;
uniform sampler2D depthTexture;
uniform sampler2D normalsTexture;

uniform mat4 mtxViewProjInv;
uniform mat4 mtxLightVP;

uniform vec3 lightPos;
uniform vec3 viewPos;
uniform float shadowBias;

in vec2 TexCoords;

out vec4 fragOutput;

const float shadowDistance = 2500;
const float transitionDistance = 100;

vec4 CalculatePosition(vec2 texture_coordinate, float depth)
{
    float z = depth * 2.0 - 1.0;

    vec4 clipSpacePosition = vec4(texture_coordinate * 2.0 - 1.0, z, 1.0);
    //Invert the view/proj space
    vec4 viewSpacePosition = mtxViewProjInv * clipSpacePosition;
    // Perspective division
    viewSpacePosition /= viewSpacePosition.w;

    //Get the position in light space
    return vec4(viewSpacePosition.xyz, 1.0);
}

float CalculateShadowPCF25(sampler2DShadow shadowTex, vec4 uv)
{
    float shadow = 0.0;
    float currentDepth = uv.z;
	for (int x = -2; x <= 2; x++) {
		for (int y = -2; y <= 2; y++) {
			float texelDepth = textureOffset(shadowMap, uv.xyz, ivec2(x, y));
			shadow += texelDepth;
 		}
	}
   return shadow / 25.0;
}

float CalculateShadowPCF9(sampler2DShadow shadowTex, vec4 uv)
{
    float shadow = 0.0;
    float currentDepth = uv.z;
	for (int x = -1; x <= 1; x++) {
		for (int y = -1; y <= 1; y++) {
			float texelDepth = textureOffset(shadowMap, uv.xyz, ivec2(x, y));
			shadow += texelDepth;
 		}
	}
   return shadow / 9.0;
}

float CalculateShadowPCF4(sampler2DShadow shadowTex, vec4 uv)
{
    float shadow = 0.0;
    shadow += textureOffset(shadowMap, uv.xyz, ivec2(0, 1));
    shadow += textureOffset(shadowMap, uv.xyz, ivec2(0, -1));
    shadow += textureOffset(shadowMap, uv.xyz, ivec2(1, 0));
    shadow += textureOffset(shadowMap, uv.xyz, ivec2(-1, 0));

   return shadow / 4.0;
}

float CalculateShadow(vec3 fragPos, vec4 fragPosLightSpace)
{
    // Perspective division
   vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
   //Adjust bias
   projCoords.z -= shadowBias;
    // Transform to [0,1] range
   projCoords = projCoords * 0.5 + 0.5;

   return CalculateShadowPCF4(shadowMap, vec4(projCoords, 1.0));
}

void main()
{            
   float depth = texture(depthTexture, TexCoords).r;
   vec4 fragPos = CalculatePosition(TexCoords, depth);
   vec4 fragPosLightSpace = mtxLightVP * fragPos;
   float shadow = CalculateShadow(fragPos.xyz, fragPosLightSpace);
   shadow = clamp(shadow, 0.0, 1.0);

   float ambientOcc = 1.0;
   float staticShadow = 1.0;

   fragOutput.r = shadow;
   fragOutput.g = ambientOcc;
   fragOutput.b = staticShadow;
   fragOutput.a = 1.0;
}  