Shader "Custom/XRayOutline"
{
    // Properties
    // {
    //     [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
    //     [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
    // }

    Properties
    {
        _XRayColor ("X-Ray Color", Color) = (0, 1, 1, 0.4)
        _OutlineColor ("Outline Color", Color) = (0, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" }

        Pass 
        {
            Name "XRAY"
            ZTest Greater
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _XRayColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _XRayColor;
            }
            ENDHLSL
        }

        // Outline
        Pass 
        {
            Name "OUTLINE"
            ZTest Greater
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Front             // Cull Front untuk efek outline expand

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // Expand vertex ke arah normal → bikin outline
                float3 expanded = IN.positionOS.xyz + IN.normalOS * 0.03;
                OUT.positionHCS = TransformObjectToHClip(expanded);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
