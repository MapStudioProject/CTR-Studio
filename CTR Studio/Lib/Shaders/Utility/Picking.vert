#version 330

layout (location = 0) in vec3 vPosition;
layout (location = 6) in vec4 vBoneWeight;
layout (location = 7) in ivec4 vBoneIndex;
layout (location = 15) in float vFaceIndex;

out float faceIndex;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

// Skinning uniforms
uniform mat4 bones[170];
uniform mat4 RigidBindTransform;
uniform int SkinCount;
uniform int UseSkinning;

vec4 skin(vec3 pos, ivec4 index)
{
    vec4 newPosition = vec4(pos.xyz, 1.0);
    if (SkinCount == 1) //Rigid
    {
        newPosition = bones[index.x] * vec4(pos, 1.0);
    }
    else //Smooth
    {
        newPosition = bones[index.x] * vec4(pos, 1.0) * vBoneWeight.x;
        newPosition += bones[index.y] * vec4(pos, 1.0) * vBoneWeight.y;
        newPosition += bones[index.z] * vec4(pos, 1.0) * vBoneWeight.z;
        if (vBoneWeight.w < 1) //Necessary. Bones may scale weirdly without
		    newPosition += bones[index.w] * vec4(pos, 1.0) * vBoneWeight.w;
    }
    return newPosition;
}


void main(){
    vec4 worldPosition = vec4(vPosition.xyz, 1);

    //Vertex Rigging
    if (UseSkinning == 1) //Animated object using the skeleton
    {
        ivec4 index = vBoneIndex;

        //Apply skinning to vertex position and normal
	    if (SkinCount == 0)
		    worldPosition = RigidBindTransform * worldPosition;

	    if (SkinCount > 0)
		    worldPosition = skin(worldPosition.xyz, index);
    }

    gl_Position = mtxCam * mtxMdl * worldPosition;
    faceIndex = vFaceIndex;
}