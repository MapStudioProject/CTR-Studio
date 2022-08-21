#version 330 core

out vec4 FragColor;
in vec2 TexCoords;

uniform sampler2D textureData;

void main()
{		
    vec4 hdrColor = texture(textureData, TexCoords).rgba;
    FragColor.rgb = hdrColor.rgb * hdrColor.a;
    FragColor.a = 1.0;
}