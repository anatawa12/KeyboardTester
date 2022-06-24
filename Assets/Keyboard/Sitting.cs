
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Sitting : UdonSharpBehaviour
{
    private bool seated = false;
    public override void Interact()
    {
        Networking.LocalPlayer.UseAttachedStation();
    }
}
