using System;
using TMPro;
using UdonSharp;
using UnityEngine.UI;

// ReSharper disable once InconsistentNaming
public class TwitterQR : UdonSharpBehaviour
{
    public GoodQrGen goodQrGen;
    public Text output;
    public TextMeshPro text;

    private string textKnown;

    private void Update()
    {
        if (textKnown != text.text)
        {
            textKnown = text.text;

            var url = URLEncode(textKnown,
                "https://twitter.com/intent/tweet?text=",
                "&hashtags=clekeyVRC");

            goodQrGen.StartCreatingQR(url, this, nameof(OnQRCreated));
            output.text = "Creating Twitter QR...";
        }
    }

    private static bool IsUrlOneSizeChar(char c)
    {
        if (c == ' ') return true;
        if ('a' <= c && c <= 'z') return true;
        if ('A' <= c && c <= 'Z') return true;
        if ('0' <= c && c <= '9') return true;
        if ("-_.!~*'()".IndexOf(c) >= 0) return true;
        return false;
    }

    private static char Hex(int c) => c < 10 ? (char)('0' + c) : (char)('A' - 10 + c);

    private static string URLEncode(string encodes, string prefix = "", string suffix = "")
    {
        var resultLength = prefix.Length + suffix.Length;

        foreach (var c in encodes)
            if (IsUrlOneSizeChar(c)) resultLength += 1;
            else if (c <= 0x007F) resultLength += 1 * 3;
            else if (c <= 0x07FF) resultLength += 2 * 3;
            else resultLength += 3 * 3;

        var result = new char[resultLength];
        if (prefix.Length != 0) Array.Copy(prefix.ToCharArray(), result, prefix.Length);

        var i = prefix.Length;
        foreach (var c in encodes)
        {
            if (c == ' ') result[i++] = '+';
            else if (IsUrlOneSizeChar(c)) result[i++] = c;
            else if (c <= 0x007F)
            {
                result[i++] = '%';
                result[i++] = Hex((c >> 4) & 0xF);
                result[i++] = Hex((c >> 0) & 0xF);
            }
            else if (c <= 0x07FF)
            {
                // 0b110a_bbbb 0b10cc_dddd
                result[i++] = '%';
                result[i++] = Hex((c >> 10) & 0x1 | 0x1100);
                result[i++] = Hex((c >> 6) & 0xF);
                result[i++] = '%';
                result[i++] = Hex((c >> 4) & 0x3 | 0x1000);
                result[i++] = Hex((c >> 0) & 0xF);
            }
            else 
            {
                // 0b1110_aaaa 0b10bb_cccc 0b10dd_eeee
                result[i++] = '%';
                result[i++] = 'E'; // 0b1110 == 0xE
                result[i++] = Hex((c >> 12) & 0xF);
                result[i++] = '%';
                result[i++] = Hex((c >> 8) & 0x3 | 0x1000);
                result[i++] = Hex((c >> 6) & 0xF);
                result[i++] = '%';
                result[i++] = Hex((c >> 4) & 0x3 | 0x1000);
                result[i++] = Hex((c >> 0) & 0xF);
            }
        }

        Array.Copy(suffix.ToCharArray(), 0, result, i, suffix.Length);

        return new string(result);
    }

    public void OnQRCreated()
    {
        output.text = goodQrGen.Output;
    }
}