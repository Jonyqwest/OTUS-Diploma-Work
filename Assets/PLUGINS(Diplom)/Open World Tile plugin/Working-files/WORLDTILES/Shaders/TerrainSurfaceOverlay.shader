Shader "Custom/TerrainOverlays/TerrainSurfaceOverlay"
{
    Properties
    {
        _MainTex("Satellite", 2D) = "white" {}

        [Header(Snow)]
        _SnowColor("Snow Color", Color) = (1, 1, 1, 1)
        _SnowThreshold("White Threshold", Range(0, 1)) = 0.55
        _SnowSoftness("White Edge Softness", Range(0.001, 0.25)) = 0.08
        _SlopeLimit("Snow Slope Limit", Range(0.001, 1)) = 0.35
        _SnowAltitude("Snow Altitude", Float) = 50
        _AltitudeFade("Altitude Fade", Float) = 30

        [Header(Grass)]
        [Toggle] _GrassEnabled("Enable Grass Overlay", Float) = 1
        _GrassThresholdGR("GR Threshold (Green-Red)", Range(-0.5, 0.5)) = 0.04
        _GrassThresholdBG("BG Threshold (Green-Blue)", Range(-0.5, 0.5)) = 0.04
        _GrassSoftness("Green Edge Softness", Range(0.001, 0.25)) = 0.035
        _GrassDarknessThreshold("Darkness Threshold", Range(0.0, 0.15)) = 0.03
        _GrassSlopeLimit("Grass Slope Limit", Range(0.001, 1)) = 0.65

        [Header(Grass Debug View)]
        _GrassLocationColor("Grass Location Color", Color) = (0, 1, 0, 1)
        _GrassLocationOpacity("Grass Location Opacity", Range(0, 1)) = 0.65

        [Header(Rendering)]
        _AlphaClip("Empty Pixel Cutoff", Range(0, 0.1)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "Queue"="Geometry+1"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }
        ZTest LEqual
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        Offset -1, -1

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #include_with_pragmas "Includes/TerrainSurfaceOverlayCore.cginc"
            ENDHLSL
        }
    }

    FallBack Off
}
