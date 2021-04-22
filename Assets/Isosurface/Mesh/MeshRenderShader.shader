// This shader fills the mesh shape with a color predefined in the code.
Shader "Custom/MeshRenderShader"
{
    // The properties block of the Unity shader. In this example this block is empty
    // because the output color is predefined in the fragment shader code.
    Properties
    { }

    // The SubShader block containing the Shader code. 
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }

        Pass
        {
            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            // This line defines the name of the vertex shader. 
            #pragma vertex vert
            // This line defines the name of the fragment shader. 
            #pragma fragment frag
            #pragma target 3.5

            // The Core.hlsl file contains definitions of frequently used HLSL
            // macros and functions, and also contains #include references to other
            // HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"            

            StructuredBuffer<float3> _Mesh;

            // The structure definition defines which variables it contains.
            // This example uses the Attributes structure as an input structure in
            // the vertex shader.
            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object
                // space.
                // float4 positionOS   : POSITION;                 
                uint vid : SV_VertexID; // vertex ID, needs to be uint
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS  : SV_POSITION;
                // float3 vIdColor : TEXCOORD0;
            };

            float3 getVertex(uint vid) {
                return _Mesh[vid];
            }           

            // The vertex shader definition with properties defined in the Varyings 
            // structure. The type of the vert function must match the type (struct)
            // that it returns.
            Varyings vert(Attributes IN, uint vid : SV_VertexID)
            {
                // Declaring the output object (OUT) with the Varyings struct.
                Varyings OUT;
                // The TransformObjectToHClip function transforms vertex positions
                // from object space to homogenous space
                // OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionHCS = TransformObjectToHClip(float4(getVertex(vid), 1.0));
                
                // OUT.vIdColor = float3(vid / (float)7, 0, 0);

                // Returning the output.
                return OUT;
            }

            // The fragment shader definition.            
            half4 frag(Varyings IN) : SV_Target
            {
                // Defining the color variable and returning it.
                half4 customColor;
                // customColor = half4(IN.vIdColor.x, 0, 0, 1);
                customColor = half4(1, 1, 0, 1);
                return customColor;
            }
            ENDHLSL
        }
    }
}
