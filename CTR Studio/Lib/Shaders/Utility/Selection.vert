#version 330

layout (location = 0) in vec3 vPositon;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

void main(){
    vec4 worldPosition = vec4(vPositon.xyz, 1);
    gl_Position = mtxMdl * mtxCam * worldPosition;
}