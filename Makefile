TARGET = sheet.exe

all: $(TARGET)

$(TARGET): src/*.cs
	gmcs \
	 -res:img/NotePair.ico,MidiSheetMusic.NotePair.ico \
	 -res:img/treble.png,MidiSheetMusic.treble.png  \
	 -res:img/bass.png,MidiSheetMusic.bass.png  \
	 -res:img/two.png,MidiSheetMusic.two.png \
	 -res:img/three.png,MidiSheetMusic.three.png \
	 -res:img/four.png,MidiSheetMusic.fo