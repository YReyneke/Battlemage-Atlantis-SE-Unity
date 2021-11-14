using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Spells/Missile Spell")]
public class MissileSpell : SpellEntity
{
    [SerializeField, Range(5, 50), Tooltip("Range (m) of missile spell")]
    private float missileRange = 30;

    [SerializeField, Range(1, 50), Tooltip("Maximum velocity (m/s) of missile spell")]
    private float maxSpeed = 10;

    [SerializeField, Range(0.5F, 10), Tooltip("Rotation speed (rev/s) of missile spell")]
    private float torque = 2;

    [SerializeField, Range(1, 100), Tooltip("Thrust force (N) of missile spell")]
    private float thrust = 20;

    [SerializeField, Tooltip("Audio to be played at launch")]
    private AudioSource launchSource;

    [SerializeField, Tooltip("Audio to be played after launch audio completes")]
    private AudioSource loopSource;

    [SerializeField, Tooltip("Delay from start of launch sound that loopSource is activated")]
    private float launchSoundTime;

    private Collider target;
    private Rigidbody rigidbodyComponent;
    private Vector3 startPos;
    private Vector3 aimPoint;

    private void FixedUpdate()
    {
        if (state == LaunchState.Released)
        {
            // Rotate towards target
            Vector3 newRotation = Vector3.RotateTowards(
                rigidbodyComponent.transform.forward,
                (aimPoint - rigidbodyComponent.transform.position).normalized,
                torque * Time.fixedDeltaTime,
                1
            );
            rigidbodyComponent.rotation = Quaternion.LookRotation(newRotation);

            // Accellerate forward
            rigidbodyComponent.AddRelativeForce(Vector3.forward * thrust);

            // Reduce velocity to maxSpeed
            float speed = rigidbodyComponent.velocity.magnitude;
            if (speed > maxSpeed)
            {
                rigidbodyComponent.velocity *= maxSpeed / speed;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (state == LaunchState.Released)
        {
            HitEntity(collision.collider);
        }
    }

    // SpellEntity functions
    protected override void Init()
    {
        GetComponent<MeshRenderer>().enabled = false;
        //Debug.Log("Missile Created - renderer disabled");
        rigidbodyComponent = GetComponent<Rigidbody>();
        torque *= 3.14159F; // convert rev/s into rad/s
    }

    protected override void PreLaunch()
    {
        Vector3 rayForwardDir = (transform.position - Camera.main.transform.position).normalized;
        Ray ray = new Ray(Camera.main.transform.position, rayForwardDir * missileRange);
        RaycastHit HitResult;
        //Debug.DrawRay(transform.position, rayForwardDir * missileRange, Color.yellow);
        if (Physics.Raycast(ray, out HitResult, missileRange, LayerMask.GetMask("Enemy Entities")))
        {
            target = HitResult.collider;
            target.SendMessage("UpdateHalfHeight", this, SendMessageOptions.RequireReceiver);
            aimPoint = tgtAimpoint;
            //Debug.Log($"Aimpoint for missile set: {tgtAimpoint}");
        }
    }

    protected override void PostLaunch() // Only runs if target != null
    {
        // End if somehow moved out of range
        if((transform.position - startPos).magnitude > missileRange)
        {
            EndSpell();
        }
    }

    protected override void LaunchFailed()
    {
        EndSpell();
    }

    protected override void EndSpell()
    {
        Destroy(gameObject);
    }

    protected override void HitEntity(Collider col)
    {
        col.SendMessage("DamageHit", Energy, SendMessageOptions.DontRequireReceiver);
        EndSpell();
    }

    /// <summary>
    /// Code run when player releases trigger
    /// </summary>
    /// <param name="ctrlr">LEM controller reference</param>
    /// <returns>True if missile locked on succesfully
    /// <br>False if missile did not lock to an enemy entity</br></returns>
    public override bool Release(LEMController ctrlr)
    {
        if(target != null)
        {
            transform.parent = null;
            state = LaunchState.Released;
            startPos = transform.position;
            GetComponent<MeshRenderer>().enabled = true;
            launchSource.Play();
            loopSource.PlayDelayed(launchSoundTime);
            //Debug.Log("Missile Released - renderer enabled");
            return true; // Target locked
        } else
        {
            //Debug.Log("Missile failed to launch: No Target!");
            state = LaunchState.Failed;
            return false; // No target locked
        }
    }
}
