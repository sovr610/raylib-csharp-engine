#version 330

// Input vertex attributes (from vertex shader)
in vec3 fragPosition;
in vec2 fragTexCoord;
in vec3 fragNormal;
in mat3 TBN;

// Input uniform values
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

uniform vec3 viewPos;
uniform vec3 lightPos;
uniform vec3 lightColor;
uniform float ambientStrength;
uniform float specularStrength;
uniform float bumpStrength;

// Output fragment color
out vec4 finalColor;

vec4 sampleBiomeTexture(BiomeTextures biome, vec2 texCoord, float weight)
{
    vec4 diffuse = texture(biome.diffuse, texCoord) * weight;
    vec3 normal = texture(biome.normal, texCoord).rgb;
    float specular = texture(biome.specular, texCoord).r * weight;
    float ao = texture(biome.ao, texCoord).r * weight;
    vec3 bump = texture(biome.bump, texCoord).rgb * weight;
    
    return vec4(diffuse.rgb, specular);
}

void main()
{
    vec4 biomeWeights = texture(biomeWeightMap, fragTexCoord);
    
    vec4 diffuseSpecular = vec4(0.0);
    vec3 normal = vec3(0.0);
    float ao = 0.0;
    vec3 bump = vec3(0.0);
    
    for (int i = 0; i < 4; i++)
    {
        vec4 biomeTexture = sampleBiomeTexture(biomeTextures[i], fragTexCoord, biomeWeights[i]);
        diffuseSpecular += biomeTexture;
        normal += texture(biomeTextures[i].normal, fragTexCoord).rgb * biomeWeights[i];
        ao += texture(biomeTextures[i].ao, fragTexCoord).r * biomeWeights[i];
        bump += texture(biomeTextures[i].bump, fragTexCoord).rgb * biomeWeights[i];
    }
    
    normal = normalize(normal * 2.0 - 1.0);   
    normal = normalize(TBN * normal);
    
    // Bump mapping
    bump = normalize(bump * 2.0 - 1.0);
    normal = normalize(normal + (bump * bumpStrength));

    // Ambient
    vec3 ambient = ambientStrength * lightColor;
  	
    // Diffuse 
    vec3 lightDir = normalize(lightPos - fragPosition);
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;
    
    // Specular
    vec3 viewDir = normalize(viewPos - fragPosition);
    vec3 reflectDir = reflect(-lightDir, normal);  
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);
    vec3 specular = specularStrength * spec * lightColor * diffuseSpecular.a;
    
    // Combine results
    vec3 result = (ambient + diffuse) * diffuseSpecular.rgb * ao + specular;
    
    finalColor = vec4(result, 1.0);
}