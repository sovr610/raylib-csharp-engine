#version 330

// Input vertex attributes (from vertex shader)
in vec2 fragTexCoord;
in vec3 fragPosition;

// Input uniform values
uniform sampler2D daySkyTexture;
uniform sampler2D nightSkyTexture;
uniform sampler2D cloudTexture;
uniform float timeOfDay; // 0.0 to 1.0, where 0.5 is noon
uniform float cloudCoverage; // 0.0 to 1.0
uniform float cloudOffset;

// Output fragment color
out vec4 finalColor;

void main()
{
    // Sample day and night textures
    vec4 dayColor = texture(daySkyTexture, fragTexCoord);
    vec4 nightColor = texture(nightSkyTexture, fragTexCoord);

    // Interpolate between day and night based on time of day
    float dayFactor = smoothstep(0.0, 0.5, timeOfDay) - smoothstep(0.5, 1.0, timeOfDay);
    vec4 skyColor = mix(nightColor, dayColor, dayFactor);

    // Sample cloud texture with offset for movement
    vec2 cloudTexCoord = fragTexCoord + vec2(cloudOffset, 0.0);
    float cloudFactor = texture(cloudTexture, cloudTexCoord).r;

    // Apply cloud coverage
    cloudFactor = smoothstep(1.0 - cloudCoverage, 1.0, cloudFactor);

    // Mix sky color with cloud color
    vec4 cloudColor = vec4(1.0, 1.0, 1.0, cloudFactor); // White clouds
    finalColor = mix(skyColor, cloudColor, cloudFactor);
}