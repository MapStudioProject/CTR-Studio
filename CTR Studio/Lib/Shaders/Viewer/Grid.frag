#version 330
uniform vec4 gridColor;
uniform int linearToSrgb;

out vec4 fragOutput;

vec4 fromLinear(vec4 linearRGB)
{
    bvec4 cutoff = lessThan(linearRGB, vec4(0.0031308));
    vec4 higher = vec4(1.055)*pow(linearRGB, vec4(1.0/2.4)) - vec4(0.055);
    vec4 lower = linearRGB * vec4(12.92);

    return mix(higher, lower, cutoff);
}

void main(){
	fragOutput = gridColor;
    if (linearToSrgb == 1)
	    fragOutput = fromLinear(gridColor);
}