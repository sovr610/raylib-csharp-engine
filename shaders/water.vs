// Vertex Shader
#version 330

// Input vertex attributes
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec4 vertexColor;

// Input uniform values
uniform mat4 mvp;
uniform mat4 matModel;
uniform mat4 matNormal;
uniform float time;
uniform float waveAmplitude;
uniform float waveFrequency;
uniform float waveSpeed;
uniform float noiseScale;
uniform float noiseSpeed;

// Output vertex attributes (to fragment shader)
out vec2 fragTexCoord;
out vec4 fragColor;
out vec3 fragPosition;
out vec3 fragNormal;

// Function to generate Simplex noise (you'd need to implement this)
float simplex3D(vec3 p);

void main()
{
    // Calculate wave offset
    float waveOffset = sin(vertexPosition.x * waveFrequency + time * waveSpeed) * 
                       cos(vertexPosition.z * waveFrequency * 0.8 + time * waveSpeed * 1.1);
    waveOffset *= waveAmplitude;

    // Add noise to the wave
    vec3 noiseInput = vertexPosition * noiseScale + vec3(0, time * noiseSpeed, 0);
    float noise = simplex3D(noiseInput) * 0.5;
    waveOffset += noise * waveAmplitude * 0.5;

    // Apply wave offset to vertex position
    vec3 position = vertexPosition;
    position.y += waveOffset;

    // Calculate vertex position in screen space
    gl_Position = mvp * vec4(position, 1.0);

    // Calculate world position for fragment shader
    fragPosition = vec3(matModel * vec4(position, 1.0));

    // Calculate normal in world space
    vec3 normal = vertexNormal;
    normal.y = 1.0 - abs(waveOffset) * 2.0; // Adjust normal based on wave height
    normal = normalize(normal);
    fragNormal = normalize(vec3(matNormal * vec4(normal, 0.0)));

    // Send vertex color to fragment shader
    fragColor = vertexColor;

    // Send texCoord to fragment shader
    fragTexCoord = vertexTexCoord;
}