#version 330 core

layout(location = 0) in vec3 vPosition;
layout(location = 1) in vec3 vNormal;

out VS_OUT {
    vec3 normal;
} vs_out;


uniform mat4 mtxMdl;
uniform mat4 mtxCam;

uniform mat4 mtxProj;
uniform mat4 camMtx;

void main()
{
	vec3 normal = vNormal;

	normal = normalize(mat3(mtxMdl) * normal.xyz);

	gl_Position = mtxCam * mtxMdl * vec4(vPosition.xyz, 1);
    mat3 normalMatrix = mat3(transpose(inverse(camMtx * mtxMdl)));
    vs_out.normal = normalize(vec3(mtxProj * vec4(normalMatrix * normal, 0.0)));
}