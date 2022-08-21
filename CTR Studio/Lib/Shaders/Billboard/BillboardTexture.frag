#version 330

in vec2 f_texcoord0;

uniform sampler2D textureMap;

uniform int useTexture;
uniform vec4 color;
uniform vec4 highlight_color;

out vec4 fragColor;

void main()
{
    if (useTexture == 0)
		fragColor = color;
	else
		fragColor = texture(textureMap, f_texcoord0).rgba;

	if (highlight_color.w > 0.0)
	{
		//Highlight intensity for object selection
		float hc_a   = highlight_color.w;
		fragColor = vec4(fragColor.rgb * (1-hc_a) + highlight_color.rgb * hc_a, fragColor.a);
	}
}