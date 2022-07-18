
using UdonSharp;
using UnityEngine.UI;

public class ConfigUI : UdonSharpBehaviour
{
    public Keyboard keyboard;
    public UIRingController left;
    public UIRingController right;

    public Slider sensitivity;
    public Toggle flickInput;
    public Toggle keepRingViewLeft;
    public Toggle keepRingViewRight;

    void Update()
    {
        var sensitivity = 1f - this.sensitivity.value;
        keyboard.ActiveMinSqrt = sensitivity * sensitivity;
        keyboard.IgnoreMaxSqrt = (sensitivity + 0.05f) * (sensitivity + 0.05f);
        keyboard.FlickInput = flickInput.isOn;
        left.KeepRingView = keepRingViewLeft;
        right.KeepRingView = keepRingViewRight;
    }
}
