#version 330

in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec3 vertexTangent;

out vec2 fragTexCoord;
out vec3 fragPosition;
out vec3 fragNormal;
out mat3 TBN;

uniform mat4 mvp;
uniform mat4 matModel;

void main()
{
    fragTexCoord = vertexTexCoord;
    fragPosition = vec3(matModel * vec4(vertexPosition, 1.0));
    fragNormal = normalize(vec3(matModel * vec4(vertexNormal, 0.0)));
    
    vec3 T = normalize(vec3(matModel * vec4(vertexTangent, 0.0)));
    vec3 B = cross(fragNormal, T);
    TBN = mat3(T, B, fragNormal);

    gl_Position = mvp * vec4(vertexPosition, 1.0);
}