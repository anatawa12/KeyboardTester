
using UdonSharp;
using UnityEngine.UI;

public class ConfigUI : UdonSharpBehaviour
{
    public Keyboard keyboard;
    public Slider sensitivity;
    public Toggle triggerToInput;

    void Update()
    {
        var sensitivity = 1f - this.sensitivity.value;
        keyboard.ActiveMinSqrt = sensitivity * sensitivity;
        keyboard.IgnoreMaxSqrt = (sensitivity + 0.05f) * (sensitivity + 0.05f);
        keyboard.TriggerToInput = triggerToInput.isOn;
    }
}
