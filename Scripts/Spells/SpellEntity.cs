using UnityEngine;
using SpellcastDataModule;

/// <summary>
/// Contains update function from monobehaviour do not override Update()
/// </summary>
public abstract class SpellEntity : MonoBehaviour
{
    protected enum LaunchState
    {
        PreLaunch,
        Released,
        Failed
    }
    protected LaunchState state = LaunchState.PreLaunch;

    [SerializeField, Tooltip("Spell energy - dictates maximum damage/heal energy of this spell")]
    protected float energy;
    public float Energy { get { return energy; } }

    [HideInInspector]
    public Vector3 tgtAimpoint = new Vector3();

    [SerializeField, Tooltip("Time this spell exists for after launch (s)\nThis also acts as grenade timer")]
    private float spellTimer = 5;

    /// <summary>
    /// Remaining time this spell will exist (seconds)
    /// </summary>
    public float Lifetime { get { return spellTimer; } }

    void Start()
    {
        Init();
    }

    void Update()
    {
        switch (state)
        {
            case LaunchState.PreLaunch: // Pre launch state
                PreLaunch();
                break;
            case LaunchState.Released: // Post launch state
                spellTimer -= Time.deltaTime;
                //Debug.Log($"Countdown: {countDown}");
                if (spellTimer <= 0)
                {
                    EndSpell();
                }
                PostLaunch();
                break;
            case LaunchState.Failed: // Pre launch failed state
                LaunchFailed();
                break;
        }
    }

    /// <summary>
    /// Runs in Start() - used to set up the spell
    /// </summary>
    protected abstract void Init();

    /// <summary>
    /// Code that runs during prelaunch state within Update()
    /// <br>This function or Release() must set 'state' to Released</br>
    /// </summary>
    protected abstract void PreLaunch();

    /// <summary>
    /// Code that runs while state.Released within Update()
    /// <br>Must call HitEntity</br>
    /// </summary>
    protected abstract void PostLaunch();

    protected abstract void LaunchFailed();

    /// <summary>
    /// Code that defines what happens when game objects are hit by this spell
    /// </summary>
    /// <param name="objectToHit">The entity to hit with this spell</param>
    protected abstract void HitEntity(Collider objectToHit); // Might want to change this to Enemy instead of GameObject!

    /// <summary>
    /// Called when lifetime runs out
    /// </summary>
    protected abstract void EndSpell();

    /// <summary>
    /// Called by input controller when player releases trigger
    /// <br>This function or PreLaunch() must set 'state' to Released</br>
    /// </summary>
    public abstract bool Release(LEMController ctrlr);
}
