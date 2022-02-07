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
        bool TryCast(ISpellCaster caster, CastSettings settings, out NetworkObject locallySpawnedObject);

        /// <summary>
        /// Gets the preview function for the spell.
        /// See PreviewComponent on how to use.
        /// </summary>
        IPreview GetPreview(ISpellCaster caster, CastSettings castSettings);
    }

    public struct CastSettings
    {
        public Vector3 position;
        public Vector3 direction;
    }

    public interface ISpellCaster
    {
        NetworkObject NetworkObject { get; }
        
        /// <summary>Gets the target Transform for a given spell. </summary>
        /// <returns>True if a cast target exists.</returns>
        bool TryGetCastTarget(INetworkSpell spell, out Transform castTarget);
        
        /// <summary> Returns cast direction.</summary>
        Vector3 GetCastDirection(INetworkSpell spell);
    }
}