using System;
using TMPro;
using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class Keyboard : UdonSharpBehaviour
{
    public TextMeshPro logText;
    public TextMeshPro mainText;
    public RectTransform rowCursor;
    public RectTransform colCursor;
    public GameObject tableRoot;

    // \0 is used for the slot not defined
    // \u0001~\u001F can be used for locale specific
    private char[][][] _keyboardTables;
    private TextMeshProUGUI[][] _charTMPs;

    [NonSerialized] public char[][] ActiveTable;
    [NonSerialized] public int LeftAngle = -1;
    [NonSerialized] public int RightAngle = -1;

    private const float ActiveMinSqrt = 0.75f * 0.75f;
    private const float IgnoreMaxSqrt = 0.80f * 0.80f;
    // tan(90/4*1 = 22.5[deg])
    private const float Tan1QuoterRightAngle = 0.41421356237f;
    // tan(90/4*3 = 67.5[deg]) = 1/tan(90/4*1 = 22.5[deg])
    private const float Tan3QuoterRightAngle = 22.5881805325f;

    private int _activeTable = 1;
    private int _activeTableOld = 0;
    private string _log;

    private bool _leftPressing = false;
    private bool _rightPressing = false;

    private void Start()
    {
        mainText.text = "";
        _keyboardTables = MakeTables(str:
            // table 0 is reserved for signs
            "(" + "[" + "{" + "<" + "\\" + ";" + "-" + "=" +
            ")" + "]" + "}" + ">" + "/" + ":" + "+" + "_" +
            "“" + "." + "?" + "1" + "2" + "3" + "4" + "5" +
            "‘" + "," + "!" + "6" + "7" + "8" + "9" + "0" +
            "&" + "*" + "¥" + "^" + "%" + "\0" + "\0" + "\0" +
            "~" + "`" + "@" + "$" + "\0" + "\0" + "\0" + "\0" +
            "\0" + "\0" + "\0" + "\0" + "\0" + "\0" + "\0" + "\0" +
            "\0" + "\0" + "\0" + "\0" + "\0" + "\0" + "\0" + "\0" +

            // table 1: Japanese.
            // \u0001: small hiragana like ぁ
            // \u0002: Dakuten
            // \u0003: Handakuten
            // \u0004: conversion request or show next candidate
            // \u0005: conversion select
            // TODO: consider merge small hiragana & (Han)? Dakuten as 12-key smartphone keyboard does
            //   and add support for separated conversion
            //   separated conversion:
            //     for きょうはいいてんきですね, separate the string to (きょうは)(いい)(てんき)(ですね)
            //     and convert per each selection
            "あ" + "い" + "う" + "え" + "お" + "や" + "ゆ" + "よ" +
            "か" + "き" + "く" + "け" + "こ" + "わ" + "を" + "ん" +
            "さ" + "し" + "す" + "せ" + "そ" + "「" + "。" + "?" +
            "た" + "ち" + "つ" + "て" + "と" + "」" + "、" + "!" +
            "な" + "に" + "ぬ" + "ね" + "の" + "〜" + "\u0002" + "\0" +
            "は" + "ひ" + "ふ" + "へ" + "ほ" + "ー" + "\u0003" + "\u0001" +
            "ま" + "み" + "む" + "め" + "も" + "\u0004" + "\0" + "\0" +
            "ら" + "り" + "る" + "れ" + "ろ" + "\u0005" + "\0" + "\0" +

            // table 2: English
            "a" + "b" + "c" + "d" + "e" + "f" + "g" + "h" +
            "A" + "B" + "C" + "D" + "E" + "F" + "G" + "H" +
            "i" + "j" + "k" + "l" + "m" + "n" + "o" + "p" +
            "I" + "J" + "K" + "L" + "M" + "N" + "O" + "P" +
            "q" + "r" + "s" + "t" + "u" + "v" + "w" + "x" +
            "Q" + "R" + "S" + "T" + "U" + "V" + "W" + "X" +
            "y" + "z" + "\"" + "." + "?" + "/" + "\0" + "\0" +
            "Y" + "Z" + "\'" + "," + "!" + "-" + "\0" + "\0" +
            "");

        _charTMPs = new TextMeshProUGUI[8][];
        for (var i = 0; i < 8; i++)
        {
            _charTMPs[i] = new TextMeshProUGUI[8];
            for (var j = 0; j < 8; j++)
                _charTMPs[i][j] = (TextMeshProUGUI)tableRoot.transform.GetChild(i * 8 + j).gameObject
                    .GetComponent(typeof(TextMeshProUGUI));
        }

        TableChanged(_activeTable);
    }

    private void Log(string log)
    {
        _log = $"{log}\n{_log}";
    }

    private void Update()
    {
        var leftInput = new Vector2(Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal"),
            Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical"));
        var rightInput = new Vector2(Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal"),
            Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical"));
        var pressingBoth = _leftPressing && _rightPressing;
        UpdateHand(leftInput, ref _leftPressing);
        UpdateHand(rightInput, ref _rightPressing);
        if (pressingBoth && (!_leftPressing || !_rightPressing))
        {
            InputChar(LeftAngle, RightAngle);
        }

        LeftAngle = _leftPressing ? StickAngle(leftInput) : -1;
        RightAngle = _rightPressing ? StickAngle(rightInput) : -1;
        logText.text =
            $"left: {leftInput.ToString("F4")}({leftInput.magnitude:F4})\n" +
            $"left angle: {LeftAngle} {(_leftPressing ? "pressing" : "free")}\n" +
            $"right: {rightInput.ToString("F4")}({rightInput.magnitude:F4})\n" +
            $"right angle: {RightAngle} {(_rightPressing ? "pressing" : "free")}\n" +
            $"table: {_activeTable}\n" +
            _log;

        if (LeftAngle == -1)
        {
            rowCursor.gameObject.SetActive(false);
        }
        else
        {
            rowCursor.gameObject.SetActive(true);
            rowCursor.anchoredPosition = new Vector2(0, 3.5f - LeftAngle);
        }

        if (RightAngle == -1)
        {
            colCursor.gameObject.SetActive(false);
        }
        else
        {
            colCursor.gameObject.SetActive(true);
            colCursor.anchoredPosition = new Vector2(-3.5f + RightAngle, 0);
        }
    }

    private void InputChar(int leftAngle, int rightAngle)
    {
        Log($"input char with {leftAngle} {rightAngle}");
        if (leftAngle < 0) return;
        if (rightAngle < 0) return;
        // 6,6~7,7 is reserved
        switch (leftAngle)
        {
            case 6:
                switch (rightAngle)
                {
                    case 6:
                        if (mainText.text.Length != 0)
                            mainText.text = mainText.text.Substring(0, mainText.text.Length - 1);
                        return;
                    case 7:
                        mainText.text += ' ';
                        return;
                }

                break;
            case 7:
                switch (rightAngle)
                {
                    case 6:
                        if (_activeTableOld == 0)
                        {
                            _activeTableOld = _activeTable;
                            _activeTable = 0;
                        }
                        else
                        {
                            _activeTable = _activeTableOld;
                            _activeTableOld = 0;
                        }

                        TableChanged(_activeTable);
                        return;
                    case 7:
                        if (_activeTableOld != 0)
                        {
                            _activeTable = _activeTableOld;
                            _activeTableOld = 0;
                        }

                        _activeTable++;
                        if (_activeTable == _keyboardTables.Length)
                            _activeTable = 1;

                        TableChanged(_activeTable);
                        return;
                }

                break;
        }

        var c = ActiveTable[leftAngle][rightAngle];
        if (c == 0)
            return;
        if (c < ' ')
        {
            // TODO: implement extra operations
            Log($"unimplemented operator char: {(int)c:x2}");
            return;
        }

        mainText.text += c;
    }

    private void TableChanged(int activeTable)
    {
        ActiveTable = _keyboardTables[activeTable];
        for (var i = 0; i < 8; i++)
        for (var j = 0; j < 8; j++)
            _charTMPs[i][j].text = ActiveTable[i][j].ToString();
        // Erase To the Left: ⌫ is not available in VRC.
        _charTMPs[6][6].text = "\u2190"; // Leftwards Arrow: ←
        _charTMPs[6][7].text = "\u2423"; // Open Box: &blank;: ␣
        _charTMPs[7][6].text = "#!";
        //_charTMPs[7][7].text = "";
    }

    private char[][][] MakeTables(string str)
    {
        if (str.Length % 64 != 0)
        {
            Debug.Log($"invalid table size: {str.Length}");

            // die
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            // ReSharper disable once PossibleNullReferenceException
            ((string)null).Trim();
        }

        char[] chars = str.ToCharArray();
        var tableCnt = str.Length / (8 * 8);
        var tables = new char[tableCnt][][];

        for (var i = 0; i < tableCnt; i++)
        {
            var table = tables[i] = new char[8][];
            for (var j = 0; j < 8; j++)
            {
                var row = table[j] = new char[8];

                for (var k = 0; k < 8; k++)
                {
                    Debug.Log($"access: {i * 64 + j * 8 + k}");
                    row[k] = chars[i * 64 + j * 8 + k];
                }
            }
        }

        return tables;
    }

    private void PressChanged(bool left)
    {
        Log($"press changed: {(left ? "left" : "right")}");
        // impossible due to C# version
        //switch ((left, _leftPressing, _rightPressing))
        //{
        //    case (false, false, false):
        //        break;
        //}
    }

    private bool UpdateHand(Vector2 location, ref bool pressing)
    {
        switch (pressing)
        {
            case true:
                if (location.sqrMagnitude < ActiveMinSqrt)
                {
                    pressing = false;
                    return true;
                }

                break;
            case false:
                if (IgnoreMaxSqrt < location.sqrMagnitude)
                {
                    pressing = true;
                    return true;
                }

                break;
        }

        return false;
    }

    /*    \       /
     * _7  \  0  /  1_
     *  \_  \   /  _/
     *  6_|-------|_2
     * _/   /   \   \_
     *  5  /  4  \  3
     *    /       \
     */
    private int StickAngle(Vector2 stick)
    {
        if (stick.x == 0 && stick.y == 0)
            return -1;
        var xAbs = Mathf.Abs(stick.x);
        var yAbs = Mathf.Abs(stick.y);
        var absRadio = xAbs / yAbs;
        if (absRadio < Tan1QuoterRightAngle)
        {
            return stick.y > 0 ? 0 : 4;
        }
        else if (absRadio < Tan3QuoterRightAngle)
        {
            if (stick.x > 0)
                return stick.y > 0 ? 1 : 3;
            else
                return stick.y > 0 ? 7 : 5;
        }
        else
        {
            return stick.x > 0 ? 2 : 6;
        }
    }
}