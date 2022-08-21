#version 330

in vec4 normal;
in vec2 texCoords0;

out vec4 fragOutput;

uniform sampler2D texture0;
uniform sampler2D texture1;
uniform sampler2D texture2;

uniform int textureCount;

void main(){
    fragOutput = vec4(1);
    if (textureCount > 0)
        fragOutput = texture(texture0, texCoords0);
}