
using UdonSharp;
using UnityEngine.UI;

public class ConfigUI : UdonSharpBehaviour
{
    public Keyboard keyboard;
    public Slider sensitivity;
    public Toggle triggerToInput;

    void Start()
    {
        keyboard.ActiveMinSqrt = sensitivity.value * sensitivity.value;
        keyboard.IgnoreMaxSqrt = (sensitivity.value + 0.05f) * (sensitivity.value + 0.05f);
        keyboard.TriggerToInput = triggerToInput.isOn;
    }
}
