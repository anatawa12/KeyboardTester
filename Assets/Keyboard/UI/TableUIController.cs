
using TMPro;
using UdonSharp;
using UnityEngine;

public class TableUIController : KeyboardDisplay
{
    public GameObject tableRoot;
    public RectTransform rowCursor;
    public RectTransform colCursor;

    private TextMeshProUGUI[] _charTMPs;

    void Start()
    {
        _charTMPs = new TextMeshProUGUI[Keyboard.TableSize];
        for (var i = 0; i < Keyboard.TableSize; i++)
            _charTMPs[i] = (TextMeshProUGUI)tableRoot.transform.GetChild(i).gameObject
                .GetComponent(typeof(TextMeshProUGUI));
    }

    public override void OnInput(int left, int right)
    {
        if (left == -1)
        {
            rowCursor.gameObject.SetActive(false);
        }
        else
        {
            rowCursor.gameObject.SetActive(true);
            rowCursor.anchoredPosition = new Vector2(0, 3.5f - left);
        }

        if (right == -1)
        {
            colCursor.gameObject.SetActive(false);
        }
        else
        {
            colCursor.gameObject.SetActive(true);
            colCursor.anchoredPosition = new Vector2(-3.5f + right, 0);
        }
    }

    public override void OnTableChanged(char[] newTable)
    {
        for (var i = 0; i < Keyboard.TableSize; i++)
            _charTMPs[i].text = newTable[i].ToString();
    }
}
