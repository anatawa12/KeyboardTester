
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class WorldSettings : UdonSharpBehaviour
{
    void Start()
    {
        if (Networking.LocalPlayer.IsValid())
        {
            Networking.LocalPlayer.SetJumpImpulse();
            Networking.LocalPlayer.SetWalkSpeed();
            Networking.LocalPlayer.SetRunSpeed();
            Networking.LocalPlayer.SetStrafeSpeed();
        }
    }
}
