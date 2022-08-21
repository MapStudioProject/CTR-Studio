#version 330

in vec2 TexCoords;

uniform float uBrightness;
uniform float uSaturation;
uniform float uGamma;
uniform float uHue;

uniform vec4 uCurve0[256];
uniform vec4 uCurve1[256];

uniform vec4 uToycamLevel1;
uniform vec4 uToycamLevel2;

#define fragData(index, color) { fragOutput ## index = vec4(color, 1.0); }

vec3 SetCurve(vec3 color)
{
    color *= 255; //0x40E00000U
    vec3 findex = floor(color);
    ivec3 iindex = ivec3(findex);

    vec3 color0 = vec3(uCurve0[iindex.x].x, uCurve0[iindex.y].y, uCurve0[iindex.z].z);
    vec3 color1 = vec3(uCurve1[iindex.x].x, uCurve1[iindex.y].y, uCurve1[iindex.z].z);

    return mix(color0, color1, (color - findex));
}

vec3 SetBrightness(vec3 color, float amount)
{
    return color * vec3(amount);
}

vec3 SetGamma(vec3 color, float gamma)
{
    return pow(color.rgb, vec3(1.0/gamma));
}

vec3 SetSaturation(vec3 color, float adjustment)
{
    const vec3 W = vec3(0.2125, 0.7154, 0.0721);
    vec3 intensity = vec3(dot(color, W));
    return mix(intensity, color, adjustment);
}

//https://gist.github.com/mairod/a75e7b44f68110e1576d77419d608786
vec3 SetHueShift( vec3 color, float hue ){

    const vec3 k = vec3(0.57735, 0.57735, 0.57735);
    float cosAngle = cos(hue);
    return vec3(color * cosAngle + cross(k, color) * sin(hue) + k * dot(k, color) * (1.0 - cosAngle));
}

vec3 SetToycam( vec3 color ){
    color = pow(color, uToycamLevel1.rgb);
    color = pow(color, uToycamLevel2.rgb);
    return color;
}

const int LUT_SIZE = 8;

void main()
{  
    float depth = 1.0 / (LUT_SIZE - 1);

    vec3 color_in = vec3(TexCoords.x, TexCoords.y, 0.0);

    for (int i = 0; i < 8; i++)
    {
        vec3 color = color_in;
        color.rgb = SetBrightness(color.rgb, uBrightness);
        color.rgb = SetSaturation(color.rgb, uSaturation);
        color.rgb = SetHueShift(color.rgb, uHue);
        //color.rgb = SetToycam(color.rgb);
        color.rgb = clamp(SetGamma(color.rgb, uGamma), 0, 1);
        color.rgb = clamp(SetCurve(color.rgb), 0, 1);

        gl_FragData[i] = vec4(color, 1.0);
        color_in.b += depth;
    }
}  