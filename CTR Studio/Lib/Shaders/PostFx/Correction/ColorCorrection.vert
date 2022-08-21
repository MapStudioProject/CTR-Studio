#version 330
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 vTexCoord0;

out vec2 TexCoords;

void main()
{
    vec2 pos = vec2(aPos.x, aPos.y);

    gl_Position = vec4(pos.x, pos.y, 0.0, 1.0); 
    TexCoords = vec2(pos.x, pos.y) * 0.5714286 + 0.5002;
}