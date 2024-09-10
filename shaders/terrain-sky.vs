#version 330

// Input vertex attributes
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec3 vertexTangent;

// Input uniform values
uniform mat4 mvp;
uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform bool isSky;

// Output vertex attributes (to fragment shader)
out vec3 fragPosition;
out vec2 fragTexCoord;
out vec3 fragNormal;
out mat3 TBN;
out float fragIsSky;

void main()
{
    fragTexCoord = vertexTexCoord;
    fragIsSky = float(isSky);
    
    if (isSky)
    {
        // For sky, use view matrix without translation
        mat4 viewRotationOnly = mat4(mat3(viewMatrix));
        gl_Position = mvp * viewRotationOnly * vec4(vertexPosition, 1.0);
        fragPosition = vertexPosition;
        fragNormal = vertexNormal;
    }
    else
    {
        // For terrain
        fragPosition = vec3(modelMatrix * vec4(vertexPosition, 1.0));
        fragNormal = normalize(mat3(modelMatrix) * vertexNormal);
        
        // Calculate tangent and bitangent
        vec3 T = normalize(mat3(modelMatrix) * vertexTangent);
        vec3 N = fragNormal;
        vec3 B = cross(N, T);
        
        // Create TBN matrix
        TBN = mat3(T, B, N);
        
        gl_Position = mvp * vec4(vertexPosition, 1.0);
    }
}