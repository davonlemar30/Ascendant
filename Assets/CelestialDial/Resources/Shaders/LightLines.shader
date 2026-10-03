// Batch 2, step 5 (owner, Oct 3: the family triangles as an overlay, approved): the four element triangles laid ON TOP of the Dial as light.
// A screen blend (every channel only rises, so nothing under a line darkens or disappears), as on the approved board (86bcbn6w6, Oct 2).
// FamilyTriangles builds the quads; each carries its own profile in its texture coordinates (layout px), so one draw makes every line, star,
// seat glow and flame: uv0 (x across the line, y along it, z the crisp core's half-width, w the kind: 0 a line, 1 a star, 2 a flame),
// uv1 (the glow's colour, already times its strength, and its sigma), uv2 (a flame's hot core colour and its seed), uv3 (x the side's length,
// yzw the crisp core's colour times its strength). Every colour is the board's own (sRGB light, screened in sRGB); the light is turned into
// linear colour at the end, so on the Dial's dark bronze it lands as the board's did. It lives under Resources so the Web build carries it.
Shader "Ascendant/Light Lines"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Fade ("Flame fade", Range(0, 1)) = 1
        _Still ("Still flames (reduced motion)", Range(0, 1)) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
        Blend OneMinusDstColor One
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
                float4 uv3 : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color  : COLOR;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
                float4 uv3 : TEXCOORD3;
                float4 worldPosition : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _ClipRect;
            half _Fade;
            half _Still;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.color = v.color; OUT.uv0 = v.uv0; OUT.uv1 = v.uv1; OUT.uv2 = v.uv2; OUT.uv3 = v.uv3;
                return OUT;
            }

            // the board's value noise (looks.cjs): a smooth step between hashed integers
            float hash1(float n) { float x = sin(n * 127.1 + 311.7) * 43758.5453; return x - floor(x); }
            float noise1(float x, float seed) { float i = floor(x), f = x - i, u = f * f * (3 - 2 * f); return lerp(hash1(i + seed * 101), hash1(i + 1 + seed * 101), u); }
            float gauss(float d, float sigma) { return sigma > 0 ? exp(-d * d / (2 * sigma * sigma)) : 0; }

            fixed4 frag(v2f IN) : SV_Target
            {
                float kind = IN.uv0.w; float3 light = 0;
                if (kind < .5) // a line: the crisp core in the vertex colour, its glow in uv1 (a Gaussian round the line)
                {
                    float d = abs(IN.uv0.x), aa = max(fwidth(IN.uv0.x), 1e-4), hw = IN.uv0.z;
                    float core = hw > 0 ? saturate((hw - d) / aa + .5) : 0;
                    light = IN.uv3.yzw * core + IN.uv1.rgb * gauss(d, IN.uv1.w);
                }
                else if (kind < 1.5) // a corner's star: four rays along the radius and the tangent, and a dot (uv0.xy: layout px, x along the radius)
                {
                    float2 p = IN.uv0.xy; float aa = max(fwidth(p.x), 1e-4), ray = 2.1, hw = .275;
                    float dotv = saturate((.65 - length(p)) / aa + .5);
                    float rx = abs(p.x) > ray + .25 ? 0 : saturate((hw * max(0, 1 - abs(p.x) / (ray + .25)) + .075 - abs(p.y)) / aa + .5);
                    float ry = abs(p.y) > ray + .25 ? 0 : saturate((hw * max(0, 1 - abs(p.y) / (ray + .25)) + .075 - abs(p.x)) / aa + .5);
                    light = IN.uv3.yzw * max(dotv, max(rx, ry)) + IN.uv1.rgb * gauss(length(p), IN.uv1.w);
                }
                else // a ribbon of flame (the board's flameLight, in layout px): its centreline wavers, tongues lick up the screen, its ends thin
                {
                    float seed = IN.uv2.w, tl = IN.uv0.y, len = max(IN.uv3.x, 1), t = _Still > .5 ? 0 : _Time.y;
                    float cc = (noise1(tl / 5.5 + t * .9, seed) - .5) * .8, hw = (.75 + .65 * noise1(tl / 3 + t * 1.7, seed + 5)) * 1.15;
                    float tongue = pow(max(0, noise1(tl / 2.1 + t * 2.6, seed + 9) - .45) / .55, 1.4) * 3.25 * 1.15;
                    float sp = IN.uv0.x - cc, reach = sp >= 0 ? hw + tongue : hw * .8, val = max(0, 1 - abs(sp) / reach);
                    float ends = min(1, min(tl, len - tl) / 3 + .4), q = pow(val, .9) * ends, hot = max(0, q - .55) / .45;
                    float body = min(1, q * 1.4), glow = min(1, gauss(sp, 1.5) * .9) * ends, bloom = min(1, gauss(sp, 4) * .55) * ends;
                    light = (IN.uv1.rgb * (body * .85 + glow * .45 + bloom * .22) + IN.uv2.rgb * hot) * _Fade;
                }
                #ifdef UNITY_UI_CLIP_RECT
                light *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                light = saturate(light);
                #ifndef UNITY_COLORSPACE_GAMMA
                light = pow(light, 1.7); // into linear colour, gentler than sRGB's curve: a screen in linear colour dims light over mid-tones, so the lines on today's bronze read as the board's (see FamilyTriangles: measured against the board)
                #endif
                return fixed4(light, 1);
            }
        ENDCG
        }
    }
}
