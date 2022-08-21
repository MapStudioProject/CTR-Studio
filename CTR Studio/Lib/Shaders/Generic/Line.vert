#version 330

layout(location = 0) in vec3 vPosition;
layout(location = 1) in vec4 vColor;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;
uniform mat4 mtxView;
uniform mat4 mtxProj;

out vec4 vertexColor;

void main()
{
    vertexColor = vColor;
    gl_Position = mtxCam*mtxMdl*vec4(vPosition.xyz, 1.0);
}