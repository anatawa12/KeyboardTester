
using System;
using System.Linq;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

public class Keyboard : UdonSharpBehaviour
{
    public TextMeshPro Text;
    // \0 is used for the slot not defined
    // \u0001~\u001F can be used for locale specific
    private char[][][] _keyboardTables;

    private const float ActiveMinSqrt = 0.10f * 0.10f;
    private const float IgnoreMaxSqrt = 0.15f * 0.15f;

    private int _activeTable = 1;
    private string _log;

    private bool _leftPressing = false;
    private Vector2 _leftInput = Vector2.zero;
    private bool _rightPressing = false;
    private Vector2 _rightInput = Vector2.zero;

    private void Start()
    {
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
            "y" + "z" + "”" + "." + "?" + "/" + "\0" + "\0" +
            "Y" + "Z" + "‘" + "," + "!" + "-" + "\0" + "\0" +

            "");
    }

    private void Log(string log)
    {
        _log = $"{log}\n{_log}";
    }

    private void Update()
    {
        Debug.Log("Update");
        Text.text = $"left: {_leftInput}({(_leftPressing ? "pressing" : "free")})\n" +
                    $"right: {_rightInput}({(_rightPressing ? "pressing" : "free")})\n" +
                    _log;
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

    private bool UpdateHand(ref Vector2 location, ref bool pressing)
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

    private void UpdateLeft() {
        Log($"update hand: left");
        if (UpdateHand(ref _leftInput, ref _leftPressing))
            PressChanged(true);
    }
    private void UpdateRight() {
        Log($"update hand: right");
        if (UpdateHand(ref _rightInput, ref _rightPressing))
            PressChanged(false);
    }

    public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
    {
        _leftInput.x = value;
        UpdateLeft();
    }

    public override void InputMoveVertical(float value, UdonInputEventArgs args)
    {
        _leftInput.y = value;
        UpdateLeft();
    }

    public override void InputLookHorizontal(float value, UdonInputEventArgs args)
    {
        _rightInput.x = value;
        UpdateRight();
    }

    public override void InputLookVertical(float value, UdonInputEventArgs args)
    {
        _rightInput.y = value;
        UpdateRight();
    }
}
