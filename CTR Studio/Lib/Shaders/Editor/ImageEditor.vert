#version 330
in vec2 vPosition;
in vec2 vTexCoord0;

uniform mat4 mtxMdl;
uniform mat4 mtxCam;

uniform vec2 scale;
uniform vec2 texCoordScale;

out vec2 TexCoords;

void main()
{
   vec2 scaleCenter = vec2(0.5);

    gl_Position = mtxCam * (vec4(vPosition.x, vPosition.y, 0.0, 1.0) * vec4(scale, 1, 1)); 
    TexCoords = (vTexCoord0 - scaleCenter) * texCoordScale + scaleCenter;
}