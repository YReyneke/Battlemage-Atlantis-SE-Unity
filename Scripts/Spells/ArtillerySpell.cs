using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Spells/Artillery Spell")]
public class ArtillerySpell : SpellEntity
{
    private enum FlightStage
    {
        Initial,
        Horizontal,
        Strike
    }

    [SerializeField, Tooltip("The game object that represents where the artillery will land")]
    private GameObject targetLocation; // Displayed during pre-launch state

    [SerializeField, Range(3, 60), Tooltip("Maximum range artillery spell will look for a target during prelaunch")]
    private float artilleryRange = 30;

    [SerializeField, Range(1, 50), Tooltip("Maximum speed of the artillery shell after launch")]
    private float maxSpeed = 10;

    [SerializeField, Range(1, 100), Tooltip("Thrust force (N) of missile spell")]
    private float thrust = 20;

    [SerializeField, Range(10, 100), Tooltip("Drop height from which the artillery spell falls")]
    private float dropHeight = 50;

    [SerializeField, Range(1, 60), Tooltip("Duration of 'horizontal' flight section before dropping the shell")]
    private float dropDelayTime = 5;

    [SerializeField, Range(1, 50), Tooltip("Maximum distance the artillery shell will hit with total damage")]
    private float killRadius = 5;

    [SerializeField, Range(1, 50), Tooltip("Maximum distance the artillery shell will hit with any damage")]
    private float damageRadius = 10;

    private Rigidbody rigidbodyComponent;
    private float dropDelayCounter = 0;
    private bool hasTarget = false;
    private Vector3 calculatedDropPoint;
    private Vector3 aimPoint;

    FlightStage stage = FlightStage.Initial;

    private void FixedUpdate()
    {
        if (state == LaunchState.Released)
        {
            switch (stage)
            {
                case FlightStage.Initial:
                    if (transform.position.y > calculatedDropPoint.y)
                    {
                        // Hide the fake and pause all motion
                        GetComponent<MeshRenderer>().enabled = false;
                        rigidbodyComponent.velocity = new Vector3();
                        rigidbodyComponent.angularVelocity = new Vector3();
                        rigidbodyComponent.ResetInertiaTensor();
                        stage = FlightStage.Horizontal;
                    }
                    else
                    {
                        // Accellerate forward
                        rigidbodyComponent.AddRelativeForce(Vector3.forward * thrust);

                        // Reduce velocity to maxSpeed
                        float speed = rigidbodyComponent.velocity.magnitude;
                        if (speed > maxSpeed)
                        {
                            rigidbodyComponent.velocity *= maxSpeed / speed;
                        }
                    }
                    break;
                case FlightStage.Horizontal:
                    dropDelayCounter += Time.fixedDeltaTime;
                    if (dropDelayCounter >= dropDelayTime)
                    {
                        transform.position = calculatedDropPoint;
                        transform.forward = Vector3.down;
                        rigidbodyComponent.isKinematic = false;
                        rigidbodyComponent.useGravity = true;
                        rigidbodyComponent.velocity = Vector3.down * maxSpeed;
                        GetComponent<MeshRenderer>().enabled = true;
                        stage = FlightStage.Strike;
                    }
                    break;
                case FlightStage.Strike:
                    // The artillery shell is falling - Hurray!
                    break;
            }
        }
    }

    /*
     * Inherited Functions
     */
    protected override void Init()
    {
        rigidbodyComponent = GetComponent<Rigidbody>();
        GetComponent<MeshRenderer>().enabled = false; // Hide artillery mesh
        targetLocation.transform.parent = null; // Disconnect aimpoint transform from this object
        targetLocation.SetActive(false); // Disable locator object until raycast picks up an aimpoint
    }

    protected override void PreLaunch()
    {
        //Debug.Log("Updating prelaunch");
        Vector3 rayForwardDir = (transform.position - Camera.main.transform.position).normalized;
        Ray ray = new Ray(Camera.main.transform.position, rayForwardDir * artilleryRange);
        RaycastHit HitResult;
        //Debug.DrawRay(Camera.main.transform.position, rayForwardDir * artilleryRange);
        if (Physics.Raycast(ray, out HitResult, artilleryRange))
        {
            targetLocation.SetActive(true);
            targetLocation.transform.position = HitResult.point;
            //Debug.Log($"Aimpoint for artillery set: {targetLocation.transform.position}");
            hasTarget = true;
        }
    }

    protected override void PostLaunch()
    {
        //Debug.Log("Updating postLaunch");
        // This section is not required - all updates are done in FixedUpdate
    }

    protected override void LaunchFailed()
    {
        EndSpell();
    }

    protected override void EndSpell()
    {
        Destroy(targetLocation);
        Destroy(gameObject);
    }

    protected override void HitEntity(Collider objectToHit)
    {
        float damageToSend = Energy;
        float distanceToTarget = (objectToHit.transform.position - transform.position).magnitude;
        if (distanceToTarget > killRadius)
        {
            distanceToTarget -= killRadius;
            damageToSend *= 1 - (distanceToTarget / (damageRadius - killRadius));
        }
        objectToHit.SendMessage("DamageHit", damageToSend, SendMessageOptions.RequireReceiver);
    }

    public override bool Release(LEMController ctrlr)
    {
        if (hasTarget)
        {
            GetComponent<MeshRenderer>().enabled = true; // Display Artillery shell
            aimPoint = targetLocation.transform.position;
            calculatedDropPoint = aimPoint;
            calculatedDropPoint.y += dropHeight;
            //targetLocation.SetActive(false); // Hide artillery aimpoint after release
            transform.parent = null; // Disconnect from midpoint between hands
            transform.forward = Vector3.up; // Fire artillery up
            state = LaunchState.Released;
            Debug.Log("Launched Artillery Spell");
            return true;
        }
        state = LaunchState.Failed;
        return false; // Artillery spell not fired
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (state == LaunchState.Released)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, damageRadius, LayerMask.GetMask("Enemy Entities"));
            if (hitColliders.Length > 0)
            {
                foreach (var col in hitColliders)
                {
                    HitEntity(col);
                }
            }
            EndSpell();
        }
    }
}
