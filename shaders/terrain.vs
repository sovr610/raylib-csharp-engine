#version 330

// Input vertex attributes
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec3 vertexTangent;

// Input uniform values
uniform mat4 mvp;
uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

// Output vertex attributes (to fragment shader)
out vec3 fragPosition;
out vec2 fragTexCoord;
out vec3 fragNormal;
out mat3 TBN;

void main()
{
    // Calculate fragment position in world space
    fragPosition = vec3(model * vec4(vertexPosition, 1.0));
    
    // Pass texture coordinates to fragment shader
    fragTexCoord = vertexTexCoord;
    
    // Calculate normal in world space
    fragNormal = normalize(mat3(model) * vertexNormal);
    
    // Calculate tangent and bitangent
    vec3 T = normalize(mat3(model) * vertexTangent);
    vec3 N = fragNormal;
    vec3 B = cross(N, T);
    
    // Create TBN matrix
    TBN = mat3(T, B, N);
    
    // Calculate final vertex position
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}