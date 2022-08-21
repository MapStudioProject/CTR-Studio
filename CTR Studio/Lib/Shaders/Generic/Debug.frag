#version 330

uniform sampler2D DiffuseTexture;
uniform sampler2D UVTestPattern;

uniform int debugShading;
uniform vec4 highlight_color;

uniform int DrawAreaID;
uniform int AreaIndex;
uniform int isSelected;

in vec2 texCoord0;
in vec3 normal;
in vec3 boneWeightsColored;
in vec3 tangent;
in vec3 bitangent;
in vec4 vertexColor;

out vec4 fragOutput;

const int DISPLAY_NORMALS = 1;
const int DISPLAY_LIGHTING = 2;
const int DISPLAY_DIFFUSE = 3;
const int DISPLAY_VTX_CLR = 4;
const int DISPLAY_UV = 5;
const int DISPLAY_UV_PATTERN = 6;
const int DISPLAY_WEIGHTS = 7;
const int DISPLAY_TANGENT = 8;
const int DISPLAY_BITANGENT = 9;

void main(){
    vec4 outputColor = vec4(1);
    vec2 displayTexCoord = texCoord0;

    vec3 N = normal;

    if (debugShading == DISPLAY_NORMALS)
    {
        vec3 displayNormal = (N * 0.5) + 0.5;
        outputColor.rgb = displayNormal;
    }
    if (debugShading == DISPLAY_DIFFUSE)
    {
        outputColor = texture(DiffuseTexture,displayTexCoord);
    }
    if (debugShading == DISPLAY_LIGHTING)
    {
        vec3 displayNormal = (N * 0.5) + 0.5;
        float halfLambert = max(displayNormal.y,0.5);
        outputColor.rgb = vec3(0.5) * halfLambert;
    }
    if (debugShading == DISPLAY_UV)
         outputColor.rgb = vec3(displayTexCoord.x, displayTexCoord.y, 1.0);
    if (debugShading == DISPLAY_UV_PATTERN)
        outputColor.rgb = texture(UVTestPattern, displayTexCoord).rgb;
    if (debugShading == DISPLAY_WEIGHTS)
        outputColor.rgb = boneWeightsColored;
    if (debugShading == DISPLAY_TANGENT)
    {
        vec3 displayTangent = (tangent * 0.5) + 0.5;
        outputColor.rgb = displayTangent;
    }
    if (debugShading == DISPLAY_BITANGENT)
    {
        vec3 displayBitangent = (bitangent * 0.5) + 0.5;
        outputColor.rgb = displayBitangent;
    }
    if (debugShading == DISPLAY_VTX_CLR)
    {
        outputColor.rgb = vertexColor.rgb;
    }

    fragOutput = outputColor;

	if (highlight_color.w > 0.0)
	{
		//Highlight intensity for object selection
		float hc_a   = highlight_color.w;
		fragOutput = vec4(fragOutput.rgb * (1-hc_a) + highlight_color.rgb * hc_a, fragOutput.a);
	}

    if (DrawAreaID == 1)
    {
         if (AreaIndex == 0) fragOutput = vec4(1, 0, 0, 1);
         if (AreaIndex == 1) fragOutput = vec4(0, 1, 0, 1);
         if (AreaIndex == 2) fragOutput = vec4(0, 0, 1, 1);
         if (AreaIndex == 3) fragOutput = vec4(1, 1, 0, 1);
         if (AreaIndex == 4) fragOutput = vec4(0, 1, 1, 1);
         if (AreaIndex == 5) fragOutput = vec4(1, 0, 1, 1);
    }
}