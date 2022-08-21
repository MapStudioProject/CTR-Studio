#version 330

uniform float Time;
uniform vec4 color;
uniform int useVertexColors;

in vec4 fColor;
in vec2 fragUV;

out vec4 fragColor;

void main() {
   if (useVertexColors == 1)
      fragColor = fColor;
   else
      fragColor = color;
}