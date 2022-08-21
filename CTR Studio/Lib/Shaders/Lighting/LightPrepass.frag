#version 330

uniform sampler2D normalsTexture;
uniform sampler2D depthTexture;

in vec2 TexCoords;
in vec3 ViewPos;
in vec2 FovScale;

out vec4 fragOutput0;
out vec4 fragOutput1;

const int MAX_POINT_LIGHTS = 1;
const int MAX_SPOT_LIGHTS = 1;

uniform vec3 viewPos;

uniform mat4 mtxProjInv;
uniform mat4 mtxViewInv;

uniform vec3 cameraPosition;

struct PointLight
{
    vec3 uPosition;
    vec3 uDirection;
    vec3 uDiffuseColor;
};

uniform PointLight pointLights[MAX_POINT_LIGHTS];

vec3 calculate_view_position(vec2 texture_coordinate, float depth, vec2 scale_factor)  // "scale_factor" is "v_fov_scale".
{
    vec2 half_ndc_position = vec2(0.5) - texture_coordinate;    // No need to multiply by two, because we already baked that into "v_tan_fov.xy".
    vec3 view_space_position = vec3(half_ndc_position * scale_factor.xy * -depth, -depth); // "-depth" because in OpenGL the camera is staring down the -z axis (and we're storing the unsigned depth).
    return(view_space_position);
}

vec3 calculate_world_position(vec2 texture_coordinate, float depth)  // "scale_factor" is "v_fov_scale".
{
    float z = depth * 2.0 - 1.0;

    vec4 clipSpacePosition = vec4(texture_coordinate * 2.0 - 1.0, z, 1.0);
    vec4 viewSpacePosition = mtxProjInv * clipSpacePosition;

    // Perspective division
    viewSpacePosition /= viewSpacePosition.w;

    vec4 worldSpacePosition = mtxViewInv * viewSpacePosition;

    return worldSpacePosition.xyz;
}

void main()
{  
    vec3 normals = texture(normalsTexture, TexCoords).rgb;
    float linearDepth  = texture(depthTexture, TexCoords).r;

    vec3 viewPosition = calculate_view_position(TexCoords, linearDepth, FovScale);
    vec3 fragPosition = calculate_world_position(TexCoords, linearDepth);

    vec3 lighting = vec3(0);

    vec3 viewDir = normalize(cameraPosition - fragPosition);
    for (int i = 0; i < MAX_POINT_LIGHTS; i++)
    {
        float dist    = length(pointLights[i].uPosition - fragPosition);
        float attenuation = 1.0 / (1.0 + 0.09 * dist + 0.032 * (dist * dist));   

        vec3 color = pointLights[i].uDiffuseColor;
        vec3 diffuse = vec3(1, 0, 0) * attenuation * 22.2;
        lighting += diffuse;
    }
    for (int i = 0; i < MAX_SPOT_LIGHTS; i++)
    {

    }

    fragOutput0 = vec4(lighting, 1.0);
    //Second output for additional brightness? Leave it zero for now.
    fragOutput1 = vec4(0);
}  