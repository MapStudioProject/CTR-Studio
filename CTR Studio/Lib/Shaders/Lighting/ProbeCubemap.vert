#version 330
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 vTexCoord0;

out vec4 in_attr0;

void main()
{
    gl_Position = vec4(aPos.x, aPos.y, 0.0, 1.0); 
    in_attr0 = vec4(vTexCoord0.x, 1.0 - vTexCoord0.y, 0, 0);
}