
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Selecter : UdonSharpBehaviour
{
    public GameObject enable;
    public GameObject[] disables;

    public override void Interact()
    {
        enable.SetActive(true);
        foreach (var disable in disables)
            disable.SetActive(false);
    }
}
