// The Table comes alive (owner, Oct 7-8; task 86bcf0x71): a placed sign's lettering takes its element's material. Unity's UI-Default shader
// for text, with the colour taken from a tileable fill (Resources/Art/letter-fire, -earth, -air, -water) sampled across the word's own
// rectangle (_Rect, in the Text's local space), not across the font atlas, so the fill runs through the whole word. _Drift moves the fill
// a little each second (embers rise, water runs, wind drifts, earth holds still); Reduced motion sets it to zero. The glyph's coverage comes
// from the font atlas as usual, so masking and clipping behave like any UI text. It lives under Resources so the Web build carries it
// (SliceView loads it by name).
Shader "Ascendant/LetterFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FillTex ("Fill", 2D) = "white" {}
        _Rect ("Word rect (x, y, width, height)", Vector) = (0, 0, 100, 40)
        _Tile ("Fill tiles across the word", Float) = 1.5
        _Drift ("Drift per second (x, y)", Vector) = (0, 0, 0, 0)
        _Lift ("Brightness", Float) = 1.35

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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
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
        Blend SrcAlpha OneMinusSrcAlpha
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
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 fill     : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _FillTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float4 _Rect;
            float _Tile;
            float4 _Drift;
            float _Lift;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                float2 local = (v.vertex.xy - _Rect.xy) / max(_Rect.zw, float2(1, 1)); // 0..1 across the word's rectangle
                OUT.fill = float2(local.x * _Tile * (_Rect.z / max(_Rect.w, 1)), local.y * _Tile) + _Drift.xy * _Time.y;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half coverage = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd).a; // the glyph's shape, from the font atlas
                half3 fill = tex2D(_FillTex, frac(IN.fill)).rgb * _Lift; // frac: the sprite is a whole file, whatever its wrap mode
                half4 color = half4(saturate(fill) * IN.color.rgb, coverage * IN.color.a);
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
