#version 330

in vec3 fragPosition;
in vec2 fragTexCoord;
in vec3 fragNormal;
in mat3 TBN;
in float fragIsSky;

// Terrain-specific uniforms
struct BiomeTextures {
    sampler2D diffuse;
    sampler2D normal;
    sampler2D specular;
    sampler2D ao;
    sampler2D bump;
    sampler2D displacement;
};

uniform BiomeTextures biomeTextures[4];
uniform sampler2D biomeWeightMap;

// Sky-specific uniforms
uniform sampler2D daySkyTexture;
uniform sampler2D nightSkyTexture;
uniform sampler2D cloudTexture;

// Shared uniforms
uniform float timeOfDay; // 0.0 to 1.0, where 0.5 is noon
uniform float cloudCoverage;
uniform float cloudOffset;
uniform vec3 viewPos;

// Celestial body uniforms
#define MAX_CELESTIAL_BODIES 10
uniform vec3 celestialBodyPositions[MAX_CELESTIAL_BODIES];
uniform vec3 celestialBodyColors[MAX_CELESTIAL_BODIES];
uniform float celestialBodyIntensities[MAX_CELESTIAL_BODIES];
uniform int celestialBodyCount;

out vec4 finalColor;

vec3 calculateTerrainLighting(vec3 albedo, vec3 normal, float specular, float ao)
{
    vec3 lighting = vec3(0.0);
    vec3 viewDir = normalize(viewPos - fragPosition);

    for (int i = 0; i < celestialBodyCount; i++)
    {
        vec3 lightDir = normalize(celestialBodyPositions[i] - fragPosition);
        float diff = max(dot(normal, lightDir), 0.0);
        
        vec3 reflectDir = reflect(-lightDir, normal);
        float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32.0);
        
        vec3 celestialLight = celestialBodyColors[i] * celestialBodyIntensities[i];
        lighting += (diff * albedo + spec * specular) * celestialLight * ao;
    }

    // Add ambient light
    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * albedo;
    lighting += ambient;

    return lighting;
}

void main()
{
    if (fragIsSky > 0.5)
    {
        // Sky rendering
        vec4 dayColor = texture(daySkyTexture, fragTexCoord);
        vec4 nightColor = texture(nightSkyTexture, fragTexCoord);

        float dayFactor = smoothstep(0.0, 0.5, timeOfDay) - smoothstep(0.5, 1.0, timeOfDay);
        vec4 skyColor = mix(nightColor, dayColor, dayFactor);

        vec2 cloudTexCoord = fragTexCoord + vec2(cloudOffset, 0.0);
        float cloudFactor = texture(cloudTexture, cloudTexCoord).r;
        cloudFactor = smoothstep(1.0 - cloudCoverage, 1.0, cloudFactor);

        vec4 cloudColor = vec4(1.0, 1.0, 1.0, cloudFactor);
        finalColor = mix(skyColor, cloudColor, cloudFactor);
    }
    else
    {
        // Terrain rendering
        vec4 biomeWeights = texture(biomeWeightMap, fragTexCoord);
        
        vec3 albedo = vec3(0.0);
        vec3 normal = vec3(0.0);
        float specular = 0.0;
        float ao = 0.0;
        
        for (int i = 0; i < 4; i++)
        {
            vec4 diffuseTex = texture(biomeTextures[i].diffuse, fragTexCoord);
            albedo += diffuseTex.rgb * biomeWeights[i];
            
            vec3 normalTex = texture(biomeTextures[i].normal, fragTexCoord).rgb;
            normal += normalTex * biomeWeights[i];
            
            specular += texture(biomeTextures[i].specular, fragTexCoord).r * biomeWeights[i];
            ao += texture(biomeTextures[i].ao, fragTexCoord).r * biomeWeights[i];
        }
        
        normal = normalize(normal * 2.0 - 1.0);
        normal = normalize(TBN * normal);
        
        vec3 lighting = calculateTerrainLighting(albedo, normal, specular, ao);
        
        finalColor = vec4(lighting, 1.0);
    }
}