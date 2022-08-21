#define ACTIVE x

uniform vec2 size;
uniform vec2 pointPos;
uniform vec2 cpInPos;
uniform vec2 cpOutPos;

float sdBox( vec2 p, vec2 b )
{
  vec2 q = abs(p) - b;
  return length(max(q,0.0)) + min(max(q.x,q.y),0.0);
}

float sdLine( vec2 p, vec2 a, vec2 b, float r )
{
  vec2 pa = p - a, ba = b - a;
  float h = clamp( dot(pa,ba)/dot(ba,ba), 0.0, 1.0 );
  return length( pa - ba*h ) - r;
}


float sdHandle(vec2 fragCoord){
    float dHandle = length(fragCoord - pointPos)-5.0;

    dHandle = min(dHandle, length(fragCoord - cpInPos)-3.0);
    dHandle = min(dHandle, length(fragCoord - cpOutPos)-3.0);

    dHandle = min(dHandle, sdLine(fragCoord, pointPos, cpInPos, 0.25));
    dHandle = min(dHandle, sdLine(fragCoord, pointPos, cpOutPos, 0.25));

    return dHandle;
}

void main()
{
    vec2 fragCoord = gl_FragCoord.xy;
    
    gl_FragColor = mix(gl_FragColor, vec4(vec3(0.0), 0.5), smoothstep(3.5,0.0,sdHandle(fragCoord+vec2(0.0,1.5)))*0.25);

    gl_FragColor = mix(gl_FragColor, vec4(vec3(1.0), 1.0), smoothstep(1.5,0.0,sdHandle(fragCoord)));
    
    //UI frame
    vec2 halfSize = size.xy/2.0;
    
    float dBox = sdBox(fragCoord-halfSize+vec2(0.0,15.0), halfSize-vec2(10.0, 25.0))-10.0;
    
    gl_FragColor = mix(gl_FragColor, vec4(0.0), smoothstep(-2.5,-1.0,dBox));
}