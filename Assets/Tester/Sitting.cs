
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Sitting : UdonSharpBehaviour
{
    private bool _seatedLocal = false;
    public VRCStation station;

    public override void Interact()
    {
        if (_seatedLocal)
            station.ExitStation(Networking.LocalPlayer);
        else
            station.UseStation(Networking.LocalPlayer);
    }

    public override void OnStationEntered(VRCPlayerApi player)
    {
        if (player == Networking.LocalPlayer)
            _seatedLocal = true;
    }

    public override void OnStationExited(VRCPlayerApi player)
    {
        if (player == Networking.LocalPlayer)
            _seatedLocal = false;
    }
}
