#version 330 core

out vec4 FragColor;
in vec2 TexCoords;

uniform sampler2D textureData;
uniform float hdrIntensity;

vec4 LinearToRGBM(vec4 value, float maxRange ) {
	float maxRGB = max( value.r, max( value.g, value.b ) );
	float M = clamp( maxRGB / maxRange, 0.0, 1.0 );
	M = ceil( M * 255.0 ) / 255.0;
	return vec4( value.rgb / ( M * maxRange ), M );
}

void main()
{		
    vec4 hdrColor = texture(textureData, TexCoords);
	float gray = dot(hdrColor.rgb, vec3(0.2125, 0.7154, 0.0721));
	float M = clamp( gray / hdrIntensity, 0.0, 1.0 );
	M = ceil( M * 255.0 ) / 255.0;

    FragColor = vec4( hdrColor.rgb / M, M );
}	