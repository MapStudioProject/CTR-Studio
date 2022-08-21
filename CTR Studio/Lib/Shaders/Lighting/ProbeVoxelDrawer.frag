#version 330
in vec3 color;

out vec4 fragOutput;
out vec4 fragOutput2;

void main(){
	fragOutput = vec4(color, 1.0);
	fragOutput2 = vec4(0);
}