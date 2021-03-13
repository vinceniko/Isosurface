using UnityEngine;
using static UnityEngine.Mathf;

namespace Isosurface 
{
    public static class FunctionLibrary
    {
        public enum FunctionName { Sphere, Torus, Pyramid, Octahedron }

        static int functionLength = 4;

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
}