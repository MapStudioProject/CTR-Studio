#version 330

uniform sampler2D u_TextureAlbedo0;
uniform int hasAlpha;
uniform int hasDiffuseMap;

in vec2 f_texcoord0;

out vec4 fragOutput;

void main()
{
	if (hasAlpha == 1 && hasDiffuseMap == 1)
	{
		float alpha = texture(u_TextureAlbedo0, f_texcoord0).a;
		if (alpha < 0.5)
			discard;
	}
}  