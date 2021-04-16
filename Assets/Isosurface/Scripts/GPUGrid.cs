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

        const int minResolution = 1;
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
            surfacePointsBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, Marshal.SizeOf(typeof(Vector4)));
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
            float step = size / (float)resolution;
            isoValsShader.SetInt(resolutionId, resolution);
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

            int groups = Mathf.CeilToInt(resolution / 4f);
            isoValsShader.Dispatch(kernelIndex, groups, groups, groups);

            transparencyMaterial.SetBuffer(isoValsId, isoValsBuffer);
            transparencyMaterial.SetFloat(stepId, step);
            transparencyMaterial.SetInt(resolutionId, resolution);
            transparencyMaterial.SetMatrix(gridToWorldID, this.transform.localToWorldMatrix);
            transparencyMaterial.SetVector(viewDirID, -Camera.main.transform.forward);
            transparencyMaterial.SetFloat(pointBrightnessID, pointBrightness);
            transparencyMaterial.SetFloat(pointSizeID, pointSize);
            
            var bounds = new Bounds(Vector3.zero, Vector3.one * (size + step));
            // Graphics.DrawMeshInstancedProcedural(mesh, 0, transparencyMaterial, bounds, resolution * resolution * resolution);

            material.SetBuffer(isoValsId, isoValsBuffer);
            material.SetFloat(stepId, step);
            material.SetInt(resolutionId, resolution);
            material.SetMatrix(gridToWorldID, this.transform.localToWorldMatrix);
            material.SetVector(viewDirID, -Camera.main.transform.forward);
            material.SetFloat(pointBrightnessID, pointBrightness);
            material.SetFloat(pointSizeID, pointSize);
            
            Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, resolution * resolution * resolution);

            // surface points
            int surfacePointsKernel = surfacePointsShader.FindKernel("SurfacePointsKernel");
            surfacePointsShader.SetInt(resolutionId, resolution);
            surfacePointsShader.SetMatrix(gridToWorldID, this.transform.localToWorldMatrix);
            surfacePointsShader.SetFloat(stepId, step);
            surfacePointsShader.SetBuffer(surfacePointsKernel, isoValsId, isoValsBuffer);
            surfacePointsShader.SetBuffer(surfacePointsKernel, surfacePointsId, surfacePointsBuffer);
            // surfacePointsShader.SetMatrixArray(shapeToWorldID, shape.Select(v => v.transform.localToWorldMatrix).ToArray());
            surfacePointsShader.Dispatch(surfacePointsKernel, groups, groups, groups);

            // var data = new Vector3[resolution*resolution*resolution];
            // surfacePointsBuffer.GetData(data);
            // print(data[10]);

            surfacePointMaterial.SetFloat(stepId, step);
            surfacePointMaterial.SetInt(resolutionId, resolution);
            surfacePointMaterial.SetMatrix(gridToWorldID, this.transform.localToWorldMatrix);
            surfacePointMaterial.SetVector(viewDirID, -Camera.main.transform.forward);
            surfacePointMaterial.SetFloat(pointSizeID, pointSize);
            surfacePointMaterial.SetBuffer(surfacePointsId, surfacePointsBuffer);

            Graphics.DrawMeshInstancedProcedural(mesh, 0, surfacePointMaterial, bounds, resolution * resolution * resolution);
        }

        // void PickNextFunction()
        // {
        //     function = transitionMode == TransitionMode.Cycle ?
        //         FunctionLibrary.GetNextFunctionName(function) :
        //         FunctionLibrary.GetRandomFunctionNameOtherThan(function);
        // }
    }
}