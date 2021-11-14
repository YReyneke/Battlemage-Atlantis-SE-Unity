using System.Collections.Generic;
using UnityEngine;
using SpellcastDataModule;

[AddComponentMenu("Spells/Grenade Spell")]
public class GrenadeSpell : SpellEntity
{
    [SerializeField, Tooltip("Grenade spell rigidbody")]
    Rigidbody RB;

    [SerializeField, Tooltip("The grenade's fixed joint component")]
    private FixedJoint throwJoint;

    [SerializeField, Tooltip("Outer kill radius (meters) of grenade"), Range(1, 20)]
    private float killRadius = 2;

    [SerializeField, Range(1, 30), Tooltip("Outer damage radius (meters) of grenade\nMax damage falls to 0 between killRadius and dmgRadius")]
    private float damageRadius = 10;

    private AudioSource grenadeBounce;

    private void Start()
    {
        grenadeBounce = GetComponent<AudioSource>();
        damageRadius = (damageRadius < killRadius) ? killRadius : damageRadius;
    }

    public void ConnectJoint(Rigidbody connectObject)
    {
        throwJoint.connectedBody = connectObject;
    }

    private void Explode()
    {
        //Debug.Log("Grenade Exploding!");
        Collider[] HitEnemies = Physics.OverlapSphere(transform.position, damageRadius, LayerMask.GetMask("Enemy Entities"));
        foreach(Collider c in HitEnemies)
        {
            HitEntity(c);
        }
    }

    /*
     * Inherited Functions
     */
    protected override void Init()
    {

    }

    protected override void PreLaunch()
    {
        //Debug.Log("Updating prelaunch");
    }

    protected override void PostLaunch()
    {
        //Debug.Log("Updating postLaunch");
    }

    protected override void LaunchFailed()
    {
        EndSpell();
    }

    protected override void EndSpell()
    {
        Explode();
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
        objectToHit.SendMessage("DamageHit", damageToSend, SendMessageOptions.DontRequireReceiver);
    }

    public override bool Release(LEMController ctrlr)
    {
        transform.parent = null;
        state = LaunchState.Released;
        Destroy(throwJoint);
        RB.velocity = ctrlr.ReleaseVelocity;
        return true;
    }

    private void OnCollisionEnter()
    {
        //Debug.Log("Bounce!");
        grenadeBounce.Play();
    }
}
