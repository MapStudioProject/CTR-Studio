#version 330

uniform sampler2D screenTexture;

in vec2 TexCoords;

uniform int isSRGB;

out vec4 fragOutput;

void main()
{  
    vec4 color = texture(screenTexture, TexCoords);
    fragOutput = vec4(color.rgb, color.a);

    if (isSRGB == 1)
        fragOutput.rgb = pow(fragOutput.rgb, vec3(1.0/2.2));
}  