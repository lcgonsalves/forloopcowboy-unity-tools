using Unity.Netcode;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Spell
{
    public interface INetworkSpell
    {
        /// <summary>
        /// Casts spell.
        /// Define here the main logic of the spell.
        /// </summary>
        /// <returns>True if spell was cast.</returns>
        bool TryCast(ISpellCaster caster, out NetworkObject locallySpawnedObject);
    }

    public interface ISpellCaster
    {
        /// <summary> Returns the cast position for a given spell. </summary>
        Vector3 GetCastPosition(INetworkSpell spell);
        
        /// <summary> Returns the cast direction for a given spell. </summary>
        Vector3 GetCastDirection(INetworkSpell spell);

        /// <summary>Gets the target Transform for a given spell. </summary>
        /// <returns>True if a cast target exists.</returns>
        bool TryGetCastTarget(INetworkSpell spell, out Transform castTarget);
    }
}