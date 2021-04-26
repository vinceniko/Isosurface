using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;

namespace Isosurface 
{
    public class GPUGrid : MonoBehaviour
    {
        [SerializeField]
        ComputeShader isoValsShader = default;
        [SerializeField]
        ComputeShader surfacePointsShader = default;
        [SerializeField]
        ComputeShader meshShader = default;
        [SerializeField]
        ComputeShader fixArgs = default;

        
        ComputeBuffer isoValsBuffer;
        ComputeBuffer surfacePointsBuffer;
        ComputeBuffer normalsBuffer;
        ComputeBuffer meshBuffer;
        ComputeBuffer meshBufferFloat;


        static readonly int 
            isoValsId = Shader.PropertyToID("_IsoVals"),
            surfacePointsId = Shader.PropertyToID("_SurfacePoints"),
            normalsId = Shader.PropertyToID("_Normals"),
            meshId = Shader.PropertyToID("_Mesh"),
            resolutionId = Shader.PropertyToID("_Resolution"),
            stepId = Shader.PropertyToID("_Step"),
            timeId = Shader.PropertyToID("_Time"),
            // sId = Shader.PropertyToID("_ShapeSize"),
            gridToWorldID = Shader.PropertyToID("_GridToWorld"),
            viewDirID = Shader.PropertyToID("_ViewDir"),
            pointBrightnessID = Shader.PropertyToID("_PointBrightness"),
            pointSizeID = Shader.PropertyToID("_PointSize"),
            shapeToWorldID = Shader.PropertyToID("_ShapeToWorld");

        
        [SerializeField]
        SDFShape[] shape;

        const int minResolution = 4;
        const int maxResolution = 96;
        // [SerializeField]
        // public int shapeSize = 1;

        [SerializeField]
        public int size = 100;

        [SerializeField, Range(minResolution, maxResolution)]
        int resolution = minResolution;

        [SerializeField, Range(0, 3.5f)]
        float pointSize = 1.5f;

        [SerializeField]
        Material material = default;

        [SerializeField]
        Material transparencyMaterial = default;
        [SerializeField]
        Material surfacePointMaterial = default;
        [SerializeField]
        Material meshMaterial = default;

        [SerializeField]
        Mesh pointMesh = default;

        [SerializeField, Range(0.0f, 0.99f)]
        float pointBrightness = 0.99f;

        [SerializeField]
        FunctionLibrary.FunctionName shapeFunction = FunctionLibrary.FunctionName.Sphere;

        enum SurfacePointsFunc { QEF, Centroid, Block }

        [SerializeField]
        SurfacePointsFunc surfacePointsFunc = SurfacePointsFunc.Centroid;

        [SerializeField]
        bool showGrid = true;
        [SerializeField]
        bool showVolume = true;
        [SerializeField]
        bool showSurface = true;
        [SerializeField]
        bool showMesh = true;

        // DrawProceduralIndirect
        ComputeBuffer argsBuffer;
        [StructLayout(LayoutKind.Sequential)]
        struct DrawCallArgBuffer
        {
            public const int size =
                sizeof(int) +
                sizeof(int) +
                sizeof(int) +
                sizeof(int);
            public int vertexCountPerInstance;
            public int instanceCount;
            public int startVertexLocation;
            public int startInstanceLocation;
        }


        // public enum TransitionMode { Cycle, Random }

        // [SerializeField]
        // TransitionMode transitionMode = TransitionMode.Cycle;

        // [SerializeField, Min(0f)]
        // float functionDuration = 1f, transitionDuration = 1f;

        // float duration;

        // bool transitioning;

        // FunctionLibrary.FunctionName transitionFunction;

        // void Awake() 
        // {
        //     transform.position = Vector3.one * (-size * 0.5f + size / resolution);
        // }

        static string GetSurfacePointsFuncName(SurfacePointsFunc surfacePointsFunc) {
            switch (surfacePointsFunc) {
                case SurfacePointsFunc.QEF:
                    return "SURFACE_POINT_QEF";
                case SurfacePointsFunc.Centroid:
                    return "SURFACE_POINT_CENTROID";
                case SurfacePointsFunc.Block:
                    return "SURFACE_POINT_BLOCKY";
                default:
                    return "";
            }
        }

        void OnEnable() 
        {
            Mesh mesh = GetComponent<MeshFilter>().mesh;
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            isoValsBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, Marshal.SizeOf(typeof(float)));
            surfacePointsBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, Marshal.SizeOf(typeof(Vector4)));
            normalsBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, Marshal.SizeOf(typeof(Vector4)));
            
            argsBuffer = new ComputeBuffer(1, DrawCallArgBuffer.size, ComputeBufferType.IndirectArguments);
            int[] args = new int[] { 0, 1, 0, 0 };
            argsBuffer.SetData(args);
            meshBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution * 3 /*dims*/ * 2 /*tris per dim*/, Marshal.SizeOf(typeof(Vector4)) * 6, ComputeBufferType.Append);
            meshBufferFloat = new ComputeBuffer(maxResolution * maxResolution * maxResolution * 3 /*dims*/ * 2 /*tris per dim*/ * 3, Marshal.SizeOf(typeof(Vector4)));
        }

        void OnDisable()
        {
            isoValsBuffer.Release();
            isoValsBuffer = null;
            surfacePointsBuffer.Release();
            surfacePointsBuffer = null;
            normalsBuffer.Release();
            normalsBuffer = null;
            argsBuffer.Release();
            argsBuffer = null;
            meshBuffer.Release();
            meshBuffer = null;
            meshBufferFloat.Release();
            meshBufferFloat = null;
        }

        void Update()
        {
            // duration += Time.deltaTime;
            // if (transitioning)
            // {
            //     if (duration >= transitionDuration)
            //     {
            //         duration -= transitionDuration;
            //         transitioning = false;
            //     }
            // }
            // else if (duration >= functionDuration)
            // {
            //     duration -= functionDuration;
            //     transitioning = true;
            //     transitionFunction = shapeFunction;
            //     PickNextFunction();
            // }

            // shapeSize = (int)(shape.transform.localScale.x / 2f);

            UpdateFunctionOnGPU();
        }

        void UpdateFunctionOnGPU () {
            int resolution_ = resolution / 4;
            resolution_ *= 4;

            float step = size / (float)resolution_;
            isoValsShader.SetInt(resolutionId, resolution_);
            isoValsShader.SetFloat(stepId, step);

            // duration += Time.deltaTime;
            // isoValsShader.SetFloat(sId, (shapeSize * (((Mathf.Sin(5f * duration) + 2f) * 0.5f))));
            // isoValsShader.SetFloat(sId, shapeSize);

            isoValsShader.SetFloat(timeId, Time.time);
            isoValsShader.SetMatrix(gridToWorldID, this.transform.localToWorldMatrix);
            isoValsShader.SetMatrixArray(shapeToWorldID, shape.Select(v => v.transform.localToWorldMatrix.inverse).ToArray());
            isoValsShader.SetBuffer(0, isoValsId, isoValsBuffer);

            int groups = Mathf.CeilToInt(resolution_ / 4f);
            isoValsShader.EnableKeyword(FunctionLibrary.GetName(shapeFunction));
            isoValsShader.Dispatch(0, groups, groups, groups);
            isoValsShader.DisableKeyword(FunctionLibrary.GetName(shapeFunction));
                
            var bounds = new Bounds(Vector3.zero, Vector3.one * (size + step));

            if (showGrid) {
                transparencyMaterial.SetBuffer(isoValsId, isoValsBuffer);
                transparencyMaterial.SetFloat(stepId, step);
                transparencyMaterial.SetInt(resolutionId, resolution_);
                transparencyMaterial.SetMatrix(gridToWorldID, this.transform.localToWorldMatrix);
                transparencyMaterial.SetVector(viewDirID, -Camera.main.transform.forward);
                transparencyMaterial.SetFloat(pointBrightnessID, pointBrightness);
                transparencyMaterial.SetFloat(pointSizeID, pointSize);
                
                Graphics.DrawMeshInstancedProcedural(pointMesh, 0, transparencyMaterial, bounds, resolution_ * resolution_ * resolution_);
            }

            if (showVolume) {
                material.SetBuffer(isoValsId, isoValsBuffer);
                material.SetFloat(stepId, step);
                material.SetInt(resolutionId, resolution_);
                material.SetMatrix(gridToWorldID, this.transform.localToWorldMatrix);
                material.SetVector(viewDirID, -Camera.main.transform.forward);
                material.SetFloat(pointBrightnessID, pointBrightness);
                material.SetFloat(pointSizeID, pointSize);
                
                Graphics.DrawMeshInstancedProcedural(pointMesh, 0, material, bounds, resolution_ * resolution_ * resolution_);
            }

            // surface points
            surfacePointsShader.SetInt(resolutionId, resolution_);
            surfacePointsShader.SetMatrix(gridToWorldID, this.transform.localToWorldMatrix);
            surfacePointsShader.SetMatrixArray(shapeToWorldID, shape.Select(v => v.transform.localToWorldMatrix.inverse).ToArray());
            surfacePointsShader.SetFloat(stepId, step);
            surfacePointsShader.SetBuffer(0, isoValsId, isoValsBuffer);
            surfacePointsShader.SetBuffer(0, surfacePointsId, surfacePointsBuffer);
            surfacePointsShader.SetBuffer(0, normalsId, normalsBuffer);
            surfacePointsShader.EnableKeyword(FunctionLibrary.GetName(shapeFunction));
            surfacePointsShader.EnableKeyword(GetSurfacePointsFuncName(surfacePointsFunc));
            // surfacePointsShader.SetMatrixArray(shapeToWorldID, shape.Select(v => v.transform.localToWorldMatrix).ToArray());
            surfacePointsShader.Dispatch(0, groups, groups, groups);
            surfacePointsShader.DisableKeyword(FunctionLibrary.GetName(shapeFunction));
            surfacePointsShader.DisableKeyword(GetSurfacePointsFuncName(surfacePointsFunc));

            // var data = new Vector3[resolution_*resolution_*resolution_];
            // surfacePointsBuffer.GetData(data);
            // print(data[10]);

            if (showSurface) {
                surfacePointMaterial.SetFloat(stepId, step);
                surfacePointMaterial.SetInt(resolutionId, resolution_);
                surfacePointMaterial.SetMatrix(gridToWorldID, this.transform.localToWorldMatrix);
                surfacePointMaterial.SetVector(viewDirID, -Camera.main.transform.forward);
                surfacePointMaterial.SetFloat(pointSizeID, pointSize);
                surfacePointMaterial.SetBuffer(surfacePointsId, surfacePointsBuffer);
                surfacePointMaterial.SetBuffer(normalsId, normalsBuffer);

                Graphics.DrawMeshInstancedProcedural(pointMesh, 0, surfacePointMaterial, bounds, resolution_ * resolution_ * resolution_);
            }

            Mesh mesh = GetComponent<MeshFilter>().mesh;
            mesh.Clear();
            if (showMesh) {
                // TODO: custom render shader (URP shader graph) that gets verts from buffer, use vert id, create variant of urp standard
                // see info at bottom for vert id info?: https://docs.unity3d.com/Manual/SL-ShaderSemantics.html
                // https://samdriver.xyz/article/compute-shader-intro
                // https://cyangamedev.wordpress.com/2020/06/05/urp-shader-code/
                // https://gist.github.com/phi-lira/225cd7c5e8545be602dca4eb5ed111ba

                // construct mesh
                meshBuffer.SetCounterValue(0);

                int meshKernel = 0;
                meshShader.SetInt(resolutionId, resolution_);
                meshShader.SetMatrix(gridToWorldID, this.transform.localToWorldMatrix);
                meshShader.SetFloat(stepId, step);
                meshShader.SetBuffer(meshKernel, isoValsId, isoValsBuffer);
                meshShader.SetBuffer(meshKernel, surfacePointsId, surfacePointsBuffer);
                meshShader.SetBuffer(meshKernel, normalsId, normalsBuffer);
                meshShader.SetBuffer(meshKernel, meshId, meshBuffer);
                meshShader.EnableKeyword(FunctionLibrary.GetName(shapeFunction));
                meshShader.Dispatch(meshKernel, groups, groups, groups);
                meshShader.DisableKeyword(FunctionLibrary.GetName(shapeFunction));

                // Copy the count.
                ComputeBuffer.CopyCount(meshBuffer, argsBuffer, 0);
                
                // TODO: do not retrieve from GPU. write a compute shader to adjust count: https://gist.github.com/DuncanF/353509dd397ea5f292fa52d1b9b5133d
                // Retrieve it into array.
                int[] args = new int[4];
                argsBuffer.GetData(args);
                
                // Actual count in append buffer.
                args[0] *= 1; // verts per triangle
                // args[2] += 1; // verts per triangle

                print(args[0]);


                argsBuffer.SetData(args);
                
                fixArgs.SetBuffer(0, "DrawCallArgs", argsBuffer);
                fixArgs.Dispatch(0, 1,1,1);

                argsBuffer.GetData(args);
                
                print(args[0]);

                args[0] *= 2;
                
                var meshArr = new Vector4[args[0]];
                meshBuffer.GetData(meshArr);
                var verts3 = new Vector3[args[0]];
                // var verts4 = new Vector4[args[0]];
                var normals3 = new Vector3[args[0]];
                for (int i = 0; i < args[0]; i+=2) {
                    verts3[i / 2] = new Vector3(meshArr[i].x, meshArr[i].y, meshArr[i].z);
                    // verts4[i / 2] = meshArr[i];
                    normals3[i / 2] = new Vector3(meshArr[i+1].x, meshArr[i+1].y, meshArr[i+1].z);
                }
                mesh.vertices = verts3;
                mesh.normals = normals3;
                var tris = new int[verts3.Length];
                for (int i = 0; i < verts3.Length; i++) {
                    tris[i] = i;
                }
                mesh.triangles = tris;
                // meshBufferFloat.SetData(verts4);

                // // if (showMesh) {
                // meshMaterial.SetPass(0);
                // meshMaterial.SetMatrix(gridToWorldID, this.transform.localToWorldMatrix);
                // meshMaterial.SetBuffer(meshId, meshBufferFloat);

                // Graphics.DrawProceduralIndirect(meshMaterial, bounds, MeshTopology.Triangles, argsBuffer, 0, null, null, UnityEngine.Rendering.ShadowCastingMode.On, true);

                
            }
        }

        // void PickNextFunction()
        // {
        //     shapeFunction = transitionMode == TransitionMode.Cycle ?
        //         FunctionLibrary.GetNextFunctionName(shapeFunction) :
        //         FunctionLibrary.GetRandomFunctionNameOtherThan(shapeFunction);
        // }
    }
}