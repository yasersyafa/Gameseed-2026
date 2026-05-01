Shader "UI/IrisWipe"
{
    Properties
    {
        _Center  ("Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _Radius  ("Radius", Range(0, 2)) = 1.5
        _Aspect  ("Aspect", Float) = 1.7777
        _Color   ("Color", Color) = (0, 0, 0, 1)
        _Softness("Edge Softness", Range(0, 0.1)) = 0.01
        _MainTex ("Sprite Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue"            = "Overlay"
            "IgnoreProjector"  = "True"
            "RenderType"       = "Transparent"
            "PreviewType"      = "Plane"
            "CanUseSpriteAtlas"= "True"
        }

        Cull   Off
        ZWrite Off
        ZTest  Always
        Blend  SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
            };

            float2 _Center;
            float  _Radius;
            float  _Aspect;
            float4 _Color;
            float  _Softness;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 d = i.uv - _Center;
                d.x *= _Aspect;
                float dist = length(d);

                float alpha = smoothstep(_Radius - _Softness, _Radius + _Softness, dist) * _Color.a;
                return half4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}
