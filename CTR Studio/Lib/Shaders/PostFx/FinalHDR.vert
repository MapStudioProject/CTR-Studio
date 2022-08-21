#version 330
layout (location = 0) in vec2 aPos;
layout (location = 2) in vec2 vTexCoord0;

out vec2 TexCoords;

void main()
{
    gl_Position = vec4(aPos.x, aPos.y, 0.0, 1.0); 
    TexCoords = vTexCoord0;
}