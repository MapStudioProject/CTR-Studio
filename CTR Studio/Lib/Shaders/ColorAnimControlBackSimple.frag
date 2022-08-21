#ifndef ACTIVE

#define ACTIVE x

#endif

uniform vec2 size;
uniform vec2 frameRange;
uniform vec2 valueRange = vec2(0.0,5.0);

uniform float lineScaleLog = 1.0;

uniform float frameCount = 100.0;

uniform sampler2D tex;

uniform vec4 leftFrameColor;
uniform vec4 rightFrameColor;

uniform float currentFrame;

//use for customization
uniform vec4 backgroundColor = vec4(0.0); //the background color of the color/curve view (the alpha controls how much it draws over the color gradient)

uniform vec4 controlBackColor = vec4(0.25); //the background color of the actual control for seemless transition (get's set automatically)

uniform vec4 frameBarBackColor = vec4(0.125); //the back color of the frame bar (the thing where it displays the frame numbers

uniform float frameBarColorVisibility = 1.0; //how prominent should the frame preview colors be on the frame bar

uniform vec4 activeColorRed = vec4(1.0,0.8,0.8,1.0); //the color of an active Red/X-curve
uniform vec4 activeColorGreen = vec4(0.8,1.0,0.8,1.0); //the color of an active Green/Y-curve
uniform vec4 activeColorBlue = vec4(0.8,0.8,1.0,1.0); //the color of an active Blue/Z-curve
uniform vec4 activeColorAlpha = vec4(1.0,1.0,1.0,1.0); //the color of an active Alpha/W-curve


in vec2 fragCoord;

layout(location = 0) out vec4 FragColor;

float sdBox( vec2 p, vec2 b )
{
  vec2 q = abs(p) - b;
  return length(max(q,0.0)) + min(max(q.x,q.y),0.0);
}

float map(float v, float minIn, float maxIn, float minOut, float maxOut){
    return (v-minIn)/(maxIn-minIn)*(maxOut-minOut)+minOut;
}

vec4 NormalColor(vec4 col){
    return mix(col, vec4(1.0), clamp(log(length(col.rgb))*0.3, 0.0, 1.0));
}

void main()
{
    float bottom = 5.0;
    float top = size.y-25.0;

    float frame = map(fragCoord.x, 0.0, size.x, frameRange.x, frameRange.y);
    vec4 col = vec4(1);

    //draw background
    float value = map(fragCoord.y, bottom, top, 0, 1.0);

    FragColor = mix(FragColor, backgroundColor, backgroundColor.a);
    FragColor = clamp(mix(FragColor, vec4(0), 0.125*float(mod(value,2.0)>1.0)*1.0)*backgroundColor.a, vec4(0.0), vec4(1.0));
    

    //draw time line (lines)
    float shiftedX = fragCoord.x + frameRange.x /(frameRange.y-frameRange.x) * size.x;
    float spacing = pow(10,ceil(lineScaleLog))/10.0 / (frameRange.y-frameRange.x) * size.x;
    float weight = mix(fract(-lineScaleLog) * 0.5, 0.5, float(mod(round(shiftedX/spacing), 10.0) == 0.0));
    float timeLines = smoothstep(4.5*weight,0.5*weight,abs(mod(shiftedX+5.0, spacing)-5.0))*weight;

    FragColor = mix(FragColor, vec4(0.3), timeLines);

    //draw out of bounds darker
    float firstFrameX = map(0, frameRange.x, frameRange.y, 0.0, size.x);
    float lastFrameX = map(frameCount, frameRange.x, frameRange.y, 0.0, size.x);

    float middle = (firstFrameX + lastFrameX) * 0.5;
    float halfSpan = (lastFrameX - firstFrameX) * 0.5;

    FragColor = mix(FragColor, vec4(vec3(0.0), 1.0), 0.5*smoothstep(0.0,0.0,abs(fragCoord.x-middle)-halfSpan));


    //current frame indicator
    float currentFrameX = map(currentFrame,frameRange.x,frameRange.y,0.0,size.x);

    FragColor = mix(FragColor,  vec4(0.0), (1-pow(min(abs((currentFrameX-fragCoord.x-4.0)*0.125), 1.0), 0.125))*0.25);
    FragColor = mix(FragColor,  vec4(1.0, 0.9, 0.1, 1.0), 1-pow(min(abs((currentFrameX-fragCoord.x)*0.125), 1.0), 0.125));


    //UI frame
    vec2 halfSize = size.xy/2.0;
    
    float dBox = sdBox(fragCoord-halfSize+vec2(0.0,15.0), halfSize-vec2(10.0, 15.0))-10.0;
    

    FragColor = mix(FragColor, vec4(0.0), smoothstep(-3.5,-1.0,dBox));
    FragColor = mix(FragColor, controlBackColor, smoothstep(-2.5,-1.0,dBox));

    //frame bar
    FragColor = mix(FragColor, frameBarBackColor, smoothstep(size.y-21.0,size.y-20.0,fragCoord.y));

    float thinTimeLines = smoothstep(1.5,0.5,abs(mod(shiftedX+5.0, spacing)-5.0))*weight;

    FragColor = mix(FragColor, vec4(1.0), thinTimeLines * smoothstep(size.y-5.0-weight*4.0,size.y-3.0-weight*4.0,fragCoord.y)*0.5);

    FragColor.a = 1.0;
}