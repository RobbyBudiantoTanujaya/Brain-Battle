Shader "BrainBattle/UI/RoundedCornerUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _UICornerRadius ("Corner Radius", Float) = 16
        _UIBorderWidth ("Border Width", Float) = 0
        _UIBorderColor ("Border Color", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 localPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;
            fixed4 _Color;
            float4 _ClipRect;
            float _UICornerRadius;
            float _UIBorderWidth;
            fixed4 _UIBorderColor;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.localPosition = IN.vertex.xy;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            float sdRoundedBox(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - (halfSize - radius);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uvCentered = IN.texcoord - 0.5;
                float2 halfSize = 0.5.xx;
                float radius01 = saturate(_UICornerRadius / max(1.0, 4096.0));
                float border01 = saturate(_UIBorderWidth / max(1.0, 4096.0));

                float aspect = max(1e-5, abs(ddx(IN.localPosition.x)) + abs(ddy(IN.localPosition.x)));
                aspect = 1.0;
                float2 localHalf = halfSize;
                float radius = saturate(_UICornerRadius * 0.001);
                float border = saturate(_UIBorderWidth * 0.001);

                float2 extents = halfSize;
                float d = sdRoundedBox(uvCentered, extents, radius);
                float aa = fwidth(d) * 1.5;
                float fillAlpha = 1.0 - smoothstep(0.0, aa, d);

                fixed4 texColor = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;
                fixed4 color = texColor * IN.color;

                if (_UIBorderWidth > 0.0)
                {
                    float innerRadius = max(radius - border, 0.0);
                    float innerDistance = sdRoundedBox(uvCentered, extents - border, innerRadius);
                    float innerAlpha = 1.0 - smoothstep(0.0, aa, innerDistance);
                    float borderMask = saturate(fillAlpha - innerAlpha);
                    color.rgb = lerp(color.rgb, _UIBorderColor.rgb * color.a, borderMask);
                    color.a = max(innerAlpha * color.a, borderMask * _UIBorderColor.a * color.a);
                }
                else
                {
                    color.a *= fillAlpha;
                }

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
