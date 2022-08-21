#version 330

in int instanceID;
in vec3 normal;

out vec4 fragOutput;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

vec3 calculateSH(
    vec3 normal,
    vec4 sh00,
    vec4 sh01,
    vec4 sh02,
    vec4 sh10,
    vec4 sh11,
    vec4 sh12,
    vec4 sh2)
{
    return vec3(0.0);
}

layout (std140) uniform ProbeSHBuffer {
    vec4 probeBuffer[4096];
};

layout (std140) uniform ProbeInfo {
    vec4 probeInfo[1024];
};

void main(){

    int index = 7 * instanceID;

    vec3 color = calculateSH(normal,
                        probeBuffer[index],
                        probeBuffer[index+1],
                        probeBuffer[index+2],
                        probeBuffer[index+3],
                        probeBuffer[index+4],
                        probeBuffer[index+5],
                        probeBuffer[index+6]);

	fragOutput = vec4(color, 1.0);
}