MODES
{
    Default();
    VrForward();
}

FEATURES
{
}

COMMON
{
    #include "postprocess/shared.hlsl"
}

struct VertexInput
{
    float3 vPositionOs : POSITION < Semantic( PosXyz ); >;
    float2 vTexCoord : TEXCOORD0 < Semantic( LowPrecisionUv ); >;
};

struct PixelInput
{
    float2 uv : TEXCOORD0;
    
    #if ( PROGRAM == VFX_PROGRAM_VS )
        float4 vPositionPs : SV_Position;
    #endif

    #if ( PROGRAM == VFX_PROGRAM_PS )
        float4 vPositionSs : SV_ScreenPosition;
    #endif
};

VS
{
    float g_flStep< Attribute( "Step" ); Default( 1.0f ); >;
    PixelInput MainVs( VertexInput i )
    {
        PixelInput o;
        o.vPositionPs = float4( i.vPositionOs.xy, 0.0f, 1.0f );
        o.uv = i.vTexCoord;
        return o;
    }
}

PS
{
    CreateTexture2D( g_tUiTexture ) < Attribute( "UiTexture" ); SrgbRead( true ); Filter( ANISOTROPIC ); AddressU( CLAMP ); AddressV( CLAMP ); >;
    float g_fDistortionAmount < Attribute( "DistortionAmount" ); Default( 1.0f ); >;

    RenderState( SrgbWriteEnable0, true );
	RenderState( ColorWriteEnable0, RGBA );
	RenderState( FillMode, SOLID );
	RenderState( CullMode, NONE );

    RenderState( BlendEnable, true );
    RenderState( SrcBlend, SRC_ALPHA );
    RenderState( DstBlend, INV_SRC_ALPHA );
    RenderState( BlendOp, ADD );
    RenderState( SrcBlendAlpha, ONE );
    RenderState( DstBlendAlpha, INV_SRC_ALPHA );
    RenderState( BlendOpAlpha, ADD );

    float2 DistortUv(float2 uv, float distortionAmount)
    {
        // Multiply block
        float horizFlipped = uv.x * (uv.x - 1);
        float vertFlippedHalf = uv.y * -2 + 1;
        float combined = vertFlippedHalf * horizFlipped;

        return float2(uv.x, uv.y + distortionAmount * combined);
    }

    // https://github.com/DOWNPOURDIGITAL/glsl-chromatic-aberration/blob/master/ca.glsl
    float4 chromatic_abberation(float2 uv, float2 resolution, float2 direction) {
        float4 col = 0;
        float2 pixelSize = 1.0 / resolution;
        float2 offset = float2(2, 0.0) * pixelSize;

        float4 left = Tex2D( g_tUiTexture, uv + offset);
        float4 center = Tex2D( g_tUiTexture, uv );
        float4 right = Tex2D( g_tUiTexture, uv - offset);

        col.ra += left.ra;
        col.ga += center.ga * float2(0.9, 1);
        col.ba += right.ba;
        col.a /= 5;
        col.a = max(col.a, center.a);

        return lerp((col + center) / 2, center, direction.x);
    }

	float4 MainPs( PixelInput i ) : SV_Target0
	{
        float2 uv = DistortUv(i.uv, g_fDistortionAmount);
        #if D_CHROMATIC_ABERRATION == 1
            float2 direction = ( uv - 0.5 ) * 2.0;
            return chromatic_abberation(uv, g_vViewportSize, direction);
        #else
            return Tex2D( g_tUiTexture, uv);
        #endif
	}
}