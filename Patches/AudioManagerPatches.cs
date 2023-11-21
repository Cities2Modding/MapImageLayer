using Game;
using Game.Audio;
using HarmonyLib;
using MapImageLayer.Systems;

namespace MapImageLayer.Patches
{
    [HarmonyPatch( typeof( AudioManager ), "OnGameLoadingComplete" )]
    class AudioManager_OnGameLoadingCompletePatch
    {
        static void Postfix( AudioManager __instance, Colossal.Serialization.Entities.Purpose purpose, GameMode mode )
        {
            if ( !mode.IsGameOrEditor( ) )
                return;

            if ( mode.IsEditor( ) )
                return;

            __instance.World.GetOrCreateSystem<ImageOverlaySystem>( );
        }
    }
}
