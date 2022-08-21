#version 330

in vec2 f_texcoord0;
in vec3 f_normal;
in vec4 f_color;

uniform sampler2D textureMap;

uniform int displayVertexColors;
uniform int hasVertexColors;
uniform int hasTextures;
uniform int halfLambert;
uniform int directionalLighting;
uniform vec4 color;
uniform vec4 highlight_color;
uniform vec3 difLightDirection;
uniform int isSRGB;

out vec4 fragColor;

const float GAMMA = 2.2;

void main()
{
	fragColor = color;

	 if (hasTextures == 1)
		fragColor *= texture(textureMap, f_texcoord0).rgba;
	if (halfLambert == 1 && directionalLighting == 0)
	{
		 vec3 displayNormal = (f_normal * 0.5) + 0.5;
		 float halfLambert = max(displayNormal.y,0.5);
		 fragColor.rgb = fragColor.rgb * halfLambert;
	}
	if (halfLambert == 1 && directionalLighting == 1)
	{
		float halfLambert = dot(f_normal, difLightDirection) * 0.5 + 0.5;
		fragColor.rgb *= vec3(halfLambert); 
	}
	if (hasVertexColors == 1)
	{
		fragColor.rgb *= f_color.rgb;
		fragColor.a *= f_color.a;
	}

	if (displayVertexColors == 1)
		fragColor.rgba *= f_color.rgba;

    if (isSRGB == 1)
        fragColor.rgb = pow(fragColor.rgb, vec3(1.0/GAMMA));

	if (highlight_color.w > 0.0)
	{
		//Highlight intensity for object selection
		float hc_a   = highlight_color.w;
		fragColor = vec4(fragColor.rgb * (1-hc_a) + highlight_color.rgb * hc_a, fragColor.a);
	}
}