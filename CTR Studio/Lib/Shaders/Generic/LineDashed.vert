#version 330

layout(location = 0) in vec3 vPosition;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;
uniform mat4 mtxView;
uniform mat4 mtxProj;

uniform vec2 viewport_size;

flat out vec2 stipple_start;
out vec2 stipple_pos;

void main()
{
    gl_Position  = mtxCam*mtxMdl*vec4(vPosition.xyz, 1.0);

    stipple_start = stipple_pos = (gl_Position .xy / gl_Position .w);
}