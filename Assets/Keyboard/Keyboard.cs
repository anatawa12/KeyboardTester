using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class Keyboard : UdonSharpBehaviour
{
    public TextMeshPro logText;
    public TextMeshPro mainText;
    public KeyboardDisplay[] displays; 

    // \0 is used for the slot not defined
    // \u0001~\u001F can be used for locale specific
    private char[][] _keyboardTables;

    [NonSerialized] public char[] ActiveTable;
    [NonSerialized] public int LeftAngle = -2;
    [NonSerialized] public int RightAngle = -2;

    public const int TableSize = 8 * 8;

    public const char OpDeleteChar = '\uE000';
    public const char OpBlank      = '\uE001';
    public const char OpSignPlane  = '\uE002';
    public const char OpNextPlane  = '\uE003';

    // Japanese specifics
    public const char OpJpSmall             = '\uE010'; // small hiragana like ぁ
    public const char OpJpDakuten           = '\uE011'; // Dakuten
    public const char OpJpHandakuten        = '\uE012'; // Handakuten
    public const char OpJpNextCandidate     = '\uE013'; // conversion request or show next candidate
    public const char OpJpSelectCandidate   = '\uE014'; // conversion select
    // TODO: consider merge small hiragana & (Han)? Dakuten as 12-key smartphone keyboard does
    //   and add support for separated conversion
    //   separated conversion:
    //     for きょうはいいてんきですね, separate the string to (きょうは)(いい)(てんき)(ですね)
    //     and convert per each selection

    [NonSerialized] public float ActiveMinSqrt = 0.75f * 0.75f;
    [NonSerialized] public float IgnoreMaxSqrt = 0.80f * 0.80f;
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
        MakeTables(str:
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
            "あ" + "い" + "う" + "え" + "お" + "や" + "ゆ" + "よ" +
            "か" + "き" + "く" + "け" + "こ" + "わ" + "を" + "ん" +
            "さ" + "し" + "す" + "せ" + "そ" + "「" + "。" + "?" +
            "た" + "ち" + "つ" + "て" + "と" + "」" + "、" + "!" +
            "な" + "に" + "ぬ" + "ね" + "の" + "〜" + OpJpDakuten + "\0" +
            "は" + "ひ" + "ふ" + "へ" + "ほ" + "ー" + OpJpHandakuten + OpJpSmall +
            "ま" + "み" + "む" + "め" + "も" + OpJpNextCandidate + "\0" + "\0" +
            "ら" + "り" + "る" + "れ" + "ろ" + OpJpSelectCandidate + "\0" + "\0" +

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

        TableChanged(_activeTable);
    }

    private void Log(string log)
    {
        _log = $"{log}\n{_log}";
    }

    private void Update()
    {
        var leftInput = GetStickPos(LeftOrRight.Left);
        var rightInput = GetStickPos(LeftOrRight.Right);
        var pressingBoth = _leftPressing && _rightPressing;
        UpdateHand(leftInput, ref _leftPressing);
        UpdateHand(rightInput, ref _rightPressing);
        if (pressingBoth && (!_leftPressing || !_rightPressing))
        {
            InputChar(LeftAngle, RightAngle);
        }

        var anglesLeftOld = LeftAngle;
        var anglesRightOld = RightAngle;
        LeftAngle = _leftPressing ? StickAngle(leftInput) : -1;
        RightAngle = _rightPressing ? StickAngle(rightInput) : -1;
        logText.text =
            $"left: {leftInput.ToString("F4")}({leftInput.magnitude:F4})\n" +
            $"left angle: {LeftAngle} {(_leftPressing ? "pressing" : "free")}\n" +
            $"right: {rightInput.ToString("F4")}({rightInput.magnitude:F4})\n" +
            $"right angle: {RightAngle} {(_rightPressing ? "pressing" : "free")}\n" +
            $"table: {_activeTable}\n" +
            _log;

        if (anglesLeftOld != LeftAngle || anglesRightOld != RightAngle)
            foreach (var display in displays)
                display.OnInput(LeftAngle, RightAngle);
    }

    public static Vector2 GetStickPos(LeftOrRight hand)
    {
        switch (hand)
        {
            case LeftOrRight.Left:
                return new Vector2(Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal"),
                    Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical"));
            case LeftOrRight.Right:
                return new Vector2(Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal"),
                    Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical"));
            default:
                return Vector2.zero;
        }
    }

    private void InputChar(int leftAngle, int rightAngle)
    {
        Log($"input char with {leftAngle} {rightAngle}");
        if (leftAngle < 0) return;
        if (rightAngle < 0) return;

        var c = ActiveTable[leftAngle * 8 + rightAngle];
        switch (c)
        {
            case '\0':
                break;
            // generic operations
            case OpDeleteChar:
                if (mainText.text.Length != 0)
                    mainText.text = mainText.text.Substring(0, mainText.text.Length - 1);
                break;
            case OpBlank:
                mainText.text += ' ';
                break;
            case OpSignPlane:
                Swap(ref _activeTableOld, ref _activeTable);

                TableChanged(_activeTable);
                break;
            case OpNextPlane:
                if (_activeTable == 0)
                    Swap(ref _activeTableOld, ref _activeTable);

                _activeTable++;
                if (_activeTable == _keyboardTables.Length)
                    _activeTable = 1;

                TableChanged(_activeTable);
                break;
            
            // japanese
            case OpJpSmall:
            case OpJpDakuten:
            case OpJpHandakuten:
                // TODO
                break;
            case OpJpNextCandidate:
            case OpJpSelectCandidate:
                // currently nop
                break;

            default:
                mainText.text += c;
                break;
        }
    }

    private static void Swap<T>(ref T a, ref T b)
    {
        // ReSharper disable once SwapViaDeconstruction
        var tmp = a;
        a = b;
        b = tmp;
    }

    private void TableChanged(int activeTable)
    {
        ActiveTable = _keyboardTables[activeTable];
        foreach (var display in displays)
            display.OnTableChanged(ActiveTable);
    }

    private void MakeTables(string str)
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
        var tableCnt = str.Length / TableSize;
        var tables = new char[tableCnt][];

        for (var i = 0; i < tableCnt; i++)
        {
            var table = tables[i] = new char[TableSize];
            for (var j = 0; j < TableSize; j++)
                table[j] = chars[i * TableSize + j];
            table[6 * 8 + 6] = OpDeleteChar;
            table[6 * 8 + 7] = OpBlank;
            table[7 * 8 + 6] = OpSignPlane;
            table[7 * 8 + 7] = OpNextPlane;
        }

        _keyboardTables = tables;
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

public abstract class KeyboardDisplay : UdonSharpBehaviour
{
    public abstract void OnInput(int left, int right);
    public abstract void OnTableChanged(char[] newTable);
}

public enum LeftOrRight
{
    Left,
    Right,
}
