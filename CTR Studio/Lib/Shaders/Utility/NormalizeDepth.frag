#version 430 core

uniform sampler2D depthTexture;

uniform float near;
uniform float far;

in vec2 TexCoords;

out vec4 fragOutput;

float linearDepth(float depth)
{
   float z = depth * 2.0 - 1.0; // back to NDC 
    return (2.0 * near * far) / (far + near - z * (far - near));	
}

void main()
{             
   float depth = texture(depthTexture, TexCoords).r;
   float linear = linearDepth(depth) / far;

   float cbuf_0 = 1.00001e-05f;
   float cbuf_1 = 0.99999f;

    float tmp5 = fma(depth, -cbuf_1, 1.0);
    float tmp8 = fma(1.0 / tmp5, cbuf_0, -cbuf_0);

    float ProjectionA = (far + near) / (far - near);
    float ProjectionB = (2 * near) / (far - near);
    float linearDepth = ProjectionB / (depth - ProjectionA);

    float z = (2.0 * near) / (far + near - depth * (far - near));
   fragOutput = vec4(vec3(depth), 1.0);
}  