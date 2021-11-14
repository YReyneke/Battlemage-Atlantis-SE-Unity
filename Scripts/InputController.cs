using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// This class handles the calling of relevant functions throughout the project as required by any user inputs, using the new input system
/// <br>As well as converting of user motion data into the game's various coordinate spaces</br>
/// <para>NOTE: Spellcasting reference grids pitch/roll are set to 0
/// <br>This allows for accurate spell-casting-point representation in world</br></para>
/// </summary>
[AddComponentMenu("Player Scripts/Input Controller")]
public class InputController : MonoBehaviour, PlayerInput.IVRInputActions
{
    // Inspector Properties
    [HideInInspector]
    public PlayerScriptHandler playerScriptHandler;  // Automatically assigned by playerScriptHandler

    [SerializeField, Tooltip("In Game Model for Right Hand Controller")]
    private GameObject RHMotionController;

    [SerializeField, Tooltip("In Game Model for Left Hand Controller")]
    private GameObject LHMotionController;

    [SerializeField, Tooltip("In Game Reference for HMD")]
    private GameObject HMDCamera;

    [SerializeField, Tooltip("Max time to allow overriding of single casting to dual casting when second button is pressed")]
    private float DualCastingDelay = 0.3333F;
    private float DelayCounter = 0;

    // *** Local Variables ***
    private PlayerInput InputComponent;

    // These variables are requested by SpellCaster to ensure correct behaviour of the spellcasting process
    private bool isLHCasting = false;
    private bool isRHCasting = false;
    private bool isDualCasting = false;

    // These grids represent the origin of the current (if any) spellcast process in world space
    private GameObject LCastingGrid;
    private GameObject RCastingGrid;

    // These contain the current velocity of each hand
    Vector3 lhVelocity;
    Vector3 rhVelocity;

    // *** Accessible Variables *** \\

    // *** LEMController interface variables
    public ref GameObject LHController { get { return ref LHMotionController; } }
    public ref GameObject RHController { get { return ref RHMotionController; } }
    public Vector3 LHVelocity { get { return lhVelocity; } }
    public Vector3 RHVelocity { get { return rhVelocity; } }

    // *** Spellcaster interface variables *** \\
    public Transform LHStartTF { get { return LCastingGrid.transform; } }
    public Transform RHStartTF { get { return RCastingGrid.transform; } }
    /// <summary>
    /// Start position in world space of LH spellcasting
    /// </summary>
    public Vector3 LHStartPos { get { return LCastingGrid.transform.position; } }
    /// <summary>
    /// Start rotation in world space of LH spellcasting
    /// </summary>
    public Vector3 LHStartRot { get { return LCastingGrid.transform.eulerAngles; } }
    /// <summary>
    /// Start position in world space of RH spellcasting
    /// </summary>
    public Vector3 RHStartPos { get { return RCastingGrid.transform.position; } }
    /// <summary>
    /// Start rotation in world space of RH spellcasting
    /// </summary>
    public Vector3 RHStartRot { get { return RCastingGrid.transform.eulerAngles; } }
    // Readonly vars for hand controller changes in position/rotation since start of spellcast
    /// <summary>
    /// Difference between current LH position and LH starting position of cast in casting grid space
    /// </summary>
    public Vector3 LHDeltaPos { get { return LCastingGrid.transform.InverseTransformPoint(LHMotionController.transform.position); } }
    /// <summary>
    /// Difference between current LH rotation and starting LH rotation of cast
    /// </summary>
    public Vector3 LHDeltaRot { get { return RectifyRotation(LHMotionController.transform.eulerAngles - LCastingGrid.transform.eulerAngles); } }
    /// <summary>
    /// Difference between current RH position and RH starting position of cast in casting grid space
    /// </summary>
    public Vector3 RHDeltaPos { get { return RCastingGrid.transform.InverseTransformPoint(RHMotionController.transform.position); } }
    /// <summary>
    /// Difference between current RH rotation and starting RH rotation of cast
    /// </summary>
    public Vector3 RHDeltaRot { get { return RectifyRotation(RHMotionController.transform.eulerAngles - RCastingGrid.transform.eulerAngles); } }
    public bool IsLHCasting { get { return isLHCasting; } }
    public bool IsRHCasting { get { return isRHCasting; } }
    public bool IsDualCasting { get { return isDualCasting; } }

    public void OnEnable()
    {
        if (InputComponent == null)
        {
            InputComponent = new PlayerInput();
            InputComponent.VRInput.SetCallbacks(this);
        }
        InputComponent.VRInput.Enable();
    }

    public void OnDisable()
    {
        InputComponent.VRInput.Disable();
    }

    // Start is called before the first frame update
    void Start()
    {
        LCastingGrid = Instantiate(new GameObject("LCastingTF"), this.transform);
        RCastingGrid = Instantiate(new GameObject("RCastingTF"), this.transform);
    }

    // Update is called once per frame
    void Update()
    {
        // Update dual casting status
        if (!isDualCasting)
        {
            if (DelayCounter < DualCastingDelay) {
                if (isLHCasting && isRHCasting)
                {
                    Debug.Log("Dualcasting initialised");
                    playerScriptHandler.spellCaster.StartDualCasting();
                    isDualCasting = true;
                }
                else if (isLHCasting != isRHCasting)
                {
                    DelayCounter += Time.deltaTime;
                }
            }
        }
        if (!isLHCasting && !isRHCasting) // reset
        {
            isDualCasting = false;
            DelayCounter = 0.0F;
        }
    }

    private Vector3 RectifyRotation(Vector3 rot)
    {
        rot.x = (rot.x > 180) ? rot.x - 360 : rot.x;
        rot.x = (rot.x < -180) ? rot.x + 360 : rot.x;
        rot.y = (rot.y > 180) ? rot.y - 360 : rot.y;
        rot.y = (rot.y < -180) ? rot.y + 360 : rot.y;
        rot.z = (rot.z > 180) ? rot.z - 360 : rot.z;
        rot.z = (rot.z < -180) ? rot.z + 360 : rot.z;

        return rot;
    }

    /*
     * Input Asset Contexts 
     * This is where input is read from the controllers
     */
    public void OnCastLH(InputAction.CallbackContext context)
    {
        //Debug.Log("Cast: " + context.ReadValueAsButton());
        isLHCasting = context.ReadValueAsButton();

        // When button is initially pressed
        // Reset reference point used to measure player hand transformation during spellcasting
        if (isLHCasting == true)
        {
            LCastingGrid.transform.position = LHMotionController.transform.position;
            LCastingGrid.transform.eulerAngles = new Vector3(0, LHMotionController.transform.eulerAngles.y, 0);
        } else // Player releases casting button
        {
            isDualCasting = false;
            playerScriptHandler.spellCaster.StopLHCasting();
        }
    }

    public void OnCastRH(InputAction.CallbackContext context)
    {
        //Debug.Log("Cast: " + context.ReadValueAsButton());
        isRHCasting = context.ReadValueAsButton();

        // When button is initially pressed
        // Reset reference point used to measure player hand transformation during spellcasting
        if (isRHCasting == true)
        {
            RCastingGrid.transform.position = RHMotionController.transform.position;
            RCastingGrid.transform.eulerAngles = new Vector3(0, RHMotionController.transform.eulerAngles.y, 0);
        }
        else // Player releases casting button
        {
            isDualCasting = false;
            playerScriptHandler.spellCaster.StopRHCasting();
        }
    }

    public void OnRHTriggerPressed(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton())
        {
            playerScriptHandler.LEMController.ActivateRHSpell();
        }
        else
        {
            playerScriptHandler.LEMController.ReleaseRH();
        }
    }

    public void OnLHTriggerPressed(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton())
        {
            playerScriptHandler.LEMController.ActivateLHSpell();
        }
        else
        {
            playerScriptHandler.LEMController.ReleaseLH();
        }
    }

    public void OnUpdateRHVelocity(InputAction.CallbackContext context)
    {
        rhVelocity = context.ReadValue<Vector3>();
    }

    public void OnUpdateLHVelocity(InputAction.CallbackContext context)
    {
        lhVelocity = context.ReadValue<Vector3>();
    }
}
