#version 330

uniform sampler2D screenTexture;
uniform float bloom_intensity;

in vec2 TexCoords;

out vec4 fragOutput;

void main()
{  
    vec4 color = texture(screenTexture, TexCoords);
    float brightness = dot(color.rgb, vec3(0.2126, 0.7152, 0.0722));

    fragOutput = vec4(0);
    if (brightness > 1.0)
        fragOutput = vec4(color.rgb, 1.3) * bloom_intensity;
}  