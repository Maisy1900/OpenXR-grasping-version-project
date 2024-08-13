using System.Collections;
using System.Collections.Generic;
using Transform3DBestFit;
using UnityEngine;

#if DONT_COMPILE
namespace VRLab.QTMTracking
{
public class FindTransform : MonoBehaviour
{
    [InspectorButton("CalculateTransformManual", ButtonWidth = 200)]
    public bool PerformCalculation = false;
    public AnchorBasedTransform anchorTransform;
    public AnchorQTMTransform qtmTransform;
    public AnchorDummyTransform dummyTransform;
    public Matrix4x4 transformMatrix4x4;

    private void OnValidate()
    {
        CalculateTransformManual();
    }

    public void CalculateTransform(AnchorBasedTransform _anchorTransform, AnchorQTMTransform _qtmTransform, AnchorDummyTransform _dummyTransform)
    {
        anchorTransform = _anchorTransform;
        qtmTransform = _qtmTransform;
        dummyTransform = _dummyTransform;
        Invoke("CalculateTransformManual", 1.0f);
    }

    internal void CalculateTransformManual()
    {
        if (!qtmTransform.hasValidData || !dummyTransform.hasValidData || !anchorTransform.hasValidData) return;

        Transform3D t3d = new Transform3D(anchorTransform.UpdateArrays(), qtmTransform.UpdateArrays());

        t3d.CalcTransform(t3d.actualsMatrix, t3d.nominalsMatrix);

        transformMatrix4x4 = Transform3d.ConvertToMatrix4x4(t3d.TransformMatrix);
        UpdateSlaveTransform();
    }

    private Matrix4x4 ConvertToMatrix4x4(double[,] source)
    {
        Matrix4x4 newMatrix = Matrix4x4.identity;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                newMatrix[i, j] = (float)source[i, j];
            }
        }
        return newMatrix;
    }

    public void UpdateSlaveTransform()
    {
        if (dummyTransform != null)
        {
            Matrix4x4 newMatrix = transformMatrix4x4.inverse * qtmTransform.transform.localToWorldMatrix;
            dummyTransform.transform.position = newMatrix.GetPosition();
            dummyTransform.transform.rotation = newMatrix.rotation; 
        }
    }
}
}
#endif