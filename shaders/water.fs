#version 330

// Input vertex attributes (from vertex shader)
in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragPosition;
in vec3 fragNormal;

// Input uniform values
uniform sampler2D normalMap;
uniform sampler2D specularMap;
uniform vec3 viewPos;

// Output fragment color
out vec4 finalColor;

void main()
{
    // Sample normal map and calculate perturbed normal
    vec3 normalMap = texture(normalMap, fragTexCoord).rgb;
    vec3 normal = normalize(fragNormal + (normalMap * 2.0 - 1.0));

    // Calculate view direction
    vec3 viewDir = normalize(viewPos - fragPosition);

    // Water color (adjust as needed)
    vec3 waterColor = vec3(0.0, 0.2, 0.3);

    // Ambient light
    vec3 ambient = waterColor * 0.2;

    // Diffuse lighting
    vec3 lightDir = normalize(vec3(0.5, 1.0, 0.3)); // Directional light
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = diff * waterColor;

    // Specular lighting
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32.0);
    vec3 specular = spec * texture(specularMap, fragTexCoord).rgb;

    // Fresnel effect
    float fresnel = pow(1.0 - max(dot(normal, viewDir), 0.0), 5.0);

    // Combine lighting components
    vec3 result = ambient + diffuse + specular + fresnel * 0.5;

    // Depth-based transparency
    float depth = gl_FragCoord.z / gl_FragCoord.w;
    float alpha = clamp(depth / 10.0, 0.1, 0.9); // Adjust divisor to control transparency

    finalColor = vec4(result, alpha);
}