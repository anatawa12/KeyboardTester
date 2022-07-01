using System.Globalization;
using UdonSharp;
using UnityEngine.UI;

public class SliderValueDisplay : UdonSharpBehaviour
{
    public Text text;
    public Slider slider;

    private void Update()
    {
        text.text = slider.value.ToString("F2", CultureInfo.InvariantCulture);
    }
}
