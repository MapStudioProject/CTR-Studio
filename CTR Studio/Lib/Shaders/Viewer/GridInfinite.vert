#version 330

in vec3 vPosition;

uniform mat4 mtxView;
uniform mat4 mtxProj;

out vec3 nearPoint;
out vec3 farPoint;

vec3 UnprojectPoint(float x, float y, float z, mat4 view, mat4 proj) {
    mat4 viewInv = inverse(view);
    mat4 projInv = inverse(proj);
    vec4 unprojectedPoint = viewInv * projInv * vec4(x, y, z, 1.0);
    return unprojectedPoint.xyz / unprojectedPoint.w;
}

// normal vertice projection
void main() {
    vec3 p = vPosition;
    nearPoint = UnprojectPoint(p.x, p.y, 0.0, mtxView, mtxProj).xyz; // unprojecting on the near plane
    farPoint  = UnprojectPoint(p.x, p.y, 1.0, mtxView, mtxProj).xyz; // unprojecting on the far plane
    gl_Position = vec4(p, 1.0);
}