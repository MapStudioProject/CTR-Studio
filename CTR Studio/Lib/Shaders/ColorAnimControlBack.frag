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
    
    float top = size.y-35.0;
    
    float frame = map(fragCoord.x, 0.0, size.x, frameRange.x, frameRange.y);
    

    //fetch curve data
    vec4 col = texture(tex, fragCoord/size);
    
    vec4 colN = texture(tex, (fragCoord+vec2(2.0,0.0))/size);
    vec4 colP = texture(tex, (fragCoord-vec2(2.0,0.0))/size);
    

    //calculate derivatives
    vec4 colTanOut = (colN-col) * size.y/(valueRange.y-valueRange.x);
    vec4 colTanIn = (col-colP) * size.y/(valueRange.y-valueRange.x);

    vec4 col2ndDeriv = colTanIn - colTanOut;

    vec4 colD = max(vec4(1.0), abs(colTanIn + colTanOut)/4.0);
    


    //calculate curves
    vec4 valPosY;
    
    valPosY.x = map(col.r, valueRange.x, valueRange.y, bottom, top);
    valPosY.y = map(col.g, valueRange.x, valueRange.y, bottom, top);
    valPosY.z = map(col.b, valueRange.x, valueRange.y, bottom, top);
    valPosY.w = map(col.a, valueRange.x, valueRange.y, bottom, top);

    vec4 curves;

    float width1Blend;

    #define WIDTH1_BLEND(c, val) clamp(sign(fragCoord.y-val)*col2ndDeriv.c*0.02, 0.0,1.0)

    #define CURVE(c, thickness, val) smoothstep(mix(colD.c,1.0,(width1Blend = WIDTH1_BLEND(c, val)))*thickness,mix(colD.c,1.0,width1Blend)*0.25,abs(val-fragCoord.y));

    
    curves.x = CURVE(x, 1.75,valPosY.x);
    curves.y = CURVE(y, 1.75,valPosY.y);
    curves.z = CURVE(z, 1.75,valPosY.z);
    curves.w = CURVE(w, 1.75,valPosY.w);
    
    float curveShad = CURVE(ACTIVE,3.0,(valPosY.ACTIVE-1.5));
    
    




    //draw background (color gradient)
    col.ACTIVE = max(0.0, map(fragCoord.y, bottom, top, valueRange.x, valueRange.y));

    col = clamp(col, vec4(0.0), vec4(10.0, 10.0, 10.0, 1.0));
    
    float checker = mix(0.5, 0.75, float((mod(fragCoord.x, 20.0) > 10.0) != (mod(fragCoord.y, 20.0) > 10.0)));

    gl_FragColor = mix(vec4(checker), clamp(NormalColor(col), vec4(0.0), vec4(1.0)), col.a);


    float value = map(fragCoord.y, bottom, top, valueRange.x, valueRange.y);

    gl_FragColor = mix(gl_FragColor, backgroundColor, backgroundColor.a);

    gl_FragColor = clamp(mix(gl_FragColor, vec4(0), 0.125*float(mod(value,2.0)>1.0)*backgroundColor.a), vec4(0.0), vec4(1.0));







    
    //draw time line (lines)
    float shiftedX = fragCoord.x + frameRange.x /(frameRange.y-frameRange.x) * size.x;
    
    
    float spacing = pow(10,ceil(lineScaleLog))/10.0 / (frameRange.y-frameRange.x) * size.x;
    
    
    float weight = mix(fract(-lineScaleLog) * 0.5, 0.5, float(mod(round(shiftedX/spacing), 10.0) == 0.0));
    
    float timeLines = smoothstep(4.5*weight,0.5*weight,abs(mod(shiftedX+5.0, spacing)-5.0))*weight;

    gl_FragColor = mix(gl_FragColor, vec4(1.0), timeLines);
    
    




    //animation curves/graphs
    gl_FragColor = mix(gl_FragColor, vec4(1.0), curves.x * 0.25);
    gl_FragColor = mix(gl_FragColor, vec4(1.0), curves.y * 0.25);
    gl_FragColor = mix(gl_FragColor, vec4(1.0), curves.z * 0.25);
    gl_FragColor = mix(gl_FragColor, vec4(1.0), curves.w * 0.25);
    
    //active curve
    mat4 activeColorMat = mat4(
        activeColorRed,
        activeColorGreen,
        activeColorBlue,
        activeColorAlpha);

    vec4 activeColorSel = vec4(0.0);
    activeColorSel.ACTIVE += 1.0;

    gl_FragColor = mix(gl_FragColor, vec4(0.0), curveShad*0.25);
   // gl_FragColor = mix(gl_FragColor, activeColorMat*activeColorSel, curves.ACTIVE);






    //draw out of bounds darker
    float firstFrameX = map(0, frameRange.x, frameRange.y, 0.0, size.x);
    float lastFrameX = map(frameCount, frameRange.x, frameRange.y, 0.0, size.x);

    float middle = (firstFrameX + lastFrameX) * 0.5;
    float halfSpan = (lastFrameX - firstFrameX) * 0.5;

    gl_FragColor = mix(gl_FragColor, vec4(vec3(0.0), 1.0), 0.5*smoothstep(0.0,0.0,abs(fragCoord.x-middle)-halfSpan));




    gl_FragColor = mix(gl_FragColor, vec4(vec3(0.0), 1.0), 0.5*smoothstep(0.0,0.0,40.0-fragCoord.x));





    //current frame indicator
    float currentFrameX = map(currentFrame,frameRange.x,frameRange.y,0.0,size.x);

    gl_FragColor = mix(gl_FragColor,  vec4(0.0), (1-pow(min(abs((currentFrameX-fragCoord.x-4.0)*0.125), 1.0), 0.125))*0.25);
    gl_FragColor = mix(gl_FragColor,  vec4(1.0, 0.9, 0.1, 1.0), 1-pow(min(abs((currentFrameX-fragCoord.x)*0.125), 1.0), 0.125));




    //UI frame
    vec2 halfSize = size.xy/2.0;
    
    float dBox = sdBox(fragCoord-halfSize+vec2(0.0,15.0), halfSize-vec2(10.0, 25.0))-10.0;
    
    gl_FragColor = mix(gl_FragColor, vec4(0.0), smoothstep(-3.5,-1.0,dBox));
    gl_FragColor = mix(gl_FragColor, controlBackColor, smoothstep(-2.5,-1.0,dBox));



    //frame bar
    gl_FragColor = mix(gl_FragColor, frameBarBackColor, smoothstep(size.y-31.0,size.y-30.0,fragCoord.y));


    float thinTimeLines = smoothstep(1.5,0.5,abs(mod(shiftedX+5.0, spacing)-5.0))*weight;

    gl_FragColor = mix(gl_FragColor, vec4(1.0), thinTimeLines * smoothstep(size.y-5.0-weight*4.0,size.y-3.0-weight*4.0,fragCoord.y)*0.5);






    //per frame color preview bar
    float closestFrame = round(frame);


    float frameMiddle = map(closestFrame,frameRange.x,frameRange.y,0.0,size.x);

    col = texture(tex, vec2(frameMiddle/size.x, 0.0));

    col = mix(col, leftFrameColor, float(closestFrame<frameRange.x));
    col = mix(col, rightFrameColor, float(closestFrame>frameRange.y));

    col = clamp(col, vec4(0.0), vec4(10.0, 10.0, 10.0, 1.0));

    col = mix(vec4(checker), NormalColor(col), col.a);

    gl_FragColor = mix(gl_FragColor,  col, smoothstep(3.5,3.0,abs(size.y-25.0-fragCoord.y))*0.0);


    gl_FragColor.a = 1.0;
}