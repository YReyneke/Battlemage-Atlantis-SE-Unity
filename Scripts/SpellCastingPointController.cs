using UnityEngine;
using SpellcastDataModule;

[AddComponentMenu("Player Scripts/Spellcasting Point Controller")]
public class SpellCastingPointController : MonoBehaviour
{
    private struct CastingNodes
    {
        public SpellID spell;
        public GameObject[] LNodes;
        public GameObject[] RNodes;

        public CastingNodes(SpellID spell, ref GameObject[] LNodes, ref GameObject[] RNodes)
        {
            this.spell = spell;
            this.LNodes = LNodes;
            this.RNodes = RNodes;
        }
    }

    /// <summary>
    /// Automatically assigned by playerScriptHandler
    /// </summary>
    [HideInInspector]
    public PlayerScriptHandler playerScriptHandler;

    [SerializeField, Tooltip("Element 0: Grenade Spell Node Prefab\n" +
                             "Element 1: Laser Spell Node Prefab\n" +
                             "Element 2: Missile Spell Node Prefab\n" +
                             "Element 3: Lightning Spell Node Prefab\n" +
                             "Element 4: Artillery Spell Node Prefab")] // Ensure these values align with SpellcastDataModule.SpellID enum
    private GameObject[] CastingNodePrefabs = new GameObject[5];

    private SpellData[] spellList;
    private CastingNodes[] spellNodeLists;

    // Start is called before the first frame update
    void Start()
    {
        spellList = playerScriptHandler.spellCaster.AllSpells;
        spellNodeLists = new CastingNodes[spellList.Length];
        InitialiseCastingPoints();
    }

    private void Update()
    {
        Vector3 LHPos = playerScriptHandler.inputController.LHController.transform.position;
        Vector3 RHPos = playerScriptHandler.inputController.RHController.transform.position;
        transform.position = LHPos + ((RHPos - LHPos) * 0.5F);
    }

    private void InitialiseCastingPoints()
    {
        for(int i = 0; i < spellList.Length; i++)
        {
            // Generate spellcasting points
            GameObject[] LNodes = new GameObject[spellList[i].Points.Length];
            GameObject[] RNodes = new GameObject[spellList[i].Points.Length];
            for (int j=0; j<spellList[i].Points.Length; j++)
            {
                // Initialise casting point object and parent to spellcasting grid
                LNodes[j] = Instantiate(CastingNodePrefabs[((int)spellList[i].ID)], playerScriptHandler.inputController.LHStartTF);
                RNodes[j] = Instantiate(CastingNodePrefabs[((int)spellList[i].ID)], playerScriptHandler.inputController.RHStartTF);
                // Deactivate until spellcasting starts
                LNodes[j].SetActive(false);
                RNodes[j].SetActive(false);
            }

            // Add to list
            spellNodeLists[i].spell = spellList[i].ID;
            spellNodeLists[i].LNodes = LNodes;
            spellNodeLists[i].RNodes = RNodes;
        }
    }

    public void UpdateSpellcastingPoints()
    {
        foreach(var spell in spellList) // All the spells in the game
        {
            foreach (var list in spellNodeLists) // All casting point lists in the game
            {
                if (spell.ID == list.spell) // If spell id is the same for each
                {
                    if(spell.Points.Length != list.LNodes.Length || spell.Points.Length != list.RNodes.Length) // Check for incorrect math
                        Debug.LogError("Node List does not contain same mount of nodes as the current spell!");

                    if (spell.isLHValid && playerScriptHandler.spellCaster.ActiveSpells.LHState == CastState.Casting) // If the current spell is available for LH
                    {
                        // Enable & update LH casting nodes for this spell
                        for (int i = 0; i < list.LNodes.Length; i++) {
                            list.LNodes[i].SetActive(true);
                            Vector3 pointPos;
                            Vector3 pointRot;

                            pointPos = HelperFunctions.InvertedPos(spell.Points[i].PointPosition) * spell.lhScalar; // Casting grid coordinate
                            pointRot = HelperFunctions.InvertedRot(spell.Points[i].PointRotation); // Casting grid coordinate

                            list.LNodes[i].transform.localPosition = pointPos;
                            list.LNodes[i].transform.localEulerAngles = pointRot;
                        }
                    } else
                    {
                        // Disable/hide LCasting nodes
                        for (int i = 0; i < list.LNodes.Length; i++)
                        {
                            list.LNodes[i].SetActive(false);
                        }
                    }

                    if (spell.isRHValid && playerScriptHandler.spellCaster.ActiveSpells.RHState == CastState.Casting) // If the current spell is available for RH
                    {
                        // Enable & update RH casting nodes for this spell
                        for (int i = 0; i < list.RNodes.Length; i++)
                        {
                            list.RNodes[i].SetActive(true);

                            Vector3 pointPos = spell.Points[i].PointPosition * spell.rhScalar; // Casting grid coordinate
                            Vector3 pointRot = spell.Points[i].PointRotation; // Casting grid coordinate

                            list.RNodes[i].transform.localPosition = pointPos;
                            list.RNodes[i].transform.localEulerAngles = pointRot;
                        }
                    }
                    else
                    {
                        // Disable/hide LCasting nodes
                        for (int i = 0; i < list.RNodes.Length; i++)
                        {
                            list.RNodes[i].SetActive(false);
                        }
                    }
                }
            }
        }
    }
}