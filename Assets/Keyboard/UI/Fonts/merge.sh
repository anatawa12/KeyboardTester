#!/usr/bin/env fontforge

Open("Noto_Emoji/static/NotoEmoji-Regular.ttf")
SelectAll()
ScaleToEm(1024)
Generate("1.ttf")

Open("Noto_Sans_Symbols_2/NotoSansSymbols2-Regular.ttf")
SelectAll()
ScaleToEm(1024)
Generate("2.ttf")

#Open("Noto_Sans_Symbols/static/NotoSansSymbols-Regular.ttf")
#SelectAll()
#ScaleToEm(1024)
#Generate("3.ttf")

Open("1.ttf")
MergeFonts("2.ttf")
#MergeFonts("3.ttf")
Generate("NotoMerged-Regular.ttf")
Close()



