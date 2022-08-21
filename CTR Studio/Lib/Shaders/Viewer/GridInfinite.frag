#version 330

uniform int solidFloor;
uniform int spotLight;

uniform float znear;
uniform float zfar;

uniform vec3 gridColor;

uniform mat4 mtxView;
uniform mat4 mtxProj;

in vec3 nearPoint; 
in vec3 farPoint;

out vec4 fragColor;

float checkerboard(vec2 fragPos, float scale) {
	return float((
		int(floor(fragPos.x / scale)) +
		int(floor(fragPos.y / scale))
	) % 2);
}

vec4 grid(vec3 fragPos3D, float scale, bool drawAxis) {
    vec2 coord = fragPos3D.xz * scale;
    vec2 derivative = fwidth(coord);
    vec2 grid = abs(fract(coord - 0.5) - 0.5) / derivative;
    float line = min(grid.x, grid.y);
    vec4 color = vec4(gridColor, 1.0 - min(line, 1.0));
    return color;
}

vec4 axisLines(vec3 fragPos3D)
{    vec2 coord = fragPos3D.xz * 10;
    vec2 derivative = fwidth(coord);
    float fadeDist = 100;

    float minimumz = min(derivative.y, fadeDist);
    float minimumx = min(derivative.x, fadeDist);
    vec4 color = vec4(0, 0, 0, 0);
    // z axis
    if(fragPos3D.x > -0.1 * minimumx && fragPos3D.x < 0.1 * minimumx)
        color.z = 1.0;
    // x axis
    if(fragPos3D.z > -0.1 * minimumz && fragPos3D.z < 0.1 * minimumz)
        color.x = 1.0;
    return color;
}

float computeDepth(vec3 pos) {
    //Calculate position in clip space
    vec4 clip_space_pos = mtxProj * mtxView * vec4(pos.xyz, 1.0);
    //Get clip space depth
    float clip_space_depth = (clip_space_pos.z / clip_space_pos.w);
    //Compute the range based on gl_DepthRange settings
	float d_far = gl_DepthRange.far;
	float d_near = gl_DepthRange.near;

	float depth = (((d_far-d_near) * clip_space_depth) + d_near + d_far) / 2.0;
	return depth;
}

float computeLinearDepth(vec3 pos) {
    vec4 clip_space_pos = mtxProj * mtxView * vec4(pos.xyz, 1.0);
    float clip_space_depth = (clip_space_pos.z / clip_space_pos.w) * 2.0 - 1.0; // put back between -1 and 1
    float linearDepth = (2.0 * znear * zfar) / (zfar + znear - clip_space_depth * (zfar - znear)); // get linear value between 0.01 and 100
    return linearDepth / zfar; // normalize
}

void main() {
   float t = -nearPoint.y / (farPoint.y - nearPoint.y);
    vec3 fragPos3D = nearPoint + t * (farPoint - nearPoint);

    gl_FragDepth = computeDepth(fragPos3D);

    float linearDepth = computeLinearDepth(fragPos3D);
    float fading = max(0, (0.5 - linearDepth));

    vec4 color = vec4(0);
    if (solidFloor == 1)
    {
        //https://github.com/martin-pr/possumwood/wiki/Infinite-ground-plane-using-GLSL-shaders

        //The overall scale of the floor tile
        float size = 10;

	    float c =
		    checkerboard(fragPos3D.xz, 1   * size) * 0.3 +
		    checkerboard(fragPos3D.xz, 10  * size) * 0.2 +
		    checkerboard(fragPos3D.xz, 100 * size) * 0.1 +
		    0.1;

        color = vec4(vec3(c), 1);
        if (spotLight == 0)
	        color = vec4(vec3(c/2.0 + 0.3), 1);
    }
    else
    {
        //Scale down the grid size.
        float size = 0.01;
        vec4 axis = axisLines(fragPos3D);

        // adding multiple resolution for the grid
        color =  grid(fragPos3D, size * 10, true);
        color += grid(fragPos3D, size * 1, true);
        color += axis;
    }

    if (spotLight == 1)
    {
	    float spotlight_output = min(1.0, 1.5 - 0.02*length(fragPos3D.xz));
	    fragColor = vec4(color.rgb*spotlight_output, 1);
    }
    else
    {
	    fragColor = color * float(t > 0);
        fragColor.a *= fading;
    }
}