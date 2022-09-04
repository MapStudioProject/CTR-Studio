#version 330 core
in vec2 TexCoords;

uniform sampler2D textureInput;

uniform int type;
uniform int operand;
uniform int isAlpha;
uniform int showAlpha;

uniform vec4 color;

out vec4 FragColor;

void main()
{
    vec4 output_color = vec4(1.0);
    if (type == 1)
        output_color = texture(textureInput, TexCoords);
    else
        output_color = color;

    FragColor = output_color;
    if (isAlpha == 1)
    {
        switch (operand)
        {
            case 0: FragColor.rgb = output_color.aaa; break;
            case 1: FragColor.rgb = vec3(1.0) - output_color.aaa; break;
            case 2: FragColor.rgb = output_color.rrr; break;
            case 3: FragColor.rgb = vec3(1.0) - output_color.rrr; break;
            case 4: FragColor.rgb = output_color.ggg; break;
            case 5: FragColor.rgb = vec3(1.0) - output_color.ggg; break;
            case 6: FragColor.rgb = output_color.bbb; break;
            case 7: FragColor.rgb = vec3(1.0) - output_color.bbb; break;
        }
    }
    else
    {
        switch (operand)
        {
            case 0: FragColor.rgb = output_color.rgb; break;
            case 1: FragColor.rgb = vec3(1.0) - output_color.rgb; break;
            case 2: FragColor.rgb = output_color.aaa; break;
            case 3: FragColor.rgb = vec3(1.0) - output_color.aaa; break;
            case 4: FragColor.rgb = output_color.rrr; break;
            case 5: FragColor.rgb = vec3(1.0) - output_color.rrr; break;
            case 8: FragColor.rgb = output_color.ggg; break;
            case 9: FragColor.rgb = vec3(1.0) - output_color.ggg; break;
            case 12: FragColor.rgb = output_color.bbb; break;
            case 13: FragColor.rgb = vec3(1.0) - output_color.bbb; break;
        }
    }
    if (showAlpha == 0)
        FragColor.a = 1.0;
}