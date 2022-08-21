#version 330
layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec3 vNormal;

out int instanceID;
out vec3 normal;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

layout (std140) uniform ProbeSHBuffer {
    vec4 probeBuffer[4096];
};

layout (std140) uniform ProbeInfo {
    vec4 probeInfo[1024];
};
void main(){
    int instance = gl_InstanceID;
    instanceID = instance;

    vec4 probeParam = probeInfo[instance];

    vec3 position = probeParam.xyz;
    float scale = probeParam.w;
    
    mat4 transform = mat4(
        vec4( scale, 0.0, 0.0, 0.0),
        vec4( 0.0, scale, 0.0, 0.0),
        vec4( 0.0, 0.0, scale, 0.0),
        vec4( position, 1.0) );

    normal = normalize(mat3(transform) * vNormal.xyz);
	gl_Position = mtxCam * transform * vec4(vPosition.xyz, 1);
}