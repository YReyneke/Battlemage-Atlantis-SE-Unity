using UnityEngine;


// This module holds all the spellcasting related enums and structs
namespace SpellcastDataModule
{
    public enum SpellID
    {
        Grenade,
        Laser,
        Missile,
        Lightning,
        Artillery,
        None
    }

    public enum CastingType
    {
        LH,
        RH,
        Dual,
        Separate,
        None
    }

    public enum CastState
    {
        Ready,
        Failed,
        Complete,
        Casting
    }

    /// <summary>
    /// 3 bools, one for each of the 3D axes
    /// <br>Set desired axis to false to disable tolerance checking for that axis</br>
    /// </summary>
    public struct CheckAxes
    {
        public bool CheckX { get; }
        public bool CheckY { get; }
        public bool CheckZ { get; }

        /// <summary>
        /// There is a bool for each axis, set to false for any axis where tolerance check is not required.
        /// </summary>
        /// <param name="checkX">If x position/pitch rotation should be checked</param>
        /// <param name="checkY">If y position/yaw rotation should be checked</param>
        /// <param name="checkZ">If z position/roll rotation should be checked</param>
        public CheckAxes(bool checkX=true, bool checkY=true, bool checkZ=true)
        {
            CheckX = checkX;
            CheckY = checkY;
            CheckZ = checkZ;
        }
    }

    /// <summary>
    /// Holds transformation reference data in terms relative to the start transform of the cast
    /// i.e. this point is at positionxyz relative to start of spell
    /// <br>It also contains an isCompleted state bool for each hand</br>
    /// <para>NOTE: First point must always have position(0,0,0) and rotation(n,0,n)!</para>
    /// </summary>
    public struct SpellPoint
    {
        public ushort PointID { get; }
        public Vector3 PointPosition { get; }
        public Vector3 PointRotation { get; }
        public CheckAxes CheckPosAxes { get; }
        public CheckAxes CheckRotAxes { get; }
        public bool isLHComplete;
        public bool isRHComplete;

        /// <summary>
        /// Create next spellpoint
        /// <para>NOTE: First point must always have position(0,0,0) and rotation(n,0,n)! Where n is any number in degrees</para>
        /// </summary>
        /// <param name="pointID">The point's ID. Must start at 0 and increase by 1 for each point!</param>
        /// <param name="pointPosition">The realtive unit-vector position from start position of this point</param>
        /// <param name="pointRotation">The relative rotation since initial rotation of this point</param>
        /// <param name="checkPosAxes">There is a bool for each axis, set to false for any axes where tolerance check is not required.<br>Default: x=true, y=true, z=true</br></param>
        /// <param name="checkRotAxes">There is a bool for each axis, set to false for any axes where tolerance check is not required.<br>Default: x=true, y=true, z=true</br></param>
        public SpellPoint(ushort pointID, Vector3 pointPosition, Vector3 pointRotation, CheckAxes checkPosAxes, CheckAxes checkRotAxes)
        {
            this.PointID = pointID;
            this.PointPosition = pointPosition;
            this.PointRotation = pointRotation;
            this.CheckPosAxes = checkPosAxes;
            this.CheckRotAxes = checkRotAxes;
            isLHComplete = false;
            isRHComplete = false;
        }
    }

    /// <summary>
    /// Holds all the data for a given spell
    /// </summary>
    public struct SpellData
    {
        public SpellID ID { get; }
        public ushort DefaultCharges { get; }
        public SpellPoint[] Points { get; }
        public bool IsDualOnly { get; }
        /// <summary>
        /// Used to ensure that no extra checking is performed once spell was successfully cast (with RH)
        /// </summary>
        public bool isRHComplete;
        /// <summary>
        /// Used to ensure that no extra checking is performed once spell was successfully cast (with LH)
        /// </summary>
        public bool isLHComplete;
        public bool isRHValid;
        public bool isLHValid;
        /// <summary>
        /// Current scale for the LH cast of this spell
        /// <br>(This multiplies the SpellPoint.PointPosition dynamically depending on user preference - hopefully)</br>
        /// </summary>
        public float lhScalar;
        /// <summary>
        /// Current scale for the RH cast of this spell
        /// <br>(This multiplies the SpellPoint.PointPosition dynamically depending on user preference - hopefully)</br>
        /// </summary>
        public float rhScalar;

        /// <summary>
        /// Holds all the data for a given spell
        /// </summary>
        /// <param name="ID">The spell identifier</param>
        /// <param name="defaultCharges">Number of useable spell charges applied to LEM on correct casting of the spell</param>
        /// <param name="points">The points that make up the movement required to cast this spell</param>
        /// <param name="isDualOnly">Used when spell is only supposed to be dual cast</param>
        public SpellData(SpellID ID, ushort defaultCharges, SpellPoint[] points, bool isDualOnly = false)
        {
            this.ID = ID;
            this.DefaultCharges = defaultCharges;
            this.Points = points;
            this.IsDualOnly = isDualOnly;

            isLHComplete = false;
            isRHComplete = false;
            isRHValid = true;
            isLHValid = true;
            lhScalar = 1;
            rhScalar = 1;
        }
    }

    /// <summary>
    /// This is a reference for what the LEM should be displaying to the player and is updated by SpellCaster
    /// <br>It is also used to check current casting state of each hand within SpellCaster to prevent player performing infinite casting</br>
    /// </summary>
    public struct ActiveSpells
    {
        public SpellID LHSpell;
        public ushort LHCharges;
        public CastState LHState;

        public SpellID RHSpell;
        public ushort RHCharges;
        public CastState RHState;

        public SpellID DualSpell;
        public ushort DualCharges;
        public CastState DualState;

        /// <summary>
        /// Default constructor (cheeky)
        /// </summary>
        /// <param name="ID">This should never be touched if used right</param>
        public ActiveSpells(SpellID ID = SpellID.None)
        {
            LHSpell = ID;
            LHCharges = 0;
            LHState = CastState.Ready;

            RHSpell = ID;
            RHCharges = 0;
            RHState = CastState.Ready;

            DualSpell = ID;
            DualCharges = 0;
            DualState = CastState.Ready;
        }
    }

    public struct HelperFunctions
    {
        /// <summary>
        /// Inverts casting point position for use with opposite hand
        /// <br>(required as default orientation is RH)</br>
        /// </summary>
        /// <param name="PosToInvert">Casting point position to convert</param>
        /// <returns>Inverted Position</returns>
        public static Vector3 InvertedPos(Vector3 PosToInvert)
        {
            Vector3 retVal = PosToInvert;
            retVal.x *= -1;
            return retVal;
        }

        /// <summary>
        /// Inverts casting point rotation for use with opposite hand
        /// <br>(required as default orientation is RH)</br>
        /// </summary>
        /// <param name="PosToInvert">Casting point rotation to convert</param>
        /// <returns>Inverted Rotation</returns>
        public static Vector3 InvertedRot(Vector3 RotToInvert)
        {
            Vector3 retVal = RotToInvert;
            retVal.y *= -1;
            retVal.z *= -1;
            return retVal;
        }

        /// <summary>
        /// Used to find the next incomplete reference point of a spell's movement during RH casting
        /// </summary>
        /// <param name="spell">Reference to the spell</param>
        /// <returns>point ID of the next incomplete point (id will never return < 1)
        /// <br>-1 if all points are complete</br></returns>
        public static int FindNextRHPoint(ref SpellData spell)
        {
            for (int i = 0; i < spell.Points.Length; i++)
            {
                if (i == 0)
                {
                    spell.Points[i].isRHComplete = true;
                }
                else
                {
                    if (!spell.Points[i].isRHComplete)
                        return i;
                }
            }
            return -1; // All points completed
        }

        /// <summary>
        /// Used to find the next incomplete reference point of a spell's movement during LH casting
        /// </summary>
        /// <param name="spell">Reference to the spell</param>
        /// <returns>point ID of the next incomplete point (id will never return < 1)
        /// <br>-1 if all points are complete</br></returns>
        public static int FindNextLHPoint(ref SpellData spell)
        {
            for (int i = 0; i < spell.Points.Length; i++)
            {
                if (i == 0)
                {
                    spell.Points[i].isLHComplete = true;
                }
                else
                {
                    if (!spell.Points[i].isLHComplete)
                        return i;
                }
            }
            return -1; // All points completed
        }
    }
}