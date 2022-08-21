#version 330
in vec3 vPosition;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

void main(){
	gl_Position = mtxMdl * mtxCam * vec4(vPosition.xyz, 1);
}