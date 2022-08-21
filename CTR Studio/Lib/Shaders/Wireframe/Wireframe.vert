#version 150
layout(location = 0) in vec3 vPosition;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

void main()
{
    gl_Position = mtxCam*mtxMdl*vec4(vPosition.xyz, 1.0);
}