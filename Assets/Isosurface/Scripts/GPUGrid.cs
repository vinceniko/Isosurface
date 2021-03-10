using UnityEngine;

public class GPUGrid : MonoBehaviour
{
    [SerializeField]
    ComputeShader computeShader = default;

    static readonly int 
        isoValsId = Shader.PropertyToID("_IsoVals"),
        resolutionId = Shader.PropertyToID("_Resolution"),
		stepId = Shader.PropertyToID("_Step"),
		timeId = Shader.PropertyToID("_Time"),
		sId = Shader.PropertyToID("_ShapeSize"),
        modelToWorldID = Shader.PropertyToID("_ModelToWorld");

    const int maxResolution = 20;

    [SerializeField]
    int shapeSize = 1;

    [SerializeField]
    int size = 100;

    ComputeBuffer isoValsBuffer;

    [SerializeField]
	Material material = default;

	[SerializeField]
	Mesh mesh = default;

    [SerializeField, Range(5, maxResolution)]
    int resolution = 5;

    [SerializeField]
    FunctionLibrary.FunctionName function = FunctionLibrary.FunctionName.Sphere;

    // public enum TransitionMode { Cycle, Random }

    // [SerializeField]
    // TransitionMode transitionMode = TransitionMode.Cycle;

    // [SerializeField, Min(0f)]
    // float functionDuration = 1f, transitionDuration = 1f;

    float duration;

    // bool transitioning;

    // FunctionLibrary.FunctionName transitionFunction;

    // void Awake() 
    // {
    //     transform.position = Vector3.one * (-size * 0.5f + size / resolution);
    // }

    void OnEnable() 
    {
        isoValsBuffer = new ComputeBuffer(maxResolution * maxResolution * maxResolution, 4);
    }

    void OnDisable()
    {
        isoValsBuffer.Release();
        isoValsBuffer = null;
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

        UpdateFunctionOnGPU();
    }

    void UpdateFunctionOnGPU () {
		float step = size / resolution;
		computeShader.SetInt(resolutionId, resolution);
		computeShader.SetFloat(stepId, step);

        duration += Time.deltaTime;
		computeShader.SetFloat(sId, (shapeSize * (((Mathf.Sin(5f * duration) + 2f) * 0.5f))));

		computeShader.SetFloat(timeId, Time.time);
		computeShader.SetMatrix(modelToWorldID, this.transform.localToWorldMatrix);

        // var kernelIndex = (int)function;
        var kernelIndex = 1;
        computeShader.SetBuffer(kernelIndex, isoValsId, isoValsBuffer);

		int groups = Mathf.CeilToInt(resolution / 4f);
        computeShader.Dispatch(kernelIndex, groups, groups, groups);

        material.SetBuffer(isoValsId, isoValsBuffer);
		material.SetFloat(stepId, step);
		material.SetInt(resolutionId, resolution);
		material.SetMatrix(modelToWorldID, this.transform.localToWorldMatrix);
        
        var bounds = new Bounds(Vector3.zero, Vector3.one * (size + step));
		Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, resolution * resolution * resolution);
	}

    // void PickNextFunction()
    // {
    //     function = transitionMode == TransitionMode.Cycle ?
    //         FunctionLibrary.GetNextFunctionName(function) :
    //         FunctionLibrary.GetRandomFunctionNameOtherThan(function);
    // }
}