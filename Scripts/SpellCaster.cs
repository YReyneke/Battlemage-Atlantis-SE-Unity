using UnityEngine;
using SpellcastDataModule;

/// <summary>
/// This script performs the entire spellcasting process
/// <br>If a cast is successful, the relevant spell object is created</br>
/// <br>If a cast is unsuccessful, this script will notify the player</br>
/// </summary>
/*
 * The spellcasting process:
 * When player is not trying to cast/launch a spell, If spell is armed to launch - update pre-launch of spell as required
 * 
 * When player is trying to cast a spell
 *     If spellcasting just started - reset checker values for all spells of relevant hand(s)
 *     Update spell checker
 *         Verify that current hand position is valid for at least one spell
 *         If player fails to cast a correct spell, notify & stop checking
 *         
 *         Otherwise:
 *             For all valid spells - (Single handed/dual casting as relevant)
 *                 If current completed path is <= first path, increase scale of spell as relevent
 *                 Check if player has moved controller too far from intended path - Invalidate any spell that is out of range
 *                      ***NOTE: Casting accuracy tests for LH must convert data for symmetry (see SpellcastDataModule.HelperFunctions)***
 *                 If final path completes, stop checking and set the spell to LEM
 *                      Remember to check for post-separate case i.e. RH finished successfully while LH is still going (inherent now)
 *                 If not, display the casting nodes for the remaining spells
 *             
 *
 * When player stops casting (function activated by InputController on button release)
 *     reset casting status for relevant hand
 *
 * When player attempts to launch a spell
 *     Check if a spell is available to launch - notify player if not
 *     Launch Spell
 */
[AddComponentMenu("Player Scripts/Spell Caster")]
public class SpellCaster : MonoBehaviour
{
    // Connected scripts
    [HideInInspector]
    public PlayerScriptHandler playerScriptHandler; // Automatically assigned by playerScriptHandler

    InputController inputController; // reference to script

    [SerializeField, Range(0.1F, 0.4F), Tooltip("Minimum distance in meters player must move hand to reach second casting point")]
    private float minSpellScale = 0.2F;

    [SerializeField, Range(0, 0.2F), Tooltip("Maximum distance in meters player can move hand incorrectly before spell fails")]
    private float maxMoveError = 0.05F;
    
    [SerializeField, Range(0, 90), Tooltip("Maximum angle in degrees player can rotate hand incorrectly before spell fails")]
    private float maxRotationError = 60;

    /*[SerializeField, Tooltip("When selected will allow single casting spells to be cast during dualcasting state")]
    private bool enableSingleSpellDualCasting = false;*/ // Nice idea but currently breaks the logic - maybe implement later

    private SpellList Spells;
    private ActiveSpells activeSpells = new ActiveSpells();

    public ref SpellData[] AllSpells => ref Spells.AllSpells;
    public ref ActiveSpells ActiveSpells { get { return ref activeSpells; } }

    // Start is called before the first frame update
    void Start()
    {
        // Generate spell list object
        Spells = ScriptableObject.CreateInstance<SpellList>();

        // Link input controller script
        inputController = playerScriptHandler.inputController;
    }

    // Update is called once per frame
    void Update()
    {
        // update spellcasting system
        switch (GetCastingType())
        {
            case CastingType.None:
                // Update ActiveSpells object as required
                UpdateActiveSpells();
                break;
            case CastingType.Dual:
                UpdateDualSpellStates();
                break;
            case CastingType.Separate:
                UpdateLHSpellStates();
                UpdateRHSpellStates();
                break;
            case CastingType.LH:
                UpdateLHSpellStates();
                break;
            case CastingType.RH:
                UpdateRHSpellStates();
                break;
        }

        // Visualise spellcasting points as required
        playerScriptHandler.spellCastingPointController.UpdateSpellcastingPoints();
    }

    /*
     * Private functions section
     */

    /// <summary>
    /// This function ensures that while dualcasting the LHCharges match the RHCharges once a spell is released
    /// </summary>
    private void UpdateActiveSpells()
    {
        // Ensure dualcasting charges match in both hands
        if(activeSpells.LHSpell == activeSpells.RHSpell && activeSpells.LHSpell == activeSpells.DualSpell)
        {
            if(activeSpells.LHCharges != activeSpells.RHCharges)
            {
                activeSpells.LHCharges = (activeSpells.LHCharges < activeSpells.RHCharges) ? activeSpells.LHCharges : activeSpells.RHCharges;
                activeSpells.RHCharges = (activeSpells.LHCharges < activeSpells.RHCharges) ? activeSpells.LHCharges : activeSpells.RHCharges;
            }
        }
    }

    /// <summary>
    /// Linker function to update spell states for dual casting
    /// </summary>
    private void UpdateDualSpellStates()
    {
        if (activeSpells.DualState == CastState.Ready) // Dualcasting just started
        {
            //Debug.Log("Dual spellcasting started");
            ResetRHSpellStates();
            activeSpells.RHState = CastState.Casting;
            ResetLHSpellStates();
            activeSpells.LHState = CastState.Casting;
            activeSpells.DualState = CastState.Casting;
        } else
        {
            if(activeSpells.DualState == CastState.Casting)
            {
                bool RHValid = ValidateRHSpells();
                bool LHValid = ValidateLHSpells();
                if(!RHValid || !LHValid)
                {
                    Debug.Log("Dualcasting failed - no spells match that movement!");
                    activeSpells.RHState = CastState.Failed;
                    activeSpells.LHState = CastState.Failed;
                    activeSpells.DualState = CastState.Failed;
                }
            }
        }
    }

    /// <summary>
    /// Linker function to update spell states for LH only
    /// </summary>
    private void UpdateLHSpellStates()
    {
        if (activeSpells.LHState == CastState.Ready)// If player only just started casting a spell
        {
            //Debug.Log("LH spellcasting started");
            ResetLHSpellStates();
            activeSpells.LHState = CastState.Casting;
        }
        else // If casting is already underway
        {
            if (activeSpells.LHState == CastState.Casting) // If a spell can still be cast
            {
                //Debug.Log("Validating LH Spells");
                if (!ValidateLHSpells())  // returns false if no spell validated
                {
                    Debug.Log("LH Casting failed - no spells match that movement!");
                    activeSpells.LHState = CastState.Failed;
                }
            }
        }
    }

    /// <summary>
    /// Linker function to update spell states for RH only
    /// </summary>
    private void UpdateRHSpellStates()
    {
        if (activeSpells.RHState == CastState.Ready)// If player only just started casting a spell
        {
            //Debug.Log("RH spellcasting started");
            ResetRHSpellStates();
            activeSpells.RHState = CastState.Casting;
        }
        else // If casting is already underway
        {
            if (activeSpells.RHState == CastState.Casting) // If a spell can still be cast
            {
                //Debug.Log("Validating RH Spells");
                if (!ValidateRHSpells())  // returns false if no spell validated
                {
                    Debug.Log("RH Casting failed - no spells match that movement!");
                    activeSpells.RHState = CastState.Failed;
                }
            }
        }
    }

    /// <summary>
    /// Request current casting status from inputController
    /// </summary>
    /// <returns><see cref="SpellcastDataModule.CastingType">Casting type</see> based on current player input</returns>
    private CastingType GetCastingType()
    {
        if(inputController.IsDualCasting)
            return CastingType.Dual;
        
        if (inputController.IsRHCasting && inputController.IsLHCasting)
            return CastingType.Separate;

        if (inputController.IsRHCasting)
            return CastingType.RH;

        if (inputController.IsLHCasting)
            return CastingType.LH;

        return CastingType.None;
    }

    /// <summary>
    /// Resets casting state variables to default for all RH spells
    /// </summary>
    private void ResetRHSpellStates()
    {
        for (int i = 0; i < Spells.AllSpells.Length; i++)
        {
            Spells.AllSpells[i].isRHComplete = false;
            Spells.AllSpells[i].isRHValid = true;
            Spells.AllSpells[i].rhScalar = minSpellScale;
            for (int j = 0; j < Spells.AllSpells[i].Points.Length; j++)
            {
                Spells.AllSpells[i].Points[j].isRHComplete = false;
            }
        }
    }

    /// <summary>
    /// Resets casting state variables to default for all LH spells
    /// </summary>
    private void ResetLHSpellStates()
    {
        for (int i = 0; i < Spells.AllSpells.Length; i++)
        {
            Spells.AllSpells[i].isLHComplete = false;
            Spells.AllSpells[i].isLHValid = true;
            Spells.AllSpells[i].lhScalar = minSpellScale;
            for (int j = 0; j < Spells.AllSpells[i].Points.Length; j++)
            {
                Spells.AllSpells[i].Points[j].isLHComplete = false;
            }
        }
    }

    /// <summary>
    /// Check validity of LH for remaining spells - updates state of all spells based on results
    /// </summary>
    /// <returns>false if no LH spell found to be valid</returns>
    private bool ValidateLHSpells()
    {
        bool areAnyValid = false;
        for (int i=0; i < Spells.AllSpells.Length; i++)
        {
            /* 
             * If player is dual casting and current spell is a dual spell, Check the hand position/spell as dual casting
             * If not dual casting and current spell is not a dual spell, check the hand position/spell as non-dual casting
             * Otherwise ignore checks
             * NOTE: LH checks require converting from standard orientation to mirrored as spell data is configured for RH
             */
            if (Spells.AllSpells[i].isLHValid)
            {
                // If dual casting and current spell is not a dual spell
                if (GetCastingType() == CastingType.Dual && !Spells.AllSpells[i].IsDualOnly)
                {
                    Spells.AllSpells[i].isLHValid = false; // Current spell is not valid
                    Debug.Log($"{Spells.AllSpells[i].ID} invalidated for LH: Dual casting");
                    continue; // Skip checks
                }
                // If not dual casting and current spell is a dual spell
                if (GetCastingType() != CastingType.Dual && Spells.AllSpells[i].IsDualOnly)
                {
                    Spells.AllSpells[i].isLHValid = false; // Current spell is not valid
                    Debug.Log($"{Spells.AllSpells[i].ID} invalidated for LH: Not dual casting");
                    continue; // Skip checks
                }

                // Check initial transfrom if cast just started
                if (!Spells.AllSpells[i].Points[0].isLHComplete)
                {
                    //Debug.Log($"Initial point")
                    Vector3 pointPos = HelperFunctions.InvertedPos(Spells.AllSpells[i].Points[0].PointPosition) * Spells.AllSpells[i].lhScalar;
                    if(!IsPointComplete( // If initial transform of hand is out of tolerance
                            inputController.LHDeltaPos, inputController.LHDeltaRot,
                            pointPos, // Initial casting point pos will always be 0,0,0
                            HelperFunctions.InvertedRot(Spells.AllSpells[i].Points[0].PointRotation),
                            maxMoveError, maxRotationError
                            ))
                    {
                        Spells.AllSpells[i].isLHValid = false; // Current spell is not valid
                        Debug.Log($"{Spells.AllSpells[i].ID} invalidated for LH: Initial orientation incorrect\n" +
                            $"Pos: {inputController.LHDeltaPos}; Rot: {inputController.LHDeltaRot}");
                        continue; // skip checks
                    }
                }

                int nextPID = HelperFunctions.FindNextLHPoint(ref Spells.AllSpells[i]); // Find next motion endpoint
                
                if (nextPID == -1) // All points are completed, stop checking and set spell to LEM
                {
                    Debug.Log($"{Spells.AllSpells[i].ID} spell completed for LH");
                    activeSpells.LHState = CastState.Complete;

                    if (GetCastingType() != CastingType.Dual) // If not dual casting apply spell to LH on LEM
                    {
                        activeSpells.LHSpell = Spells.AllSpells[i].ID;
                        activeSpells.LHCharges = Spells.AllSpells[i].DefaultCharges;
                    }
                    else // If dual casting
                    {
                        if(activeSpells.RHState == CastState.Complete) // If both hands are done casting apply spell to LEM
                        {
                            Debug.Log("Dual casting successful");
                            activeSpells.DualState = CastState.Complete;
                            /*activeSpells.DualSpell = Spells.AllSpells[i].ID;
                            activeSpells.DualCharges = Spells.AllSpells[i].DefaultCharges;*/ // Initially intended to allow dual spells to be cast simultaneously to single hand spells - this has now been reconcidered
                            // And as such all successful spell casting will overwrite the active spell(S) of the LEM
                            activeSpells.LHSpell = Spells.AllSpells[i].ID;
                            activeSpells.LHCharges = Spells.AllSpells[i].DefaultCharges;
                            activeSpells.RHSpell = Spells.AllSpells[i].ID;
                            activeSpells.RHCharges = Spells.AllSpells[i].DefaultCharges;
                        }
                    }
                    return true;
                }
                else // There are still incomplete points
                {
                    // Check hand motion position in tolerance - NOTE: inverting pos & rot for LH
                    Vector3 castPointPos = HelperFunctions.InvertedPos(Spells.AllSpells[i].Points[nextPID].PointPosition) * Spells.AllSpells[i].lhScalar; // Next pos
                    Vector3 preCastPointPos = HelperFunctions.InvertedPos(Spells.AllSpells[i].Points[nextPID - 1].PointPosition) * Spells.AllSpells[i].lhScalar; // Previous pos
                    Vector3 castPointRot = HelperFunctions.InvertedRot(Spells.AllSpells[i].Points[nextPID].PointRotation); // Next rot
                    Vector3 preCastPointRot = HelperFunctions.InvertedRot(Spells.AllSpells[i].Points[nextPID - 1].PointRotation); // Previous rot

                    if (nextPID <= 2) // First movement scales the cast
                    {
                        float moveRange; // Largest movement in one axis - not magnitude!
                        moveRange = (Mathf.Abs(inputController.LHDeltaPos.x) >= Mathf.Abs(inputController.LHDeltaPos.y)) ?
                                     Mathf.Abs(inputController.LHDeltaPos.x) : Mathf.Abs(inputController.LHDeltaPos.y);
                        moveRange = (moveRange >= Mathf.Abs(inputController.LHDeltaPos.z)) ?
                                     moveRange : Mathf.Abs(inputController.LHDeltaPos.z);

                        if (nextPID == 1) // If within first movement
                        {
                            // Perform scaling
                            moveRange = (moveRange > minSpellScale) ? moveRange : minSpellScale;
                            Spells.AllSpells[i].lhScalar = moveRange;
                        }
                        else // nextPID == 2
                        {
                            if (IsPointComplete( // If still within bounds of previous point
                            inputController.LHDeltaPos, inputController.LHDeltaRot, // Start transform
                            preCastPointPos, preCastPointRot, // Previous casting point
                            maxMoveError, maxRotationError
                            ))
                            {
                                moveRange = (moveRange > minSpellScale) ? moveRange : minSpellScale;
                                Spells.AllSpells[i].lhScalar = moveRange;
                            }
                        }
                    }

                    //Debug.Log("Checking pos tolerance");
                    bool isPosIn = IsPointInTolerance(
                        inputController.LHDeltaPos,
                        preCastPointPos,
                        castPointPos,
                        maxMoveError,
                        Spells.AllSpells[i].Points[nextPID].CheckPosAxes
                        );

                    //Debug.Log("Checking rot tolerance");
                    bool isRotIn = IsPointInTolerance(
                        inputController.LHDeltaRot,
                        preCastPointRot,
                        castPointRot,
                        maxRotationError,
                        Spells.AllSpells[i].Points[nextPID].CheckRotAxes
                        );

                    if(!isPosIn || !isRotIn)
                    {
                        Debug.Log($"LH Position/Rotation out of bounds for spell {Spells.AllSpells[i].ID}");
                        Spells.AllSpells[i].isLHValid = false;
                    }
                    else
                    {
                        // Check if hand is at next motion point
                        bool isComplete = IsPointComplete(
                            inputController.LHDeltaPos, inputController.LHDeltaRot, // Start transform
                            castPointPos, castPointRot,
                            maxMoveError, maxRotationError
                            );
                        Spells.AllSpells[i].Points[nextPID].isLHComplete = isComplete; // Update complete status of this point for LH
                    }
                }
            }

            // Set to true if any spell is valid
            if (Spells.AllSpells[i].isLHValid)
                areAnyValid = true;
        }
        return areAnyValid;
    }

    /// <summary>
    /// Check validity of RH for remaining spells
    /// </summary>
    /// <returns>false if no RH spell found to be valid</returns>
    private bool ValidateRHSpells()
    {
        bool areAnyValid = false;
        for (int i = 0; i < Spells.AllSpells.Length; i++)
        {
            /* 
             * If player is dual casting and current spell is a dual spell, Check the hand position/spell as dual casting
             * If not dual casting and current spell is not a dual spell, check the hand position/spell as non-dual casting
             * Otherwise ignore checks
             */
            if (Spells.AllSpells[i].isRHValid)
            {
                // If dual casting and current spell is not a dual spell
                if (GetCastingType() == CastingType.Dual && !Spells.AllSpells[i].IsDualOnly)
                {
                    Spells.AllSpells[i].isRHValid = false; // Current spell is not valid
                    Debug.Log($"{Spells.AllSpells[i].ID} invalidated for RH: Dual casting");
                    continue; // Skip checks
                }

                // If not dual casting and current spell is a dual spell
                if (GetCastingType() != CastingType.Dual && Spells.AllSpells[i].IsDualOnly)
                {
                    Spells.AllSpells[i].isRHValid = false; // Current spell is not valid
                    Debug.Log($"{Spells.AllSpells[i].ID} invalidated for RH: Not dual casting");
                    continue; // Skip checks
                }

                // Check initial transfrom if cast just started
                if (!Spells.AllSpells[i].Points[0].isRHComplete)
                {
                    //Debug.Log($"Initial point")
                    if (!IsPointComplete( // If initial transform of hand is out of tolerance
                            inputController.RHDeltaPos, inputController.RHDeltaRot,
                            Spells.AllSpells[i].Points[0].PointPosition * Spells.AllSpells[i].rhScalar, // Initial casting point pos will always be 0,0,0
                            Spells.AllSpells[i].Points[0].PointRotation,
                            maxMoveError, maxRotationError
                            ))
                    {
                        Spells.AllSpells[i].isRHValid = false; // Current spell is not valid
                        Debug.Log($"{Spells.AllSpells[i].ID} invalidated for RH: Initial orientation incorrect\n" +
                            $"Pos: {inputController.RHDeltaPos}; Rot: {inputController.RHDeltaRot}");
                        continue; // skip checks
                    }
                }

                // Find next motion endpoint
                int nextPID = HelperFunctions.FindNextRHPoint(ref Spells.AllSpells[i]);

                // All points are completed, stop checking and set spell to LEM
                if (nextPID == -1)
                {
                    Debug.Log($"{Spells.AllSpells[i].ID} spell completed for RH");
                    activeSpells.RHState = CastState.Complete;

                    if (GetCastingType() != CastingType.Dual) // If not dual casting apply spell to LH on LEM
                    {
                        activeSpells.RHSpell = Spells.AllSpells[i].ID;
                        activeSpells.RHCharges = Spells.AllSpells[i].DefaultCharges;
                    }
                    else // If dual casting
                    {
                        if (activeSpells.LHState == CastState.Complete) // If both hands are done casting apply spell to LEM
                        {
                            activeSpells.DualState = CastState.Complete;
                            Debug.Log("Dual casting successful");
                            activeSpells.DualSpell = Spells.AllSpells[i].ID;
                            activeSpells.DualCharges = Spells.AllSpells[i].DefaultCharges;
                        }
                    }
                    return true;
                }
                else // There are still incomplete points
                {
                    // Check hand motion position in tolerance - NOTE: inverting pos & rot for LH
                    Vector3 castPointPos = Spells.AllSpells[i].Points[nextPID].PointPosition * Spells.AllSpells[i].rhScalar; // Next pos
                    Vector3 preCastPointPos = Spells.AllSpells[i].Points[nextPID - 1].PointPosition * Spells.AllSpells[i].rhScalar; // Previous pos
                    Vector3 castPointRot = Spells.AllSpells[i].Points[nextPID].PointRotation; // Next rot
                    Vector3 preCastPointRot = Spells.AllSpells[i].Points[nextPID - 1].PointRotation; // Previous rot

                    if (nextPID <= 2) // First movement scales the cast
                    {
                        float moveRange; // Largest movement in one axis - not magnitude!
                        moveRange = (Mathf.Abs(inputController.RHDeltaPos.x) >= Mathf.Abs(inputController.RHDeltaPos.y)) ?
                                     Mathf.Abs(inputController.RHDeltaPos.x) : Mathf.Abs(inputController.RHDeltaPos.y);
                        moveRange = (moveRange >= Mathf.Abs(inputController.RHDeltaPos.z)) ?
                                     moveRange : Mathf.Abs(inputController.RHDeltaPos.z);

                        if (nextPID == 1) // If within first movement
                        {
                            // Perform scaling
                            moveRange = (moveRange > minSpellScale) ? moveRange : minSpellScale;
                            Spells.AllSpells[i].rhScalar = moveRange;
                        }
                        else // nextPID == 2
                        {
                            if (IsPointComplete( // If still within bounds of previous point
                            inputController.RHDeltaPos, inputController.RHDeltaRot, // Start transform
                            preCastPointPos, preCastPointRot, // Previous casting point
                            maxMoveError, maxRotationError
                            ))
                            {
                                moveRange = (moveRange > minSpellScale) ? moveRange : minSpellScale;
                                Spells.AllSpells[i].rhScalar = moveRange;
                            }
                        }
                    }

                    //Debug.Log("Checking pos tolerance");
                    bool isPosIn = IsPointInTolerance(
                        inputController.RHDeltaPos,
                        preCastPointPos,
                        castPointPos,
                        maxMoveError,
                        Spells.AllSpells[i].Points[nextPID].CheckPosAxes
                        );

                    //Debug.Log("Checking rot tolerance");
                    bool isRotIn = IsPointInTolerance(
                        inputController.RHDeltaRot,
                        preCastPointRot,
                        castPointRot,
                        maxRotationError,
                        Spells.AllSpells[i].Points[nextPID].CheckRotAxes
                        );

                    if (!isPosIn || !isRotIn)
                    {
                        Debug.Log($"RH Position/Rotation out of bounds for spell {Spells.AllSpells[i].ID}");
                        Spells.AllSpells[i].isRHValid = false;
                    }
                    else
                    {
                        // Check if hand is at next motion point
                        bool isComplete = IsPointComplete(
                            inputController.RHDeltaPos, inputController.RHDeltaRot, // Start transform
                            castPointPos, castPointRot,
                            maxMoveError, maxRotationError
                            );
                        Spells.AllSpells[i].Points[nextPID].isRHComplete = isComplete; // Update complete status of this point for RH
                    }
                }
            }

            // Set to true if any spell is valid
            if (Spells.AllSpells[i].isRHValid)
                areAnyValid = true;
        }
        return areAnyValid;
    }

    /// <summary>
    /// <para>Used to check if one Vector3 is between two others, plus tolerance (maxError).
    /// <br>This works with both positional and rotational Vector3 units</br></para>
    /// NOTE: All vectors must be provided in same coordinate space to function correctly! i.e. spellcastingGrid
    /// </summary>
    /// <param name="vecToCheck">The current position/rotation vector</param>
    /// <param name="startPos">The initial reference point</param>
    /// <param name="endPos">The end reference point</param>
    /// <param name="maxError">The maximum out-of-bounds motion allowed</param>
    /// <param name="checkAxes">Set any axis to false to disable tolerance checks in that axis</param>
    /// <returns>true - If vecToCheck is between startPos and endPos.<br>false - If not</br></returns>
    private bool IsPointInTolerance(Vector3 vecToCheck, Vector3 startPos, Vector3 endPos, float maxError, CheckAxes checkAxes)
    {
        /*Debug.Log($"checkPos: {vecToCheck}; startPos: {startPos}; endPos: {endPos};\n" +
            $"maxError: {maxError}; checkX: {checkAxes.CheckX}, checkY: {checkAxes.CheckY}, checkZ: {checkAxes.CheckZ}");*/

        if (checkAxes.CheckX)
        {
            // Check x direction
            if (startPos.x <= endPos.x)
            {
                if (vecToCheck.x < startPos.x - maxError || // Moved too far left
                    vecToCheck.x > endPos.x   + maxError) // Moved too far right
                {
                    return false;
                }
            }
            else
            {
                if (vecToCheck.x > startPos.x + maxError || // Moved too far right
                    startPos.x   < endPos.x   - maxError) // Moved too far left
                {
                    return false;
                }
            }
        }

        if (checkAxes.CheckY)
        {
            // Check y direction
            if (startPos.y <= endPos.y)
            {
                if (vecToCheck.y < startPos.y - maxError || // Moved too far down
                    vecToCheck.y > endPos.y + maxError) // Moved too far up
                {
                    return false;
                }
            }
            else
            {
                if (vecToCheck.y > startPos.y + maxError || // Moved too far up
                    startPos.y < endPos.y - maxError) // Moved too far down
                {
                    return false;
                }
            }
        }

        if (checkAxes.CheckZ)
        {
            // Check z direction
            if (startPos.z <= endPos.z)
            {
                if (vecToCheck.z < startPos.z - maxError || // Moved too far back
                    vecToCheck.z > endPos.z + maxError) // Moved too far forward
                {
                    return false;
                }
            }
            else
            {
                if (vecToCheck.z > startPos.z + maxError || // Moved too far forward
                    startPos.z < endPos.z - maxError) // Moved too far back
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Compares two tranfoms to verify if one is within specific boundary conditions of the other
    /// </summary>
    /// <param name="posToCheck">The position to check</param>
    /// <param name="rotToCheck">The rotation to check</param>
    /// <param name="endPos">The position to compare with</param>
    /// <param name="endRot">The rotation to compare with</param>
    /// <param name="maxPosError">The maximum positional difference/axis to classify as within bounds</param>
    /// <param name="maxRotError">The maximum rotational difference/axis to classify as within bounds</param>
    /// <returns>True if transform.ToCheck is within maxError of transform.end</returns>
    private bool IsPointComplete(Vector3 posToCheck, Vector3 rotToCheck, Vector3 endPos, Vector3 endRot, float maxPosError, float maxRotError)
    {
        // Check position
        if (posToCheck.x > endPos.x + maxPosError || posToCheck.x < endPos.x - maxPosError)
            return false;
        if (posToCheck.y > endPos.y + maxPosError || posToCheck.y < endPos.y - maxPosError)
            return false;
        if (posToCheck.z > endPos.z + maxPosError || posToCheck.z < endPos.z - maxPosError)
            return false;

        // Check rotation
        if (rotToCheck.x > endRot.x + maxRotError || rotToCheck.x < endRot.x - maxRotError)
            return false;
        if (rotToCheck.y > endRot.y + maxRotError || rotToCheck.y < endRot.y - maxRotError)
            return false;
        if (rotToCheck.z > endRot.z + maxRotError || rotToCheck.z < endRot.z - maxRotError)
            return false;

        return true;
    }

    /*
     * Public functions section
     */

    public void StartDualCasting()
    {
        activeSpells.DualState = CastState.Ready;
    }

    /// <summary>
    /// Used by InputController to reset spellcasting state of RH when player releases casting button
    /// </summary>
    public void StopRHCasting()
    {
        activeSpells.RHState = CastState.Ready;
    }

    /// <summary>
    /// Used by InputController to reset spellcasting state of LH when player releases casting button
    /// </summary>
    public void StopLHCasting()
    {
        activeSpells.LHState = CastState.Ready;
    }
}
