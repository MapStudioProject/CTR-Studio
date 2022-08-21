#version 330 core
in vec2 texCoord;

out vec4 FragColor0;
out vec4 FragColor1;
out vec4 FragColor2;
out vec4 FragColor3;
out vec4 FragColor4;
out vec4 FragColor5;

const int MAX_LIGHTS = 6;

struct Settings
{
    float rim_angle;
    float rim_width;
    int type;
};

struct LightSource
{
    vec3 dir;
    vec4 lowerColor;
    vec4 upperColor;
    int lutIndex;
};

uniform Settings settings;

uniform LightSource lights[MAX_LIGHTS];

uniform sampler2D uNormalTex;
uniform sampler2D uLutTex;

vec4 CalculateLight(vec3 normal, LightSource light)
{
    float amount = 0.5 * -dot(normal, light.dir) + 0.5;
    float lut = light.lutIndex / 32.0;
    return vec4(mix(light.lowerColor.rgb, light.upperColor.rgb, texture(uLutTex, vec2(amount, lut)).r), amount);
}

vec4 CalculateCubeFaceColor(vec3 normal, int index)
{
    vec4 outColor = vec4(0);
    for (int i = 0 ; i < MAX_LIGHTS; i++) {
        outColor += CalculateLight(normal, lights[i]);
    }
    return outColor;
}

vec4 CalculateSphereColor(vec3 normal, int index)
{
    vec4 outColor = vec4(0);
    for (int i = 0 ; i < MAX_LIGHTS; i++) {
        outColor += CalculateLight(normal, lights[i]);
    }
    return outColor;
}

void main()
{		
    //Setup normals for each cubemap face
    vec3 nrm = texture(uNormalTex, texCoord).rgb;
    vec3 normal0 = vec3(  nrm.x,  nrm.y, -nrm.z );
    vec3 normal1 = vec3( -nrm.x,  nrm.y,  nrm.z );
    vec3 normal2 = vec3( -nrm.z,  nrm.x,  nrm.y );
    vec3 normal3 = vec3( -nrm.z, -nrm.x, -nrm.y );
    vec3 normal4 = vec3( -nrm.z,  nrm.y, -nrm.x );
    vec3 normal5 = vec3(  nrm.z,  nrm.y,  nrm.x );

    FragColor0 = CalculateCubeFaceColor(normal0, 0);
    FragColor1 = CalculateCubeFaceColor(normal1, 1);
    FragColor2 = CalculateCubeFaceColor(normal2, 2);
    FragColor3 = CalculateCubeFaceColor(normal3, 3);
    FragColor4 = CalculateCubeFaceColor(normal4, 4);
    FragColor5 = CalculateCubeFaceColor(normal5, 5);
}