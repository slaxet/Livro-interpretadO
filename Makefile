TARGET = sheet.exe

all: $(TARGET)

$(TARGET): src/*.cs
	gmcs \
	 -res:img/NotePair.ico,MidiSheetMusic.NotePair.ico \
	 -res:img/treble.png,MidiSheetMusic.treble.png  \
	 -res:img/bass.png,MidiSheetMusic.bass.png  \
	 -re