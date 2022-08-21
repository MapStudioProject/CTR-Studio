#version 330

uniform vec4 color;

flat in vec2 stipple_start;
in vec2 stipple_pos;

uniform float  dash_width;
uniform float dash_factor;
uniform vec2 viewport_size;

out vec4 fragColor;

void main()
{
    vec2  dir  = (stipple_pos.xy-stipple_start.xy) * viewport_size/2.0;
    float dist = length(dir);

    if (fract(dist / (dash_width + dash_factor)) > dash_width/(dash_width + dash_factor))
         discard; 

    fragColor = color;
}