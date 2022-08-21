#version 330 core
in vec2 TexCoords;

uniform sampler2D screenTexture;

uniform int flipVertical;

out vec4 FragColor;

void main()
{
    if (flipVertical == 1)
        FragColor = texture(screenTexture, vec2(TexCoords.x, 1 - TexCoords.y));
    else
        FragColor = texture(screenTexture, TexCoords);
}