#version 150

noperspective in vec3 edgeDistance;

out vec4 FragColor;

float WireframeIntensity(vec3 distanceToEdges)
{
    float minDistance = min(min(distanceToEdges.x, distanceToEdges.y), distanceToEdges.z);

    // Constant wireframe thickness relative to the screen size.
    float thickness = 0.01;
    float smoothAmount = 1;

    float delta = fwidth(minDistance);
    float edge0 = delta * thickness;
    float edge1 = edge0 + (delta * smoothAmount);
    float smoothedDistance = smoothstep(edge0, edge1, minDistance);

    return 1 - smoothedDistance;
}

void main()
{
  float amount = WireframeIntensity(edgeDistance);
  fragColor.rgb =  vec3(1.0) * amount;
  fragColor.a = 1.0;
}