#version 330

in vec2 fragTexCoord;
in vec3 fragPosition;
in vec3 fragNormal;
in mat3 TBN;

out vec4 finalColor;

uniform sampler2D texture0;  // Diffuse map
uniform sampler2D texture1;  // Normal map
uniform sampler2D texture2;  // Specular map
uniform sampler2D texture3;  // Bump map (roughness)
uniform sampler2D texture4;  // Ambient Occlusion map

uniform vec3 viewPos;

void main()
{
    // Diffuse
    vec3 diffuseColor = texture(texture0, fragTexCoord).rgb;
    
    // Normal mapping
    vec3 normal = texture(texture1, fragTexCoord).rgb;
    normal = normalize(normal * 2.0 - 1.0);
    normal = normalize(TBN * normal);
    
    // Specular
    vec3 specularColor = texture(texture2, fragTexCoord).rgb;
    
    // Roughness (from bump map)
    float roughness = texture(texture3, fragTexCoord).r;
    
    // Ambient Occlusion
    float ao = texture(texture4, fragTexCoord).r;

    // Lighting calculation (simplified for this example)
    vec3 lightDir = normalize(vec3(1.0, 1.0, -1.0));  // Directional light
    vec3 viewDir = normalize(viewPos - fragPosition);
    vec3 halfwayDir = normalize(lightDir + viewDir);

    float diff = max(dot(normal, lightDir), 0.0);
    float spec = pow(max(dot(normal, halfwayDir), 0.0), 32.0 * (1.0 - roughness));

    vec3 ambient = 0.1 * diffuseColor * ao;
    vec3 diffuse = diff * diffuseColor;
    vec3 specular = spec * specularColor;

    vec3 result = ambient + diffuse + specular;

    finalColor = vec4(result, 1.0);
}