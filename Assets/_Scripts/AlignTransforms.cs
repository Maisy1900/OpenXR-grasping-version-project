using UnityEngine;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

public class AlignTransforms : MonoBehaviour
{
    [System.Serializable]
    public struct ReferencePoint
    {
        public Vector3 pointA; // Point in mocap system A
        public Vector3 pointB; // Corresponding point in mocap system B
    }

    public List<ReferencePoint> referencePoints = new List<ReferencePoint>();
    private List<Vector3> collectedPointsA = new List<Vector3>();
    private List<Vector3> collectedPointsB = new List<Vector3>();
    public Transform pointA; // The transform from mocap system A
    public Transform pointB; // The transform from mocap system B

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            CollectSample();
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            if (referencePoints.Count >= 3)
            {
                AlignSystems();
            }
            else
            {
                Debug.LogError("At least 3 reference points are required for alignment.");
            }
        }
    }

    void CollectSample()
    {
        Vector3 positionA = pointA.position;
        Vector3 positionB = pointB.position;

        collectedPointsA.Add(positionA);
        collectedPointsB.Add(positionB);

        ReferencePoint rp = new ReferencePoint
        {
            pointA = positionA,
            pointB = positionB
        };

        referencePoints.Add(rp);

        Debug.Log("Collected sample: A=" + positionA + ", B=" + positionB);
    }

    void AlignSystems()
    {
        // Step 1: Compute Centroids
        Vector3 centroidA = ComputeCentroid(referencePoints, true);
        Vector3 centroidB = ComputeCentroid(referencePoints, false);

        // Step 2: Remove the Translation Component
        List<Vector3> zeroMeanA = GetZeroMeanPoints(referencePoints, centroidA, true);
        List<Vector3> zeroMeanB = GetZeroMeanPoints(referencePoints, centroidB, false);

        // Step 3: Compute the Rotation Matrix
        Matrix4x4 rotationMatrix = ComputeRotationMatrix(zeroMeanA, zeroMeanB);

        PlayerPrefs.SetFloat("m00", rotationMatrix[row: 0, column: 0]);
        PlayerPrefs.SetFloat("m01", rotationMatrix[row: 0, column: 1]);
        PlayerPrefs.SetFloat("m02", rotationMatrix[row: 0, column: 2]);
        PlayerPrefs.SetFloat("m03", rotationMatrix[row: 0, column: 3]);

        PlayerPrefs.SetFloat("m10", rotationMatrix[row: 1, column: 0]);
        PlayerPrefs.SetFloat("m11", rotationMatrix[row: 1, column: 1]);
        PlayerPrefs.SetFloat("m12", rotationMatrix[row: 1, column: 2]);
        PlayerPrefs.SetFloat("m13", rotationMatrix[row: 1, column: 3]);

        PlayerPrefs.SetFloat("m20", rotationMatrix[row: 2, column: 0]);
        PlayerPrefs.SetFloat("m21", rotationMatrix[row: 2, column: 1]);
        PlayerPrefs.SetFloat("m22", rotationMatrix[row: 2, column: 2]);
        PlayerPrefs.SetFloat("m23", rotationMatrix[row: 2, column: 3]);

        PlayerPrefs.SetFloat("m30", rotationMatrix[row: 3, column: 0]);
        PlayerPrefs.SetFloat("m31", rotationMatrix[row: 3, column: 1]);
        PlayerPrefs.SetFloat("m32", rotationMatrix[row: 3, column: 2]);
        PlayerPrefs.SetFloat("m33", rotationMatrix[row: 3, column: 3]);
         
        // Step 4: Compute the Translation Vector
        Vector3 translationVector = centroidB - rotationMatrix.MultiplyPoint3x4(centroidA);
        PlayerPrefs.SetFloat("translationvector_x", translationVector.x);
        PlayerPrefs.SetFloat("translationvector_y", translationVector.y);
        PlayerPrefs.SetFloat("translationvector_z", translationVector.z);


        // Output the transformation
        Debug.Log("Rotation Matrix:\n" + rotationMatrix);
        Debug.Log("Translation Vector: " + translationVector);

        // Step 6: Apply the Transformation to verify
        foreach (var rp in referencePoints)
        {
            Vector3 transformedPoint = rotationMatrix.MultiplyPoint3x4(rp.pointA) + translationVector;
            Debug.Log("Original Point in B: " + rp.pointB + " Transformed Point: " + transformedPoint);
        }
    }

    Vector3 ComputeCentroid(List<ReferencePoint> points, bool isSystemA)
    {
        Vector3 centroid = Vector3.zero;
        foreach (var point in points)
        {
            centroid += isSystemA ? point.pointA : point.pointB;
        }
        return centroid / points.Count;
    }

    List<Vector3> GetZeroMeanPoints(List<ReferencePoint> points, Vector3 centroid, bool isSystemA)
    {
        List<Vector3> zeroMeanPoints = new List<Vector3>();
        foreach (var point in points)
        {
            zeroMeanPoints.Add((isSystemA ? point.pointA : point.pointB) - centroid);
        }
        return zeroMeanPoints;
    }

    Matrix4x4 ComputeRotationMatrix(List<Vector3> zeroMeanA, List<Vector3> zeroMeanB)
    {
        // Create a 3x3 matrix H
        var H = Matrix<double>.Build.Dense(3, 3);

        for (int i = 0; i < zeroMeanA.Count; i++)
        {
            var a = zeroMeanA[i];
            var b = zeroMeanB[i];
            H[0, 0] += a.x * b.x;
            H[0, 1] += a.x * b.y;
            H[0, 2] += a.x * b.z;
            H[1, 0] += a.y * b.x;
            H[1, 1] += a.y * b.y;
            H[1, 2] += a.y * b.z;
            H[2, 0] += a.z * b.x;
            H[2, 1] += a.z * b.y;
            H[2, 2] += a.z * b.z;
        }

        // Perform SVD
        var svd = H.Svd();

        var U = svd.U;
        var S = svd.S;
        var VT = svd.VT;

        // Compute the rotation matrix
        var rotationMatrix = U * VT;

        // Ensure it's a proper rotation matrix
        if (rotationMatrix.Determinant() < 0)
        {
            for (int i = 0; i < 3; i++)
            {
                VT[2, i] *= -1;
            }
            rotationMatrix = U * VT;
        }

        // Convert Math.NET Numerics matrix to Unity Matrix4x4
        Matrix4x4 unityRotationMatrix = new Matrix4x4(
            new Vector4((float)rotationMatrix[0, 0], (float)rotationMatrix[1, 0], (float)rotationMatrix[2, 0], 0),
            new Vector4((float)rotationMatrix[0, 1], (float)rotationMatrix[1, 1], (float)rotationMatrix[2, 1], 0),
            new Vector4((float)rotationMatrix[0, 2], (float)rotationMatrix[1, 2], (float)rotationMatrix[2, 2], 0),
            new Vector4(0, 0, 0, 1)
        );

        return unityRotationMatrix;
    }
}
