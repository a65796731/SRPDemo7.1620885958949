Shader "Custom/PostFXStack"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        
        Cull  Off
        ZTest Always
        ZWrite Off


        HLSLINCLUDE
        #include "../../ShaderLibrary/Common.hlsl"
        #include "PostFXStackPasses.hlsl"
        ENDHLSL
     
       
          Pass{
          Name "Bloom Horizontal"
          HLSLPROGRAM
           #pragma target 3.5
           #pragma vertex DefaultPassVertex
           #pragma fragment BloomHorizontalPassFragment
          ENDHLSL
        
        }
            Pass{
          Name "Bloom Vertical"
          HLSLPROGRAM
           #pragma target 3.5
           #pragma vertex DefaultPassVertex
           #pragma fragment BloomVerticalPassFragment
          ENDHLSL
        
        }
        Pass{
        Name "Bloom Combine"
         HLSLPROGRAM
         #pragma target 3.5
         #pragma vertex DefaultPassVertex
         #pragma fragment BloomComBinePassFragment
         ENDHLSL
        }
        Pass{
          Name "Bloom Prefilter"
          HLSLPROGRAM
          #pragma target 3.5
          #pragma vertex DefaultPassVertex
          #pragma fragment BloomPrefilterPassFragment
          ENDHLSL

        }
        Pass{
          Name "Copy"
          HLSLPROGRAM
           #pragma target 3.5
           #pragma vertex DefaultPassVertex
           #pragma fragment CopyPassFragment
          ENDHLSL
        
        }

    
    }
   
}
