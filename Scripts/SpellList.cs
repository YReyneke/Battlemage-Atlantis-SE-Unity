using UnityEngine;
using SpellcastDataModule;

/// <summary>
/// This class contains all the game's spells and relevant runtime data for each one
/// <para>NOTE: Spell data is set up to be compatible with RH - inversion will be required for all LH calculations</para>
/// </summary>
[AddComponentMenu("Player Scripts/Spell List")]
public class SpellList : ScriptableObject
{
    private static SpellData GrenadeSpell = new SpellData(SpellID.Grenade, 3, new SpellPoint[] { 
        new SpellPoint(0, new Vector3(0,0,0), new Vector3(0,0,0), new CheckAxes(true,true,true), new CheckAxes(true,true,true)),
        new SpellPoint(1, new Vector3(-1,-1,0), new Vector3(0,0,0), new CheckAxes(true,true,true), new CheckAxes(true,true,true)),
        new SpellPoint(2, new Vector3(0,-2,0), new Vector3(0,0,0), new CheckAxes(true,true,true), new CheckAxes(true,true,true)),
        new SpellPoint(3, new Vector3(1,-1,0), new Vector3(0,0,0), new CheckAxes(true,true,true), new CheckAxes(true,true,true)),
        new SpellPoint(4, new Vector3(0,0,0), new Vector3(0,0,0), new CheckAxes(true,true,true), new CheckAxes(true,true,true))
    });

    private static SpellData LaserSpell = new SpellData(SpellID.Laser, 5, new SpellPoint[] {
        new SpellPoint(0, new Vector3(0,0,0), new Vector3(0,0,90), new CheckAxes(true,true,true), new CheckAxes(true,true,true)),
        new SpellPoint(1, new Vector3(1,0,0), new Vector3(0,0,90), new CheckAxes(true,true,true), new CheckAxes(true,true,true))
    });

    private static SpellData MissileSpell = new SpellData(SpellID.Missile, 3, new SpellPoint[] {
        new SpellPoint(0, new Vector3(0,0,0), new Vector3(0,0,0), new CheckAxes(true,true,true), new CheckAxes(true,true,true)),
        new SpellPoint(1, new Vector3(0,0,1), new Vector3(0,0,90), new CheckAxes(true,true,true), new CheckAxes(true,true,true))
    });

    private static SpellData LightningSpell = new SpellData(SpellID.Lightning, 2, new SpellPoint[] {
        new SpellPoint(0, new Vector3(0,0,0), new Vector3(0,0,0), new CheckAxes(true,true,true), new CheckAxes(true,true,true)),
        new SpellPoint(1, new Vector3(-1,1,0), new Vector3(0,0,0), new CheckAxes(true,true,true), new CheckAxes(true,true,true)),
        new SpellPoint(2, new Vector3(-0.5F,1.5F,0), new Vector3(0,0,0), new CheckAxes(true,true,true), new CheckAxes(true,true,true)),
        new SpellPoint(3, new Vector3(-1,2,0), new Vector3(0,0,0), new CheckAxes(true,true,true), new CheckAxes(true,true,true))
    });

    private static SpellData ArtillerySpell => new SpellData(SpellID.Artillery, 1, new SpellPoint[] {
        new SpellPoint(0, new Vector3(0,0,0), new Vector3(0,0,0), new CheckAxes(true,true,true), new CheckAxes(true,true,true)),
        new SpellPoint(1, new Vector3(1,0,0), new Vector3(0,0,0), new CheckAxes(true,true,true), new CheckAxes(true,true,true)),
        new SpellPoint(2, new Vector3(1,0,0), new Vector3(0,0,90), new CheckAxes(true,true,true), new CheckAxes(true,true,true)),
        new SpellPoint(3, new Vector3(1,-1,0), new Vector3(0,0,90), new CheckAxes(true,true,true), new CheckAxes(true,true,true))
    }, true); // Is dual only

    private SpellData[] spellDataList = new SpellData[] { GrenadeSpell, LaserSpell, MissileSpell, LightningSpell, ArtillerySpell };

    /// <summary>
    /// Array containing a list of all the game's spells
    /// <br>Each with their own motion data, and current overall completion state</br>
    /// </summary>
    public ref SpellData[] AllSpells => ref spellDataList;

    /// <returns>String containing default position and rotation data for every point along a spells casting movement</returns>
    public string AsString()
    {
        string str = "";
        foreach (var spell in AllSpells)
        {
            foreach (var point in spell.Points)
            {
                str += $"Point {point.PointID}:\n     Pos: {point.PointPosition}\n     Rot: {point.PointRotation}\n";
            }
            str += "\n";
        }
        return str;
    }
}
