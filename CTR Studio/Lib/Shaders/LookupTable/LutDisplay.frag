#version 400 core

uniform sampler3D dynamic_texture_array;

in vec2 TexCoords;

out vec4 out_color;

void main()
{
    float texCoordShift = TexCoords.x / 16;
    float texCoordX = TexCoords.x;

    out_color.rgb = textureLod( dynamic_texture_array, vec4(texCoordX,TexCoords.y,0,0), 0).rgb;
    out_color.a = 1.0;
}