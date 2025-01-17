Shader"Unlit/ProjectionShader"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "black" {}
        _BrushTex ("Brush (RGB)", 2D) = "black" {}
        _BrushUV ("Brush UV", Vector) = (0, 0, 0, 0)
        _HeightStrength("HeightStrength", float) = 0
        _HeightOffset("HeightOffset", float) = 0
        _Rotation("Rotation", float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma shader_feature paint_max paint_overlay paint_min paint_blend
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _BrushTex;
            float4 _BrushUV;
            float _HeightStrength;
            float _HeightOffset;
            float _Rotation;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                // �e�N�X�`���̎擾
                fixed4 mainTex = tex2D(_MainTex, i.uv);
                float2 buv = float2(_BrushUV.x - 0.5 / _BrushUV.z, _BrushUV.y - 0.5 / _BrushUV.w);

                i.uv -=_BrushUV;
                float angle = radians(_Rotation);
                float cosA = cos(angle);
                float sinA = sin(angle);
                float2x2 rotationMatrix = float2x2(cosA, -sinA, sinA, cosA);
                i.uv = mul(rotationMatrix, i.uv);
                i.uv += _BrushUV;

                // �u���V�̈ʒu���v�Z
                float2 brushUV = clamp((i.uv - buv) * _BrushUV.zw, 0.0, 1.0);
                float maskX = step(_BrushUV.x - 0.5/_BrushUV.z, i.uv.x) * step(i.uv.x, _BrushUV.x + 0.5/_BrushUV.z);
                float maskY = step(_BrushUV.y - 0.5/_BrushUV.w, i.uv.y) * step(i.uv.y, _BrushUV.y + 0.5/_BrushUV.w);
                float mask = maskX * maskY;

                // �u���V�e�N�X�`���̎擾
                float4 brushTex = tex2D(_BrushTex, brushUV) * _HeightStrength;
                brushTex *= mask;
                float threshold = step(0.001, brushTex.r);
                brushTex += lerp(0, _HeightOffset, threshold);

                float4 output;
                // shaderfeature�ɂ���ď�����ύX
                // 1.�傫�����̒l���Ƃ�
                #if defined(paint_max) 
                    output = max(mainTex, brushTex);
                // 2.�u���V���㏑������
                #elif defined(paint_overlay)
                    output = (mainTex * (1.0 - threshold) + brushTex);
                // 3.���������̒l���Ƃ�
                #elif defined(paint_min)
                    output = lerp(mainTex, min(mainTex, brushTex),threshold);
                #elif defined(paint_blend)
                    output = lerp(mainTex, lerp(mainTex, brushTex, threshold), 0.5);
                #endif

                // �u���V��K�p
                return output;
            }
            ENDCG
        }
    }
    FallBack"Diffuse"
}
