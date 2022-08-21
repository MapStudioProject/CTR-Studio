#version 330 core
in vec2 TexCoords;

uniform sampler2DShadow depthTexture;
uniform float near_plane;
uniform float far_plane;

out vec4 FragColor;

// required when using a perspective projection matrix
float LinearizeDepth(float depth)
{
    float z = depth * 2.0 - 1.0; // Back to NDC 
    return (2.0 * near_plane * far_plane) / (far_plane + near_plane - z * (far_plane - near_plane));	
}

void main()
{
    float depthValue = texture(depthTexture, vec3(TexCoords, 1.0));
    FragColor = vec4(vec3(depthValue), 1.0);
}