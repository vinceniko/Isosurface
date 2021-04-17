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

        static readonly int 
            isoValsId = Shader.PropertyToID("_IsoVals"),
            surfacePointsId = Shader.PropertyToID("_SurfacePoints"),
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

        ComputeBuffer isoValsBuffer;
        ComputeBuffer surfacePointsBuffer;
        ComputeBuffer surfacePointsCountBuffer = default;

        [SerializeField]
        Material material = default;

        [SerializeField]
        Material transparencyMaterial = default;
        [SerializeField]
        Material surfacePointMaterial = default;

        [SerializeField]
        Mesh mesh = default;

        [SerializeField, Range(0.0f, 0.99f)]
        float pointBrightness = 0.99f;

        [SerializeField]
        FunctionLibrary.FunctionName function = FunctionLibrary.FunctionName.Sphere;


        [SerializeField]
        bool showVolume = true;

        [SerializeField]
        bool showGrid = true;

        [SerializeField]
        bool showSurface = true;

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

        void OnEnable() 
        {
            isoValsBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, 4);
            surfacePointsBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, Marshal.SizeOf(typeof(Vector4)), ComputeBufferType.Append);
            surfacePointsCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
        }

        void OnDisable()
        {
            isoValsBuffer.Release();
            isoValsBuffer = null;
            surfacePointsBuffer.Release();
            surfacePointsBuffer = null;
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
            //     transitionFunction = function;
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

            var kernelIndex = (int)function;
            // var kernelIndex = 1;
            isoValsShader.SetBuffer(kernelIndex, isoValsId, isoValsBuffer);

            int groups = Mathf.CeilToInt(resolution_ / 4f);
            isoValsShader.Dispatch(kernelIndex, groups, groups, groups);
                
            var bounds = new Bounds(Vector3.zero, Vector3.one * (size + step));

            if (showGrid) {
                transparencyMaterial.SetBuffer(isoValsId, isoValsBuffer);
                transparencyMaterial.SetFloat(stepId, step);
                transparencyMaterial.SetInt(resolutionId, resolution_);
                transparencyMaterial.SetMatrix(gridToWorldID, this.transform.localToWorldMatrix);
                transparencyMaterial.SetVector(viewDirID, -Camera.main.transform.forward);
                transparencyMaterial.SetFloat(pointBrightnessID, pointBrightness);
                transparencyMaterial.SetFloat(pointSizeID, pointSize);
                
                Graphics.DrawMeshInstancedProcedural(mesh, 0, transparencyMaterial, bounds, resolution_ * resolution_ * resolution_);
            }

            if (showVolume) {
                material.SetBuffer(isoValsId, isoValsBuffer);
                material.SetFloat(stepId, step);
                material.SetInt(resolutionId, resolution_);
                material.SetMatrix(gridToWorldID, this.transform.localToWorldMatrix);
                material.SetVector(viewDirID, -Camera.main.transform.forward);
                material.SetFloat(pointBrightnessID, pointBrightness);
                material.SetFloat(pointSizeID, pointSize);
                
                Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, resolution_ * resolution_ * resolution_);
            }

            // surface points
            surfacePointsBuffer.SetCounterValue(0);

            int surfacePointsKernel = surfacePointsShader.FindKernel("SurfacePointsKernel");
            surfacePointsShader.SetInt(resolutionId, resolution_);
            surfacePointsShader.SetMatrix(gridToWorldID, this.transform.localToWorldMatrix);
            surfacePointsShader.SetFloat(stepId, step);
            surfacePointsShader.SetBuffer(surfacePointsKernel, isoValsId, isoValsBuffer);
            surfacePointsShader.SetBuffer(surfacePointsKernel, surfacePointsId, surfacePointsBuffer);
            // surfacePointsShader.SetMatrixArray(shapeToWorldID, shape.Select(v => v.transform.localToWorldMatrix).ToArray());
            surfacePointsShader.Dispatch(surfacePointsKernel, groups, groups, groups);

            // Copy the count.
            ComputeBuffer.CopyCount(surfacePointsBuffer, surfacePointsCountBuffer, 0);
            
            // Retrieve it into array.
            int[] counter = new int[1] { 0 };
            surfacePointsCountBuffer.GetData(counter);
            
            // Actual count in append buffer.
            int count = counter[0]; // <-- This is the answer

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

                Graphics.DrawMeshInstancedProcedural(mesh, 0, surfacePointMaterial, bounds, count);
            }
        }

        // void PickNextFunction()
        // {
        //     function = transitionMode == TransitionMode.Cycle ?
        //         FunctionLibrary.GetNextFunctionName(function) :
        //         FunctionLibrary.GetRandomFunctionNameOtherThan(function);
        // }
    }
}