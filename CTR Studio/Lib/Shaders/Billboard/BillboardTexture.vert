#version 330

layout(location = 0) in vec3 vPosition;
layout(location = 2) in vec2 vTexCoord0;

uniform vec3 scale;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;
uniform mat4 mtxView;
uniform mat4 mtxProj;

out vec2 f_texcoord0;

void main()
{
    f_texcoord0 = vTexCoord0;

    //Toggle verticle view rotation on the Y axis
    bool vertical = true;

    mat4 modelView = mtxView * mtxMdl;

    //Remove the rotations from the model view matrix and re add the model scale
    //The billboard will instead use the view rotation.

    // Column 0:
    modelView[0][0] = scale.x; 
    modelView[0][1] = 0.0; 
    modelView[0][2] = 0.0; 

    // Column 1:
    if (vertical) {
        modelView[1][0] = 0.0; 
        modelView[1][1] = scale.y; 
        modelView[1][2] = 0.0;
    }

    // Column 2:
    modelView[2][0] = 0.0;
    modelView[2][1] = 0.0;
    modelView[2][2] = scale.z;

    //Billboard output
    vec4 P = modelView * vec4(vPosition.xyz, 1);
    gl_Position = mtxProj * P;
}