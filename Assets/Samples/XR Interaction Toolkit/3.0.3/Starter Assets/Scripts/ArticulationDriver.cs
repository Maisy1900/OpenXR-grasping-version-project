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

public class ArticulationDriver : MonoBehaviour
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


    ArticulationBody thisArticulation; // Root-Parent articulation body 
    float xTargetAngle, yTargetAngle = 0f;

    [Range(-90f, 90f)]
    public float angle = 0f;

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

        // Index finger  
        
        float ang_targX = 0f;
        float tempAng = 0f;
        float tempAngY = 0f;
        float ang_targY = 0f;
        /*
        if (articulationBods[0]) // R_IndexProximal
        {
            tempAng = NormalizeAngle(driverJoints[0].transform.localRotation.eulerAngles.z);
            ang_targX = MapAngle(tempAng, 2.774716f, 22.92734f, -10.6921f, 76.78067f);

            tempAngY = NormalizeAngle(driverJoints[0].transform.localRotation.eulerAngles.x);
            ang_targY = MapAngle(tempAngY, 349.3079f, 76.78067f, 76.78067f, 76.78067f); // Verify the mapping

            RotateTo(articulationBods[0], ang_targX, ang_targY);
            infoText.text = "Angles: " + tempAngY.ToString("F2");
        }
        */
        if (articulationBods[1]) // R_IndexIntermediate
        {
           tempAng = NormalizeAngle(driverJoints[1].transform.localRotation.eulerAngles.z);
           ang_targX = MapAngle(tempAng, 356.9656f, 356.9691f, 15.05899f, 15.65378f);

            //tempAngY = NormalizeAngle(driverJoints[1].transform.localRotation.eulerAngles.x);
            //ang_targY = MapAngle(tempAngY, 15.05899f, 15.65378f, 15.65378f, 15.65378f);

            Debug.Log("Intermediate tempAng: " + tempAng + ", ang_targX: " + ang_targX);
            Debug.Log("Intermediate tempAngY: " + tempAngY + ", ang_targY: " + ang_targY);

            RotateTo(articulationBods[1], ang_targX, ang_targY);
            infoText.text = "Angles: " + tempAngY.ToString("F2");
        }
        /*
        if (articulationBods[2]) // R_IndexDistal
        {
            // Get the Z-axis rotation angle of the third driver joint and normalize it
            tempAng = NormalizeAngle(driverJoints[2].transform.localRotation.eulerAngles.z);
            // Map the Z-axis rotation angle to a new range for target X angle
            ang_targX = MapAngle(tempAng, 358.0546f, 358.2885f, -2.7183f, 61.23251f);

            // Get the X-axis rotation angle of the third driver joint and normalize it
            tempAngY = NormalizeAngle(driverJoints[2].transform.localRotation.eulerAngles.x);
            // Map the X-axis rotation angle to a new range for target Y angle
            ang_targY = MapAngle(tempAngY, 357.2817f, 61.23251f, 61.23251f, 61.23251f);

            // Rotate the third articulation body to the target angles
            RotateTo(articulationBods[2], ang_targX, ang_targY);

            // Update the infoText with the current Y angle formatted to two decimal places
            //infoText.text = "Angles: " + tempAngY.ToString("F2");
        }
        */
        // RotateTo(articulationBods[0], 50f, 15f, 0f);
        // RotateTo(articulationBods[1], 50f, 15f, 0f);
        // RotateTo(articulationBods[2], 50f, 15f, 0f);

        // Middle Finger 
        RotateTo(articulationBods[3], 50f, 15f, 0f);
        RotateTo(articulationBods[4], 50f, 15f, 0f);
        RotateTo(articulationBods[5], 50f, 15f, 0f);
        //Little Finger
        RotateTo(articulationBods[6], 50f, 15f, 0f);
        RotateTo(articulationBods[7], 50f, 15f, 0f);
        RotateTo(articulationBods[8], 50f, 15f, 0f);
        //Ring Finger
        RotateTo(articulationBods[9], 50f, 15f, 0f);
        RotateTo(articulationBods[10], 50f, 15f, 0f);
        RotateTo(articulationBods[11], 50f, 15f, 0f);
        //Thumb
        RotateTo(articulationBods[12], 50f, 15f, 0f);
        RotateTo(articulationBods[13], 50f, 15f, 0f);
        RotateTo(articulationBods[14], 50f, 15f, 0f);
        #endregion
        

        /*
        #region Finger and thumb 
        for (int i = 0; i < driverJoints.Length; i++)
        {
            float ang_targX = 0f;
            float tempAng = 0f;
            float tempAngY = 0f;
            float ang_targY = 0f;

            // # Add more thumb DoFs. Perhaps change thumb joint type form revolute to spherical 
            if (driverJoints[i].name.Contains("Thumb"))
            {
                // For the left and right hand thumb joint 0 the direction in one of the axis is reversed 
                if (handedness == Hand.Right)
                {
                    if (driverJoints[i].name.Contains("ThumbMetacarpal"))
                    {
                        // X-Axis joint control
                        tempAng = driverJoints[i].transform.localRotation.eulerAngles.z;
                        if (tempAng < 100f) { xTargetAngle = tempAng + 360f; }
                        else { xTargetAngle = tempAng; }
                        xTargetAngle = tempAng;
                        ang_targX = map(xTargetAngle, 365f, 300f, -29f, 29f); // angle;
                                                                              //infoText.text = "ProxyHand v0.2 \nAngles: " + xTargetAngle.ToString("F2");

                        // Z-Axis joint control
                        tempAngY = driverJoints[i].transform.localRotation.eulerAngles.y;
                        if (tempAngY < 100f) { yTargetAngle = tempAngY + 360f; }
                        else { yTargetAngle = tempAngY; }
                        yTargetAngle = tempAngY;
                        ang_targY = map(yTargetAngle, 300f, 285f, -15, 5f);
                        //infoText.text = "ProxyHand v0.2 \nAngles: " + ang_targY.ToString("F2");
                    }
                    else if (driverJoints[i].name.Contains("ThumbProximal"))
                    {
                        tempAngY = driverJoints[i].transform.localRotation.eulerAngles.y;
                        if (tempAngY < 100f) { yTargetAngle = tempAngY + 360f; }
                        else { yTargetAngle = tempAngY; }
                        yTargetAngle = tempAngY;
                        ang_targY = map(yTargetAngle, 330f, 25f, -10f, 50f);
                        //infoText.text = "ProxyHand v0.1 \nAngles: " + ang_targY.ToString("F2");
                    }
                    else
                    {
                        tempAng = driverJoints[i].transform.localRotation.eulerAngles.z;
                        if (tempAng < 100f) { xTargetAngle = tempAng + 360f; }
                        else { xTargetAngle = tempAng; }

                        ang_targX = map(xTargetAngle, 380f, 300f, -40f, 80f);
                    }
                }
                else if (handedness == Hand.Left)
                {
                    if (driverJoints[i].name.Contains("thumb0"))
                    {
                        // X-Axis joint control
                        tempAng = driverJoints[i].transform.localRotation.eulerAngles.z;
                        if (tempAng < 100f) { xTargetAngle = tempAng + 360f; }
                        else { xTargetAngle = tempAng; }
                        xTargetAngle = tempAng;
                        ang_targX = map(xTargetAngle, 365f, 300f, -29f, 29f); // angle;
                        //infoText.text = "ARiHand v0.2 \nAngles: " + xTargetAngle.ToString("F2");

                        // Z-Axis joint control
                        tempAngY = driverJoints[i].transform.localRotation.eulerAngles.y;
                        if (tempAngY < 100f) { yTargetAngle = tempAngY + 360f; }
                        else { yTargetAngle = tempAngY; }
                        yTargetAngle = tempAngY;
                        ang_targY = map(yTargetAngle, 300f, 285f, -15, 5f);
                        //infoText.text = "ProxyHand v0.2 \nAngles: " + ang_targY.ToString("F2");
                    }
                    else if (driverJoints[i].name.Contains("thumb1"))
                    {
                        tempAngY = driverJoints[i].transform.localRotation.eulerAngles.y;
                        if (tempAngY < 100f) { yTargetAngle = tempAngY + 360f; }
                        else { yTargetAngle = tempAngY; }
                        yTargetAngle = tempAngY;
                        ang_targY = map(yTargetAngle, 330f, 25f, -10f, 50f);
                        //infoText.text = "ProxyHand v0.1 \nAngles: " + ang_targY.ToString("F2");
                    }
                    else
                    {
                        tempAng = driverJoints[i].transform.localRotation.eulerAngles.z;
                        if (tempAng < 100f) { xTargetAngle = tempAng + 360f; }
                        else { xTargetAngle = tempAng; }

                        ang_targX = map(xTargetAngle, 380f, 300f, -40f, 80f);
                    }
                }

                    RotateTo(articulationBods[i], ang_targX, ang_targY);
            }
            else
            {
                tempAng = driverJoints[i].transform.localRotation.eulerAngles.z;
                //if (tempAng < 100f) { xTargetAngle = tempAng + 360f; }
                //else { xTargetAngle = tempAng; }
                ang_targX = tempAng; // map(xTargetAngle, 372f, 270f, -10f, 85f);
                //RotateTo(articulationBods[i], ang_targ);

                tempAngY = driverJoints[i].transform.localRotation.eulerAngles.x;
                //if (tempAngY < 100f) { yTargetAngle = tempAngY + 360f; }
                //else { yTargetAngle = tempAngY; }
                yTargetAngle = tempAngY;
                ang_targY = map(yTargetAngle, 10, 1f, 0f, 10f);
                //infoText.text = "ProxyHand v0.1 \nAngles: " + yTargetAngle.ToString("F2") + " Mapped Angle: " + ang_targY.ToString("F2");
                RotateTo(articulationBods[i], ang_targX, ang_targY);
            }

            if (driverJoints[i].tag == "b_l_index1")
            {
                infoText.text = "Index Prox: " + tempAng.ToString("F2");
                tempAng = driverJoints[i].transform.localRotation.eulerAngles.z;
                ang_targX = map(tempAng, 2.6f, 11f, -10f, 85f);

                tempAngY = driverJoints[i].transform.localRotation.eulerAngles.x;

                ang_targY = map(tempAngY, 10, 1f, 0f, 10f);
                RotateTo(articulationBods[i], ang_targX, ang_targY);

                infoText.text = "Angles: " + tempAngY.ToString("F2");
            }

        }
        */
        /*
        for (int i = 0; i < driverJoints.Length; i++)
        {
            float currentAngle = driverJoints[i].localRotation.eulerAngles.z;
            minAngles[i] = Mathf.Min(minAngles[i], currentAngle);
            maxAngles[i] = Mathf.Max(maxAngles[i], currentAngle);
        }
        */
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
        body.zDrive = new ArticulationDrive()
        {
            stiffness = body.zDrive.stiffness,
            forceLimit = body.zDrive.forceLimit,
            damping = body.zDrive.damping,
            lowerLimit = body.zDrive.lowerLimit,
            upperLimit = body.zDrive.upperLimit,
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
    float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F) angle += 360F;
        if (angle > 360F) angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }

    public static float NormalizeAngle(float angle)
    {
        angle = angle % 360;
        if (angle < 0) angle += 360;
        return angle;
    }
    public static float MapAngle(float value, float leftMin, float leftMax, float rightMin, float rightMax)
    {
        // Normalize the input value to the 0-360 range
        value = NormalizeAngle(value);
        leftMin = NormalizeAngle(leftMin);
        leftMax = NormalizeAngle(leftMax);

        // Perform the mapping
        return rightMin + (value - leftMin) * (rightMax - rightMin) / (leftMax - leftMin);
    }

}