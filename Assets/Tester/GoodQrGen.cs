using System;
using System.Text;
using UdonSharp;
using UnityEngine;
using VRC.Udon;

public class GoodQrGen : UdonSharpBehaviour
{
    [SerializeField] private UdonQR udonQr;
    [NonSerialized] public string Output;

    // inputs
    private string _input;
    private UdonSharpBehaviour _callbackBehaviour;
    private string _callbackMethod;

    // step is 
    //  0: waiting
    //  1: create QR
    //  2: rank: same cell for 5 more horizontal
    //  3: rank: same cell for 5 more vertical
    //  4: rank: 2 by 2 cell
    //  5: rank: horizontal finder pattern like
    //  6: rank: vertical finder pattern like
    //  7: rank: percentage 
    //     and run next step or call callback
    // there's 3 sleep frame per step. this counter is at second byte
    private int _step;
    private int _pattern;
    //private int _iteration;
    private int _width;

    private string[] _qrStrings;
    private char[][] _qrs;
    private int[] _ranks;

    public void StartCreatingQR(
        string input,
        UdonSharpBehaviour callbackBehaviour,
        string callbackMethod)
    {
        _input = input;
        _callbackBehaviour = callbackBehaviour;
        _callbackMethod = callbackMethod;
        _pattern = 0;
        _step = 1;
        _qrs = new char[8][];
        _qrStrings = new string[8];
        _ranks = new int[8];
        Output = null;
    }

    private void Update()
    {
        if (_step == 0) return;
        Debug.Log($"GoodQrGen: {_step} for {_pattern}");
        switch (_step)
        {
            case 1:
            {
                var result = udonQr.Create(_input, 0, _pattern);
                _qrs[_pattern] = result.ToCharArray();
                _qrStrings[_pattern] = result;
                _width = Width(result);
                _step++;
                break;
            }
            case 2:
                _ranks[_pattern] += RankFiveSameCell(_qrs[_pattern], _width, _width + 1, 1);
                _step++;
                break;
            case 3:
                _ranks[_pattern] += RankFiveSameCell(_qrs[_pattern], _width, 1, _width + 1);
                _step++;
                break;
            case 4:
                _ranks[_pattern] += RankTwoByTwo(_qrs[_pattern], _width);
                _step++;
                break;
            case 5:
                _ranks[_pattern] += RankFinderPattern(_qrs[_pattern], _width, _width + 1, 1);
                _step++;
                break;
            case 6:
                _ranks[_pattern] += RankFinderPattern(_qrs[_pattern], _width, 1, _width + 1);
                _step++;
                break;
            case 7:
                _ranks[_pattern] += RankRatio(_qrs[_pattern], _width);
                if (_pattern == 7)
                {
                    var minPattern = 0;
                    var minRank = _ranks[0];
                    for (int i = 1; i < 8; i++)
                    {
                        if (_ranks[i] < minRank)
                        {
                            minRank = _ranks[i];
                            minPattern = i;
                        }
                    }

                    Output = _qrStrings[minPattern];
                    if (_callbackBehaviour != null)
                        _callbackBehaviour.SendCustomEvent(_callbackMethod);
                    _callbackBehaviour = null;
                    _callbackMethod = null;
                    _qrs = null;
                    _qrStrings = null;
                    _ranks = null;
                    _input = null;
                    _step = 0;
                }
                else
                {
                    _pattern++;
                    _step = 1;
                }
                break;
        }

        //_step += 0x100;
        //if ((_step & 0xFF00) == 0x0400)
        //    _step &= 0x00FF;
    }

    private int Width(string str)
    {
        for (var i = 1; i <= 40; i++)
        {
            var width = 17 + 4 * i;
            if (str.Length == width * (width + 1))
                return width;
        }

        return Die<int>();
    }

    private static int RankFiveSameCell(char[] qr, int width, int iStep, int jStep)
    {
        var rank = 0;
        for (var i = 0; i < width; i++)
        {
            var lastColor = '\0';
            var count = 0;
            for (var j = 0; j < width; j++)
            {
                var c = qr[i * iStep + j * jStep];
                if (c == lastColor)
                {
                    count++;
                }
                else
                {
                    if (count >= 5)
                        rank += count - 2;
                    lastColor = c;
                }
            }
        }

        return rank;
    }

    private static int RankTwoByTwo(char[] qr, int width)
    {
        var squareCount = 0;
        var line = width + 1;
        for (var i = 0; i < width - 1; i++)
        {
            var lastColor = qr[i * line];
            for (var j = 1; j < width; j++)
            {
                var c = qr[i * line + j];
                if (c == lastColor)
                {
                    var lastLine = (i + 1) * line;
                    if (qr[lastLine + j - 1] == lastColor && qr[lastLine + j] == lastColor)
                    {
                        squareCount++;
                    }
                }
                lastColor = c;
            }
        }

        return squareCount * 3;
    }

    private static int RankFinderPattern(char[] str, int width, int iStep, int jStep)
    {
        var patternCnt = 0;
        for (var i = 0; i < width; i++)
        {
            var stat = 0;
            for (var j = 0; j < width; j++)
            {
                var c = str[i * iStep + j * jStep];
                stat = stat << 1 | (c == White ? 1 : 0);
                stat &= 0b111111111111;
                if (stat == 0b111101000101 || stat == 0b101000101111)
                    patternCnt += 1;
            }
        }

        return patternCnt * 40;
    }

    private static int RankRatio(char[] qr, int width)
    {
        var blackCnt = 0;
        var line = width + 1;
        for (var i = 0; i < width; i++)
        for (var j = 0; j < width; j++)
            if (qr[i * line + j] != White)
                blackCnt++;
        var total = width * width;
        var percentage = blackCnt * 100 / (float)total;

        for (int i = 0; i < 10; i++)
        {
            var min = 50 - i * 5;
            var max = 50 + i * 5;
            if (min <= percentage && percentage <= max)
                return i * 10;
        }
        return 90;
    }

    private const char White = '\u2591';

    static T Die<T>()
    {
        // ReSharper disable once PossibleNullReferenceException
        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
        ((string)null).ToString();
        return default;
    }
}
