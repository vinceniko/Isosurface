using UnityEngine;
using static UnityEngine.Mathf;

public static class FunctionLibrary
{

    public delegate Vector3 Function(float u, float v, float t);

    public enum FunctionName { Sphere, Wave, MultiWave, Ripple, Torus }

    static int functionLength = 5;

    public static FunctionName GetNextFunctionName(FunctionName name)
    {
        return (int)name < functionLength - 1 ? name + 1 : 0;
    }

    public static FunctionName GetRandomFunctionNameOtherThan(FunctionName name)
    {
        var choice = (FunctionName)Random.Range(1, functionLength);
        return choice == name ? 0 : choice;
    }
}