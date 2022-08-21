#version 330
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 vTexCoord0;

out vec2 TexCoords;
out vec3 ViewPos;
out vec2 FovScale;

uniform float fov_x;
uniform float fov_y;
uniform float z_range;

void main()
{
    gl_Position = vec4(aPos.x, aPos.y, 1.0, 1.0); 
    TexCoords = vec2(vTexCoord0.x, 1.0 - vTexCoord0.y);
   
    FovScale.x = tan(fov_x / 2.0);
    FovScale.y = tan(fov_y / 2.0);
    FovScale *= 2.0; // Required to avoid the multiplication by 2.0 in the fragment shader at "vec2 P_ndc = vec2(1.0) - texture_coordinate * 2.0;".
    FovScale *= z_range;
}   