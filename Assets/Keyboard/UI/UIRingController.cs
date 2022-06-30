
using System;
using UdonSharp;
using UnityEngine;

public class UIRingController : KeyboardDisplay
{
    public RingCharsController singleChars;
    public RingCharsController[] vectorSelections;
    public LeftOrRight hand;
    public RectTransform stick;

    [NonSerialized] public int Index = -2;
    private char[] _table;
    private string[] _seventhLine;
    private int _mainRadius;
    private int _subRadius;

    private void Start()
    {
        switch (hand)
        {
            case LeftOrRight.Left:
                _mainRadius = 8;
                _subRadius = 1;
                break;
            case LeftOrRight.Right:
                _mainRadius = 1;
                _subRadius = 8;
                break;
            default:
                ((object)null).ToString();
                break;
        }
    }

    private void Update()
    {
        stick.anchoredPosition = Keyboard.GetStickPos(hand) * 25 + new Vector2(25, 25);
    }

    public override void OnInput(int left, int right)
    {
        int main = -1;
        int sub = -1;
        switch (hand)
        {
            case LeftOrRight.Left:
                main = left;
                sub = right;
                break;
            case LeftOrRight.Right:
                main = right;
                sub = left;
                break;
            default:
                ((object)null).ToString();
                break;
        }

        if (sub == -1)
        {
            singleChars.Disable();
            for (var mainI = 0; mainI < 8; mainI++)
                vectorSelections[mainI].SetChars(
                    _table[mainI * _mainRadius + 0 * _subRadius],
                    _table[mainI * _mainRadius + 1 * _subRadius],
                    _table[mainI * _mainRadius + 2 * _subRadius],
                    _table[mainI * _mainRadius + 3 * _subRadius],
                    _table[mainI * _mainRadius + 4 * _subRadius],
                    _table[mainI * _mainRadius + 5 * _subRadius],
                    _table[mainI * _mainRadius + 6 * _subRadius],
                    _table[mainI * _mainRadius + 7 * _subRadius]);
        }
        else
        {
            foreach (var selection in vectorSelections)
                selection.Disable();

            singleChars.SetChars(
                _table[0 * _mainRadius + sub * _subRadius],
                _table[1 * _mainRadius + sub * _subRadius],
                _table[2 * _mainRadius + sub * _subRadius],
                _table[3 * _mainRadius + sub * _subRadius],
                _table[4 * _mainRadius + sub * _subRadius],
                _table[5 * _mainRadius + sub * _subRadius],
                _table[6 * _mainRadius + sub * _subRadius],
                _table[7 * _mainRadius + sub * _subRadius]);
        }
    }

    public override void OnTableChanged(char[] newTable)
    {
        _table = newTable;
    }
}
