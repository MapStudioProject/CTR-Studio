#version 330 core

precision highp float;

uniform vec2 pixelSize;
uniform vec4 highlight_color;
uniform vec4 outline_color;

out vec4 FragColor;
  
in vec2 TexCoords;

uniform int ENABLE_BLOOM;
uniform int ENABLE_LUT;
uniform int ENABLE_SRGB;
uniform int ENABLE_FBO_ALPHA;

uniform sampler2D uColorTex;
uniform sampler2D uBloomTex;
uniform sampler2D uHighlightTex;

const float LUT_SIZE = 8.0;

const float GAMMA = 2.2;

vec4 SelectionColor(vec4 color)
{
    //alpha value refers to a mesh being selected or not
    //0.0 for not selected
    float mask = texture(uHighlightTex, TexCoords).a;
    float alpha = 0.0;

    //Full mesh selection mask.
    if (mask != 0.0) 
    {
        //Highlight the entire mesh.
        float hc_a = highlight_color.a;
	    return vec4(color.rgb * (1-hc_a) + highlight_color.rgb * hc_a, color.a);
    }
    else
    {
        //Create an outline
        float width = 4.0;
	    float a = -4.0 * color.a;
        //Add all the lookups of the selection mask alpha channel for edge checking
	    a += texture(uHighlightTex, TexCoords + width * vec2( pixelSize.x, 0.0)).a;
	    a += texture(uHighlightTex, TexCoords + width * vec2(-pixelSize.x, 0.0)).a;
	    a += texture(uHighlightTex, TexCoords + width * vec2( 0.0, pixelSize.y)).a;
	    a += texture(uHighlightTex, TexCoords + width * vec2( 0.0,-pixelSize.y)).a;
        //Check if it reaches the required threshold
		if (color.a < 1.0 && a > 0.0)
			return outline_color; //The outline color
		else
			return color; //Normal color (non selected area)
    }
}

void main()
{             
    vec4 hdrColor = texture(uColorTex, TexCoords).rgba;
    vec4 highlightTex = texture(uHighlightTex, TexCoords).rgba;

    vec3 outputColor = hdrColor.rgb;

    if (ENABLE_BLOOM == 1)
    {
        vec3 bloomColor = texture(uBloomTex, TexCoords).rgb;
        //Add bloom post effects
        outputColor += bloomColor;
    }
    if (ENABLE_SRGB == 1)
    {
        outputColor.rgb = pow(outputColor.rgb, vec3(1.0/GAMMA));
    }
    FragColor.rgb = outputColor.rgb;

    FragColor = SelectionColor(FragColor);

    //Used for keeping alpha information if needed
    if (ENABLE_FBO_ALPHA == 1)
        FragColor.a = hdrColor.a;
    else
        FragColor.a = 1.0;
}