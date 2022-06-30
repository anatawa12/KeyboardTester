using TMPro;
using UdonSharp;

public class RingCharsController : UdonSharpBehaviour
{
    private TextMeshProUGUI[] meshs;

    void Start()
    {
        meshs = new TextMeshProUGUI[8];
        for (var i = 0; i < 8; i++)
            meshs[i] = transform.GetChild(8).gameObject.GetComponent<TextMeshProUGUI>();
    }

    public void SetChars(char char0, char char1, char char2, char char3, char char4, char char5, char char6, char char7)
    {
        gameObject.SetActive(true);
        meshs[0].text = char0.ToString();
        meshs[1].text = char1.ToString();
        meshs[2].text = char2.ToString();
        meshs[3].text = char3.ToString();
        meshs[4].text = char4.ToString();
        meshs[5].text = char5.ToString();
        meshs[6].text = char6.ToString();
        meshs[7].text = char7.ToString();
    }

    public void Disable()
    {
        gameObject.SetActive(false);
    }
}
