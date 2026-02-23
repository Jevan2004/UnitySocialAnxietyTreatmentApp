using UnityEngine;

public class otherWalkScript : MonoBehaviour
{
    public HingeJoint upperLegL;
    public HingeJoint lowerLegL;
    public HingeJoint upperLegR;
    public HingeJoint lowerLegR;

    public float walkSpeed = 2f;
    public float legSwing = 50f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Keep upright
        Vector3 up = transform.up;
        Vector3 torque = Vector3.Cross(up, Vector3.up);
        rb.AddTorque(torque * 500f);

        // Forward movement
        rb.linearVelocity = new Vector3(
            transform.forward.x * walkSpeed,
            rb.linearVelocity.y,
            transform.forward.z * walkSpeed
        );
    }


    void SetMotor(HingeJoint hinge, float targetVelocity)
    {
        if (hinge == null) return;

        JointMotor motor = hinge.motor;
        motor.force = 200f;
        motor.targetVelocity = targetVelocity;
        hinge.motor = motor;
        hinge.useMotor = true;
    }
}
