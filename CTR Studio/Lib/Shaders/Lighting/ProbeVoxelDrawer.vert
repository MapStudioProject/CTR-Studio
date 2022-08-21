#version 330
layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec4 vCoef0;
layout (location = 2) in vec4 vCoef1;
layout (location = 3) in vec4 vCoef2;
layout (location = 4) in vec4 vCoef3;
layout (location = 5) in vec4 vCoef4;
layout (location = 6) in vec4 vCoef5;
layout (location = 7) in vec4 vCoef6;

out vec3 color;

uniform mat4 mtxCam;

vec3 calculateSH(
    vec3 normal,
    vec4 sh00,
    vec4 sh01,
    vec4 sh02,
    vec4 sh10,
    vec4 sh11,
    vec4 sh12,
    vec4 sh2)
{
    return vec3(0.0);
}


void main(){    
	gl_Position = mtxCam * vec4(vPosition.xyz, 1);
    color = calculateSH(vec3(1.0),
                        vCoef0,
                        vCoef1,
                        vCoef2,
                        vCoef3,
                        vCoef4,
                        vCoef5,
                        vCoef6);
}