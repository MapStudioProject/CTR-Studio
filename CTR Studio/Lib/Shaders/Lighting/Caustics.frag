#version 330 core
out vec4 FragColor;

in vec4 clipSpacePos;
in vec4 position;
in vec2 FovScale;

uniform sampler2D NormalsTexture;
uniform sampler2D LinearDepthTexture;
uniform sampler2D PatternTexture;
uniform sampler2D IndirectTexture;

uniform float clipRange;
uniform float clipNear;
uniform float clipFar;

uniform vec2 projectionOffset;
uniform vec2 projectionScale;

uniform mat4 mtxViewProjInv;
uniform mat4 mtxLightVP;

uniform vec2 viewSize;
uniform vec2 viewAspect;

uniform vec3 cameraPos;
uniform vec3 cameraDir;

in vec4 lightProjParameters;
  
vec4 CalculateProjPosition(vec2 texture_coordinate, float depth)
{
    float z = depth * 2.0 - 1.0;

    vec4 clipSpacePosition = vec4(texture_coordinate * 2.0 - 1.0, z, 1.0);
    //Invert the view/proj space
    vec4 viewSpacePosition = mtxViewProjInv * clipSpacePosition;
    // Perspective division
    viewSpacePosition /= viewSpacePosition.w;

    //Get the projection space position
    return  vec4(viewSpacePosition.xyz, 1.0);
}

void main()
{
    //Create tex coords
	vec3 ndc = clipSpacePos.xyz / clipSpacePos.w; //perspective divide/normalize
    vec2 texCoord  = (ndc.xy / 2.0 + 0.5); //ndc is -1 to 1 in GL. scale for 0 to 1
    texCoord = vec2(texCoord.x, 1.0 - texCoord.y);

    //Setup lighting
    vec3 normal = texture(NormalsTexture, texCoord).rgb;
    float d_p = dot(cameraDir, normal);
    //Temp for now
    vec3 displayNormal = (normal.xyz * 0.5) + 0.5;
    float intensity = max(displayNormal.y,0.5);


    //Setup the projection tex coord data
    float depth = texture(LinearDepthTexture, texCoord).r;
    vec4 projPos = CalculateProjPosition(texCoord, depth);
    vec2 projTexCoord = projPos.xy / projPos.w;

    //Load indirect coordinates
    vec2 indirectCoords = texture(IndirectTexture, projTexCoord).rg;
    indirectCoords += projectionOffset;
    indirectCoords *= projectionScale;
    //Load the diffuse color
    vec3 causticPattern = texture(PatternTexture, projTexCoord).rgb;
    //Apply caustic diffuse color
    vec3 color = causticPattern * 4.0f;
	FragColor = vec4(color.xyz, 1.0);
}