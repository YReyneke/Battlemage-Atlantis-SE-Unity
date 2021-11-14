using UnityEngine;

/// <summary>
/// Placeholder class for all scripts related to player - all script components come through here for any cross-reference requirements
/// </summary>
[AddComponentMenu("Player Scripts/Script Handler")]
public class PlayerScriptHandler : MonoBehaviour
{
    // Script placeholders
    [SerializeReference]
    public InputController inputController;

    [SerializeReference]
    public SpellCaster spellCaster;

    [SerializeReference]
    public LEMController LEMController;

    [Tooltip("The Spell casting point controller object")]
    public SpellCastingPointController spellCastingPointController;

    // Start is called before the first frame update
    void Start()
    {
        inputController.playerScriptHandler = this;
        spellCaster.playerScriptHandler = this;
        spellCastingPointController.playerScriptHandler = this;
        LEMController.playerScriptHandler = this;
    }
}
