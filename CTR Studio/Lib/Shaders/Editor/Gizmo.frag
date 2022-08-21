#version 330

uniform vec4 color;
uniform vec4 selectionColor;

out vec4 fragOutput;

void main()
{             
   fragOutput = color;
   fragOutput.rgb = fragOutput.rgb * (1 - selectionColor.a) + selectionColor.rgb * selectionColor.a;
}  