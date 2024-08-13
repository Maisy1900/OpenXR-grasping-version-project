using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QTM_ApplyTransform : MonoBehaviour
{
    public Transform meta_camera; 

    Matrix4x4 rotationMatrix = new Matrix4x4();

    Vector3 translationvector = new Vector3(); 

    // Start is called before the first frame update
    void Start()
    {
       float m00 = PlayerPrefs.GetFloat("m00");
       float m01 = PlayerPrefs.GetFloat("m01");
       float m02 = PlayerPrefs.GetFloat("m02");
       float m03 = PlayerPrefs.GetFloat("m03");

       float m10 = PlayerPrefs.GetFloat("m10");
       float m11 = PlayerPrefs.GetFloat("m11");
       float m12 = PlayerPrefs.GetFloat("m12");
       float m13 = PlayerPrefs.GetFloat("m13");
              
       float m20 = PlayerPrefs.GetFloat("m20");
       float m21 = PlayerPrefs.GetFloat("m21");
       float m22 = PlayerPrefs.GetFloat("m22");
       float m23 = PlayerPrefs.GetFloat("m23");

       float m30 = PlayerPrefs.GetFloat("m30");
       float m31 = PlayerPrefs.GetFloat("m31");
       float m32 = PlayerPrefs.GetFloat("m32");
       float m33 = PlayerPrefs.GetFloat("m33");

       // Compute the Translation Vector
       float tx = PlayerPrefs.GetFloat("translationvector_x");
       float ty = PlayerPrefs.GetFloat("translationvector_y");
       float tz = PlayerPrefs.GetFloat("translationvector_z");
       translationvector = new Vector3(tx,ty,tz);


        // Convert Math.NET Numerics matrix to Unity Matrix4x4
        rotationMatrix = new Matrix4x4(
            new Vector4(m00, m10, m20, m30),
            new Vector4(m01, m11, m21, m31),
            new Vector4(m02, m12, m22, m32),
            new Vector4(m03, m13, m23, m33)
        );

    }

    // Update is called once per frame
    void Update()
    {
        Vector3 transformedPoint = rotationMatrix.MultiplyPoint3x4(meta_camera.position) + translationvector;
        transform.position = transformedPoint;
    }
}
