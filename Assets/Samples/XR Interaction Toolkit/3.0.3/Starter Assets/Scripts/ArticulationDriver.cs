using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
//using TMPro;
//using NumpyDotNet; 
//using NumSharp;

public enum Hand
{
    None,
    Right,
    Left,
};

public class ArticulationDriver_v2 : MonoBehaviour
{
    public Hand handedness = Hand.Right;

    // Physics body driver
    public ArticulationBody _palmBody;
    public Transform driverHand;

    public Transform[] driverJoints;
    public ArticulationBody[] articulationBods;
    public Transform driverHandRoot;
    public Vector3 driverHandOffset;
    public Vector3 rotataionalOffset;
    public CapsuleCollider[] _capsuleColliders;
    public BoxCollider[] _palmColliders;
    public Text infoText;
    public Text angleDisplayText;

    [Range(0f,50f)]
    public float xoffset = 15f;
    [Range(0f, 50f)]
    public float yoffset = 15f;

    //offsets
    [Range(-50f, 50f)] public float[] XOffsets = new float[15];
    [Range(-50f, 50f)] public float[] YOffsets = new float[15];
    [Range(-50f, 50f)] public float[] ZOffsets = new float[15];

    ArticulationBody thisArticulation; // Root-Parent articulation body 
    float xTargetAngle, yTargetAngle = 0f;

    [Range(-90f, 90f)]
    public float angle = 0f;

    float[] initialXAngles = new float[15];
    float[] initialYAngles = new float[15];
    float[] initialZAngles = new float[15];

    public void MeasureInitialAngles()
    {
        for (int k = 0; k < driverJoints.Length; k++)
        {
            initialXAngles[k] = driverJoints[k].transform.localRotation.eulerAngles.x;
            initialYAngles[k] = driverJoints[k].transform.localRotation.eulerAngles.y;
            initialZAngles[k] = driverJoints[k].transform.localRotation.eulerAngles.z;
        }
    }

    void Start()
    {
        thisArticulation = GetComponent<ArticulationBody>();
    }

    void FixedUpdate()
    {
        # region Wrist movement 

        //Quaternion rotWithOffset = driverHandRoot.rotation * Quaternion.Euler(rotataionalOffset); ;
        //thisArticulation.TeleportRoot(driverHandRoot.position, rotWithOffset);

        // Counter Gravity; force = mass * acceleration
        _palmBody.AddForce(-Physics.gravity * _palmBody.mass);
        foreach (ArticulationBody body in articulationBods)
        {
            //int dofs = body.jointVelocity.dofCount;
            float velLimit = 1.75f;
            body.maxAngularVelocity = velLimit;
            body.maxDepenetrationVelocity = 3f;

            body.AddForce(-Physics.gravity * body.mass);
        }

        // Apply tracking position velocity; force = (velocity * mass) / deltaTime
        float massOfHand = _palmBody.mass; // + (N_FINGERS * N_ACTIVE_BONES * _perBoneMass);
        Vector3 palmDelta = ((driverHand.transform.position + driverHandOffset) +
          (driverHand.transform.rotation * Vector3.back * driverHandOffset.x) +
          (driverHand.transform.rotation * Vector3.up * driverHandOffset.y)) - _palmBody.worldCenterOfMass;

        // Setting velocity sets it on all the joints, adding a force only adds to root joint
        //_palmBody.velocity = Vector3.zero;
        float alpha = 0.05f; // Blend between existing velocity and all new velocity
        _palmBody.velocity *= alpha;
        _palmBody.AddForce(Vector3.ClampMagnitude((((palmDelta / Time.fixedDeltaTime) / Time.fixedDeltaTime) * (_palmBody.mass + (1f * 5))) * (1f - alpha), 8000f * 1f));

        // Apply tracking rotation velocity 
        // TODO: Compensate for phantom forces on strongly misrotated appendages
        // AddTorque and AngularVelocity both apply to ALL the joints in the chain
        Quaternion palmRot = _palmBody.transform.rotation * Quaternion.Euler(rotataionalOffset);
        Quaternion rotation = driverHand.transform.rotation * Quaternion.Inverse(palmRot);
        Vector3 angularVelocity = Vector3.ClampMagnitude((new Vector3(
          Mathf.DeltaAngle(0, rotation.eulerAngles.x),
          Mathf.DeltaAngle(0, rotation.eulerAngles.y),
          Mathf.DeltaAngle(0, rotation.eulerAngles.z)) / Time.fixedDeltaTime) * Mathf.Deg2Rad, 45f * 1f);
        //palmBody.angularVelocity = Vector3.zero;
        //palmBody.AddTorque(angularVelocity);
        _palmBody.angularVelocity = angularVelocity;
        _palmBody.angularDamping = 15f;

        #endregion

        // *******************************************************************************************
        // *******************************************************************************************
        // *******************************************************************************************

        #region Stabilize ArticulationBody / Prevent Random Jittering
        foreach (BoxCollider collider in _palmColliders)
        {
            collider.enabled = false;
        }
        foreach (CapsuleCollider collider in _capsuleColliders)
        {
            collider.enabled = false;
        }
        for (int a = 0; a < articulationBods.Length; a++)
        {
            articulationBods[a].jointVelocity = new ArticulationReducedSpace(0f, 0f, 0f);
            articulationBods[a].velocity = Vector3.zero;
            articulationBods[a].angularVelocity = Vector3.zero;
        }
        foreach (BoxCollider collider in _palmColliders)
        {
            collider.enabled = true;
        }
        foreach (CapsuleCollider collider in _capsuleColliders)
        {
            collider.enabled = true;
        }
        #endregion

        // *******************************************************************************************
        // *******************************************************************************************
        // *******************************************************************************************

        #region Finger movement
        if(Input.GetKeyDown(KeyCode.M)) // Measure initial angles of each driver joint and use it later
        {
            int k = 0; 
            foreach(Transform jointTF in driverJoints)
            {
                initialXAngles[k] = driverJoints[k].transform.localRotation.eulerAngles.x;
                initialYAngles[k] = driverJoints[k].transform.localRotation.eulerAngles.y;
                initialZAngles[k] = driverJoints[k].transform.localRotation.eulerAngles.z;
                k++;
            }
        }

        for (int i = 0; i < driverJoints.Length; i++)
        {
            float tempAngXProx = driverJoints[i].transform.localRotation.eulerAngles.x - XOffsets[i];
            float tempAngYProx = driverJoints[i].transform.localRotation.eulerAngles.y - YOffsets[i];
            float tempAngZProx = driverJoints[i].transform.localRotation.eulerAngles.z - ZOffsets[i];

            float ang_targXProx = CalculateBendingAngle(initialXAngles[i], tempAngXProx);
            float ang_targYProx = CalculateBendingAngle(initialYAngles[i], tempAngYProx);
            float ang_targZProx = CalculateBendingAngle(initialZAngles[i], tempAngZProx);
            // RotateTo(articulationBods[i], ang_targXProx, ang_targYProx, ang_targZProx);

            if (articulationBods[i].tag.Contains("xyrotations"))
            {
                RotateTo(articulationBods[i], ang_targXProx, ang_targYProx);
            }
            else if (articulationBods[i].tag.Contains("xrotations"))
            {
                RotateTo(articulationBods[i], ang_targXProx);
            }
            else
            {
                if(handedness == Hand.Right)
                    RotateTo(articulationBods[i], ang_targZProx, -ang_targXProx, ang_targYProx);
                else
                    RotateTo(articulationBods[i],- ang_targZProx, ang_targXProx, ang_targYProx);
            }
        }
        #endregion
    }

    float NormalizeAngle(float angle)
    {
        return angle % 360;
    }
    float CalculateBendingAngle(float initialAngle, float currentAngle)
    {
        float normalizedInitial = NormalizeAngle(initialAngle);
        float normalizedCurrent = NormalizeAngle(currentAngle);

        // If there's a significant change (like jumping from 360 to 0 or vice versa)
        if (Mathf.Abs(normalizedCurrent - normalizedInitial) > 180)
        {
            if (normalizedCurrent > normalizedInitial)
            {
                normalizedInitial += 360;
            }
            else
            {
                normalizedCurrent += 360;
            }
        }

        return normalizedCurrent - normalizedInitial;
    }

    // 3 versions of the RotateTo functions (overloaded) 
    void RotateTo(ArticulationBody body, float targetTor)
    {
        body.xDrive = new ArticulationDrive()
        {
            stiffness = body.xDrive.stiffness,
            forceLimit = body.xDrive.forceLimit,
            damping = body.xDrive.damping,
            lowerLimit = body.xDrive.lowerLimit,
            upperLimit = body.xDrive.upperLimit,
            target = targetTor
        };
    }
    //fingers rotateTo function
    void RotateTo(ArticulationBody body, float targetTorX, float targetTorY)
    {
        body.xDrive = new ArticulationDrive()
        {
            stiffness = body.xDrive.stiffness,
            forceLimit = body.xDrive.forceLimit,
            damping = body.xDrive.damping,
            lowerLimit = body.xDrive.lowerLimit,
            upperLimit = body.xDrive.upperLimit,
            target = targetTorX
        };
        body.yDrive = new ArticulationDrive()
        {
            stiffness = body.yDrive.stiffness,
            forceLimit = body.yDrive.forceLimit,
            damping = body.yDrive.damping,
            lowerLimit = body.yDrive.lowerLimit,
            upperLimit = body.yDrive.upperLimit,
            target = targetTorY
        };
    }

    void RotateTo(ArticulationBody body, float targetTorX, float targetTorY, float targetTorZ)
    {
        body.xDrive = new ArticulationDrive()
        {
            stiffness = body.xDrive.stiffness,
            forceLimit = body.xDrive.forceLimit,
            damping = body.xDrive.damping,
            lowerLimit = body.xDrive.lowerLimit,
            upperLimit = body.xDrive.upperLimit,
            target = targetTorX
        };
        body.yDrive = new ArticulationDrive()
        {
            stiffness = body.yDrive.stiffness,
            forceLimit = body.yDrive.forceLimit,
            damping = body.yDrive.damping,
            lowerLimit = body.yDrive.lowerLimit,
            upperLimit = body.yDrive.upperLimit,
            target = targetTorY
        };
        body.zDrive = new ArticulationDrive()
        {
            stiffness = body.zDrive.stiffness,
            forceLimit = body.zDrive.forceLimit,
            damping = body.zDrive.damping,
            lowerLimit = body.zDrive.lowerLimit,
            upperLimit = body.zDrive.upperLimit,
            target = targetTorZ
        };
    }

    void RotateTo(ArticulationBody articulation, Vector3 targetTor)
    {
        #region Approach 1
        //articulation.xDrive = new ArticulationDrive()
        //{
        //    target = targetTor.x
        //};

        //articulation.yDrive = new ArticulationDrive()
        //{
        //    target = targetTor.y
        //};

        //articulation.zDrive = new ArticulationDrive()
        //{
        //    target = targetTor.z
        //};
        #endregion

        #region Approach 2
        var driveX = articulation.xDrive;
        driveX.target = targetTor.x;
        articulation.xDrive = driveX;

        var driveY = articulation.yDrive;
        driveY.target = targetTor.y;
        articulation.yDrive = driveY;

        var driveZ = articulation.zDrive;
        driveZ.target = targetTor.z;
        articulation.zDrive = driveZ;
        #endregion  
    }

    public static float map(float value, float leftMin, float leftMax, float rightMin, float rightMax)
    {
        return rightMin + (value - leftMin) * (rightMax - rightMin) / (leftMax - leftMin);
    }

    public static float MapAngle(float value, float leftMin, float leftMax, float rightMin, float rightMax)
    {
        // Perform the mapping
        return rightMin + (value - leftMin) * (rightMax - rightMin) / (leftMax - leftMin);
    }
    /*if the min/max angles are far apart then restrict between larger number to smaller number
     * eg
     * if the min angle is 355 and max is 10 then restrict monvement between 355 to 10 degrees and restrict movement between 10 and 355
     * 
     */
}