#version 330

in vec3 vPositon;
in vec3 vNormal;
in vec2 vTexCoord0;
in vec2 vTexCoord1;
in vec2 vTexCoord2;
in vec4 vColor;
in ivec4 vBoneIndex;
in vec4 vBoneWeight;
in vec3 vTangent;
in vec3 vBitangent;

out vec2 f_texcoord0;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;
uniform mat4 mtxLightVP;

// Skinning uniforms
uniform mat4 bones[170];
uniform int SkinCount;
uniform int UseSkinning;
uniform int BoneIndex;
uniform mat4 RigidBindTransform;

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
    vec4 worldPosition = vec4(vPositon.xyz, 1);

    //Vertex Rigging
    if (UseSkinning == 1) //Animated object using the skeleton
    {
        ivec4 index = vBoneIndex;
        //Apply skinning to vertex position and normal
	    if (SkinCount > 0)
		    worldPosition = skin(worldPosition.xyz, index);
        //Single bind models that have no skinning to the bone they are mapped to
        if (SkinCount == 0)
        {
            worldPosition = RigidBindTransform * worldPosition;
        }
    }

    gl_Position = mtxLightVP * mtxMdl * worldPosition;
   f_texcoord0 = vTexCoord0;
}