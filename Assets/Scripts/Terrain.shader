Shader "Custom/Terrain"
{
    Properties
    {
        testTexture("Texture", 2D) = "white"{}
        testScale("Scale", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        const static int maxLayerCount = 8;
        const static float epsilon = 1E-4; // very small number

        int layerCount;
        float3 baseColours[maxLayerCount];
        float baseStartHeights[maxLayerCount];
        float baseBlends[maxLayerCount];
        float baseColourStrength[maxLayerCount];
        float baseTextureScales[maxLayerCount];

        float minHeight;
        float maxHeight;

        sampler2D testTexture;
        float testScale;

        UNITY_DECLARE_TEX2DARRAY(baseTextures);

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        float inverseLerp(float minVal, float maxVal, float currentVal) {
            // currentVal must be minVal < currentVal < maxValue
            return saturate((currentVal-minVal)/(maxVal-minVal));
        }

        float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIdx) {

            //triPlanar Mapping
            float3 scaledWorldPos = worldPos / scale;

            float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIdx)) * blendAxes.x;
            float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIdx)) * blendAxes.y;
            float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIdx)) * blendAxes.z;
            
            return xProjection + yProjection + zProjection;
            //o.Albedo = xProjection + yProjection + zProjection;
        
            //test
            //o.Albedo = tex2D(testTexture, IN.worldPos.xy/testScale);

        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
            float3 blendAxes = abs(IN.worldNormal);
            // make sure blendAxes is no > 1;
            blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;
            for(int i=0; i<layerCount; i++) {
                //epsilon - to make sure we don't end up with /0 
                float drawStrength = inverseLerp(-baseBlends[i]/2 - epsilon, baseBlends[i]/2, heightPercent - baseStartHeights[i]);
                float3 baseColour = baseColours[i] * baseColourStrength[i];
                float3 textureColour = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1 - baseColourStrength[i]);
                o.Albedo = o.Albedo * (1 - drawStrength) + (baseColour + textureColour) * drawStrength;
            }

        }
        ENDCG
    }
    FallBack "Diffuse"
}
