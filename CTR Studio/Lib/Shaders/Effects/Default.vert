#version 330

in vec3 v_inPos;
in vec3 v_inNormal;
in vec4 v_inColor;
in vec2 v_inTexCoord;

out vec2 texCoords0;
out vec3 normal;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

void main(){
    vec4 worldPosition = vec4(v_inPos.xyz, 1);
    gl_Position = mtxCam * mtxMdl * worldPosition;
    normal = v_inNormal;
    texCoords0 = v_inTexCoord;
}