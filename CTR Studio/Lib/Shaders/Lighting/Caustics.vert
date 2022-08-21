#version 330
layout (location = 0) in vec3 vPositon;

uniform mat4 mtxCam;
uniform mat4 mtxMdl;
uniform mat4 mtxView;
uniform mat4 mtxProj;

uniform vec4 viewParams;
uniform vec2 viewSize;

out vec4 clipSpacePos;
out vec4 position;
out vec2 FovScale;

uniform float fov_x;
uniform float fov_y;
uniform float clipRange;
uniform float clipDiv;

void main()
{
    clipSpacePos = mtxProj * mtxView * vec4(vPositon, 1.0);
    position = vec4(vPositon, 1.0);
        //Flip Y as the LPP is flipped
    clipSpacePos.y = -clipSpacePos.y;

    gl_Position = clipSpacePos; 

    FovScale.x = tan(fov_x / 2.0);
    FovScale.y = tan(fov_y / 2.0);
    FovScale *= 2.0; // Required to avoid the multiplication by 2.0 in the fragment shader at "vec2 P_ndc = vec2(1.0) - texture_coordinate * 2.0;".
    FovScale *= clipRange;
}