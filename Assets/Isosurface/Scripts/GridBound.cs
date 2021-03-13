using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Isosurface 
{
    [ExecuteInEditMode]
    public class GridBound : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            transform.localScale = Vector3.one * transform.parent.gameObject.GetComponent<GPUGrid>().size;
        }
    }
}
