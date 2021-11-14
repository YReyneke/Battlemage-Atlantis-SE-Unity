using UnityEngine;
using SpellcastDataModule;

/// <summary>
/// This script controls the LEM (Life Energy Manipulator) and provides the following features:
/// When the player pulls the trigger(s) it instantiates the selected spell object into the game world
/// Provided there are enough charges of a given spell on the LEM
/// It also updates the display of the hand controllers to show the player what spell's charges are available to fire
/// </summary>
public class LEMController : MonoBehaviour
{
    /* 
     * When player completes a spell - apply relevant spell
     * While player pulls trigger pre-launch of applied spell for that hand
     * When player releases trigger, launch the spell
     */
    [HideInInspector]
    public PlayerScriptHandler playerScriptHandler;

    [SerializeField, Tooltip("Grenade spell prefab")]
    private GrenadeSpell grenadeSpell;

    [SerializeField, Tooltip("Grenade offset from hand")]
    private Vector3 grenadeHandOffset;

    [SerializeField, Tooltip("Missile spell prefab")]
    private MissileSpell missileSpell;

    [SerializeField, Tooltip("Target locator of right hand")]
    private GameObject RHTargetLocator;

    [SerializeField, Tooltip("Target locator of left hand")]
    private GameObject LHTargetLocator;

    [SerializeField, Tooltip("Artillery spell prefab")]
    private ArtillerySpell artillerySpell;

    private Vector3 releaseVelocity;
    [HideInInspector]
    public Vector3 ReleaseVelocity { get { return releaseVelocity; } }

    private SpellEntity LHSpell;
    private SpellEntity RHSpell;
    private SpellEntity DualSpell;

    bool isLHTriggered = false;
    bool isRHTriggered = false;

    // Start is called before the first frame update
    void Start()
    {
        RHTargetLocator.SetActive(false);
        LHTargetLocator.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ActivateLHSpell()
    {
        if (!isLHTriggered)
        {
            isLHTriggered = true;
            if (playerScriptHandler.spellCaster.ActiveSpells.LHCharges > 0)
            {
                switch (playerScriptHandler.spellCaster.ActiveSpells.LHSpell)
                {
                    case SpellID.Grenade:
                        GrenadeSpell gndSpell = Instantiate(grenadeSpell, playerScriptHandler.inputController.LHController.transform);
                        gndSpell.transform.localPosition = grenadeHandOffset;
                        gndSpell.ConnectJoint(playerScriptHandler.inputController.LHController.GetComponent<Rigidbody>());
                        LHSpell = gndSpell;
                        break;
                    case SpellID.Laser:
                        break;
                    case SpellID.Missile:
                        LHTargetLocator.SetActive(true);
                        MissileSpell mslSpell = Instantiate(missileSpell, LHTargetLocator.transform);
                        LHSpell = mslSpell;
                        break;
                    case SpellID.Lightning:
                        break;
                    case SpellID.Artillery:
                        if (isRHTriggered)
                        {
                            ArtillerySpell atlSpell = Instantiate(artillerySpell, playerScriptHandler.spellCastingPointController.transform);
                            LHSpell = atlSpell;
                            RHSpell = atlSpell;
                        }
                        break;
                }
            }
        }
    }

    public void ActivateRHSpell()
    {
        if (!isRHTriggered)
        {
            isRHTriggered = true;
            if (playerScriptHandler.spellCaster.ActiveSpells.RHCharges > 0)
            {
                switch (playerScriptHandler.spellCaster.ActiveSpells.RHSpell)
                {
                    case SpellID.Grenade:
                        GrenadeSpell gndSpell = Instantiate(grenadeSpell, playerScriptHandler.inputController.RHController.transform);
                        gndSpell.transform.localPosition = grenadeHandOffset;
                        gndSpell.ConnectJoint(playerScriptHandler.inputController.RHController.GetComponent<Rigidbody>());
                        RHSpell = gndSpell;
                        break;
                    case SpellID.Laser:
                        break;
                    case SpellID.Missile:
                        RHTargetLocator.SetActive(true);
                        MissileSpell mslSpell = Instantiate(missileSpell, RHTargetLocator.transform);
                        RHSpell = mslSpell;
                        break;
                    case SpellID.Lightning:
                        break;
                    case SpellID.Artillery: // Dual cast spell
                        if (isLHTriggered)
                        {
                            ArtillerySpell atlSpell = Instantiate(artillerySpell, playerScriptHandler.spellCastingPointController.transform);
                            LHSpell = atlSpell;
                            RHSpell = atlSpell;
                        }
                        break;
                }
            }
        }
    }

    public void ReleaseLH()
    {
        isLHTriggered = false;
        releaseVelocity = playerScriptHandler.inputController.LHVelocity;
        LHTargetLocator.SetActive(false);
        if (LHSpell != null)
        {
            if (LHSpell.Release(this)) // If release was successful
            {
                playerScriptHandler.spellCaster.ActiveSpells.LHCharges -= 1; // reduce spell charges
            }
        }
    }

    public void ReleaseRH()
    {
        isRHTriggered = false;
        releaseVelocity = playerScriptHandler.inputController.RHVelocity;
        RHTargetLocator.SetActive(false);
        if (RHSpell != null)
        {
            if (RHSpell.Release(this)) // If release was successful
            {
                playerScriptHandler.spellCaster.ActiveSpells.RHCharges -= 1; // reduce spell charges
            }
        }
    }
}

