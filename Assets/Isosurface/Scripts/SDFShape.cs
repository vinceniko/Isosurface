using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Isosurface
{
    [ExecuteInEditMode]
    public class SDFShape : MonoBehaviour
    {
        // [SerializeField]
        // GPUGrid grid;

        [SerializeField, Range(1, 100)]
        int shapeSize = 40;

        [SerializeField]
        bool sineAnimate = true;

        [SerializeField]
        int rotationAngles = 20;

        float duration;

        // Update is called once per frame
        void Update()
        {
            if (sineAnimate)
            {
                duration += Time.deltaTime;
                var f = 2f;
                var sinShapeSize = shapeSize * ((((2f * Mathf.Sin((f * duration - 1f)/3.3f) + Mathf.Cos(f * duration)) / 3f))+1);
                // var sinShapeSize = shapeSize * ((((1.5f * Mathf.Sin(f * duration - 2.7f) * Mathf.Sin(3.3f * f * duration - 4.4f)) / 3f))+0.8f);

                transform.localScale = Vector3.one * sinShapeSize * 2f;
            }
            else
            {
                transform.localScale = Vector3.one * shapeSize * 2f;
            }

            transform.Rotate(new Vector3(0f, rotationAngles * Time.deltaTime, 0f), Space.World);
            // grid.shapeSize = shapeSize;
            // grid.shapeToWorld = transform.localToWorldMatrix;
        }
    }
}
