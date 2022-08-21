#version 400 core

uniform samplerCube dynamic_texture;
uniform samplerCubeArray dynamic_texture_array;

uniform int is_array;

uniform int arrayLevel;
uniform int mipLevel;

in vec2 TexCoords;

out vec4 out_color;

void main()
{
    float phi=TexCoords.x*3.1415*2;
    float theta=(-TexCoords.y+0.5)*3.1415;
    
    vec3 dir = vec3(cos(phi)*cos(theta),sin(theta),sin(phi)*cos(theta));

    if (is_array == 1)
        out_color.rgb = textureLod( dynamic_texture_array, vec4(dir, arrayLevel), mipLevel).rgb;
    else
        out_color.rgb = textureLod( dynamic_texture, dir, mipLevel ).rgb;

    out_color.a = 1.0;
}