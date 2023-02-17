
/*
 * Copyright (c) 2007-2012 Madhav Vaidyanathan
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License version 2.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Printing;

namespace MidiSheetMusic {


/** @class SheetMusic
 *
 * The SheetMusic Control is the main class for displaying the sheet music.
 * The SheetMusic class has the following public methods:
 *
 * SheetMusic()
 *   Create a new SheetMusic control from the given midi file and options.
 * 
 * SetZoom()
 *   Set the zoom level to display the sheet music at.
 *
 * DoPrint()
 *   Print a single page of sheet music.
 *
 * GetTotalPages()
 *   Get the total number of sheet music pages.
 *
 * OnPaint()
 *   Method called to draw the SheetMuisc
 *
 * These public methods are called from the MidiSheetMusic Form Window.
 *
 */
public class SheetMusic {

    /* Measurements used when drawing.  All measurements are in pixels.
     * The values depend on whether the menu 'Large Notes' or 'Small Notes' is selected.
     */
    public const  int LineWidth = 1;    /** The width of a line */
    public const  int LeftMargin = 4;   /** The left margin */
    public const  int TitleHeight = 14; /** The height for the title on the first page */
    public static int LineSpace;        /** The space between lines in the staff */
    public static int StaffHeight;      /** The height between the 5 horizontal lines of the staff */
    public static int NoteHeight;      /** The height of a whole note */
    public static int NoteWidth;       /** The width of a whole note */

    public const int PageWidth = 800;    /** The width of each page */
    public const int PageHeight = 1050;  /** The height of each page (when printing) */
    public static Font LetterFont;       /** The font for drawing the letters */

    private List<Staff> staffs; /** The array of staffs to display (from top to bottom) */
    private KeySignature mainkey; /** The main key signature */
    private int    numtracks;     /** The number of tracks */
    private float  zoom;          /** The zoom level to draw at (1.0 == 100%) */
    private bool   scrollVert;    /** Whether to scroll vertically or horizontally */
    private string filename;      /** The name of the midi file */
    private int showNoteLetters;    /** Display the note letters */
    private Color[] NoteColors;     /** The note colors to use */
    private SolidBrush shadeBrush;  /** The brush for shading */
    private SolidBrush shade2Brush; /** The brush for shading left-hand piano */
    private Pen pen;                /** The black pen for drawing */


    /** Initialize the default note sizes.  */
    static SheetMusic() {
        SetNoteSize(false);
    }

    /** Create a new SheetMusic control, using the given parsed MidiFile.
     *  The options can be null.
     */
    public SheetMusic(MidiFile file, MidiOptions options) {
        init(file, options); 
    }

    /** Create a new SheetMusic control, using the given midi filename.
     *  The options can be null.
     */
    public SheetMusic(string filename, MidiOptions options) {
        MidiFile file = new MidiFile(filename);
        init(file, options); 
    }

    /** Create a new SheetMusic control, using the given raw midi byte[] data.
     *  The options can be null.
     */
    public SheetMusic(byte[] data, string title, MidiOptions options) {
        MidiFile file = new MidiFile(data, title);
        init(file, options); 
    }


    /** Create a new SheetMusic control.
     * MidiFile is the parsed midi file to display.
     * SheetMusic Options are the menu options that were selected.
     *
     * - Apply all the Menu Options to the MidiFile tracks.
     * - Calculate the key signature
     * - For each track, create a list of MusicSymbols (notes, rests, bars, etc)
     * - Vertically align the music symbols in all the tracks
     * - Partition the music notes into horizontal staffs
     */
    public void init(MidiFile file, MidiOptions options) {
        if (options == null) {
            options = new MidiOptions(file);
        }
        zoom = 1.0f;
        filename = file.FileName;

        SetColors(options.colors, options.shadeColor, options.shade2Color);
        pen = new Pen(Color.Black, 1);

        List<MidiTrack> tracks = file.ChangeMidiNotes(options);
        SetNoteSize(options.largeNoteSize);
        scrollVert = options.scrollVert;
        showNoteLetters= options.showNoteLetters;
        TimeSignature time = file.Time; 
        if (options.time != null) {
            time = options.time;
        }
        if (options.key == -1) {
            mainkey = GetKeySignature(tracks);
        }
        else {
            mainkey = new KeySignature(options.key);
        }

        numtracks = tracks.Count;

        int lastStart = file.EndTime() + options.shifttime;

        /* Create all the music symbols (notes, rests, vertical bars, and
         * clef changes).  The symbols variable contains a list of music 
         * symbols for each track.  The list does not include the left-side 
         * Clef and key signature symbols.  Those can only be calculated 
         * when we create the staffs.
         */
        List<MusicSymbol>[] symbols = new List<MusicSymbol> [ numtracks ];
        for (int tracknum = 0; tracknum < numtracks; tracknum++) {
            MidiTrack track = tracks[tracknum];
            ClefMeasures clefs = new ClefMeasures(track.Notes, time.Measure);
            List<ChordSymbol> chords = CreateChords(track.Notes, mainkey, time, clefs);
            symbols[tracknum] = CreateSymbols(chords, clefs, time, lastStart);
        }

        List<LyricSymbol>[] lyrics = null;
        if (options.showLyrics) {
            lyrics = GetLyrics(tracks);
        }

        /* Vertically align the music symbols */
        SymbolWidths widths = new SymbolWidths(symbols, lyrics);
        // SymbolWidths widths = new SymbolWidths(symbols);
        AlignSymbols(symbols, widths);

        staffs = CreateStaffs(symbols, mainkey, options, time.Measure);
        CreateAllBeamedChords(symbols, time);
        if (lyrics != null) {
            AddLyricsToStaffs(staffs, lyrics);
        }

        /* After making chord pairs, the stem directions can change,
         * which affects the staff height.  Re-calculate the staff height.
         */
        foreach (Staff staff in staffs) {
            staff.CalculateHeight();
        }

        //BackColor = Color.White;

        SetZoom(1.0f);
    }


    /** Get the best key signature given the midi notes in all the tracks. */
    private KeySignature GetKeySignature(List<MidiTrack> tracks) {
        List<int> notenums = new List<int>();
        foreach (MidiTrack track in tracks) {
            foreach (MidiNote note in track.Notes) {
                notenums.Add(note.Number);
            }
        }
        return KeySignature.Guess(notenums);
    }


    /** Create the chord symbols for a single track.
     * @param midinotes  The Midinotes in the track.
     * @param key        The Key Signature, for determining sharps/flats.
     * @param time       The Time Signature, for determining the measures.
     * @param clefs      The clefs to use for each measure.
     * @ret An array of ChordSymbols
     */
    private
    List<ChordSymbol> CreateChords(List<MidiNote> midinotes, 
                                   KeySignature key,
                                   TimeSignature time,
                                   ClefMeasures clefs) {

        int i = 0;
        List<ChordSymbol> chords = new List<ChordSymbol>();
        List<MidiNote> notegroup = new List<MidiNote>(12);
        int len = midinotes.Count; 

        while (i < len) {

            int starttime = midinotes[i].StartTime;
            Clef clef = clefs.GetClef(starttime);

            /* Group all the midi notes with the same start time
             * into the notes list.
             */
            notegroup.Clear();
            notegroup.Add(midinotes[i]);
            i++;
            while (i < len && midinotes[i].StartTime == starttime) {
                notegroup.Add(midinotes[i]);
                i++;
            }

            /* Create a single chord from the group of midi notes with
             * the same start time.
             */
            ChordSymbol chord = new ChordSymbol(notegroup, key, time, clef, this);
            chords.Add(chord);
        }

        return chords;
    }

    /** Given the chord symbols for a track, create a new symbol list
     * that contains the chord symbols, vertical bars, rests, and clef changes.
     * Return a list of symbols (ChordSymbol, BarSymbol, RestSymbol, ClefSymbol)
     */
    private List<MusicSymbol> 
    CreateSymbols(List<ChordSymbol> chords, ClefMeasures clefs,
                  TimeSignature time, int lastStart) {

        List<MusicSymbol> symbols = new List<MusicSymbol>();
        symbols = AddBars(chords, time, lastStart);
        symbols = AddRests(symbols, time);
        symbols = AddClefChanges(symbols, clefs, time);

        return symbols;
    }

    /** Add in the vertical bars delimiting measures. 
     *  Also, add the time signature symbols.
     */
    private
    List<MusicSymbol> AddBars(List<ChordSymbol> chords, TimeSignature time,
                              int lastStart) {

        List<MusicSymbol> symbols = new List<MusicSymbol>();

        TimeSigSymbol timesig = new TimeSigSymbol(time.Numerator, time.Denominator);
        symbols.Add(timesig);

        /* The starttime of the beginning of the measure */
        int measuretime = 0;

        int i = 0;
        while (i < chords.Count) {
            if (measuretime <= chords[i].StartTime) {
                symbols.Add(new BarSymbol(measuretime) );
                measuretime += time.Measure;
            }
            else {
                symbols.Add(chords[i]);
                i++;
            }
        }

        /* Keep adding bars until the last StartTime (the end of the song) */
        while (measuretime < lastStart) {
            symbols.Add(new BarSymbol(measuretime) );
            measuretime += time.Measure;
        }

        /* Add the final vertical bar to the last measure */
        symbols.Add(new BarSymbol(measuretime) );
        return symbols;
    }

    /** Add rest symbols between notes.  All times below are 
     * measured in pulses.
     */
    private
    List<MusicSymbol> AddRests(List<MusicSymbol> symbols, TimeSignature time) {
        int prevtime = 0;

        List<MusicSymbol> result = new List<MusicSymbol>( symbols.Count );

        foreach (MusicSymbol symbol in symbols) {
            int starttime = symbol.StartTime;
            RestSymbol[] rests = GetRests(time, prevtime, starttime);
            if (rests != null) {
                foreach (RestSymbol r in rests) {
                    result.Add(r);
                }
            }

            result.Add(symbol);

            /* Set prevtime to the end time of the last note/symbol. */
            if (symbol is ChordSymbol) {
                ChordSymbol chord = (ChordSymbol)symbol;
                prevtime = Math.Max( chord.EndTime, prevtime );
            }
            else {
                prevtime = Math.Max(starttime, prevtime);
            }
        }
        return result;
    }

    /** Return the rest symbols needed to fill the time interval between
     * start and end.  If no rests are needed, return nil.
     */
    private
    RestSymbol[] GetRests(TimeSignature time, int start, int end) {
        RestSymbol[] result;
        RestSymbol r1, r2;

        if (end - start < 0)
            return null;

        NoteDuration dur = time.GetNoteDuration(end - start);
        switch (dur) {
            case NoteDuration.Whole:
            case NoteDuration.Half:
            case NoteDuration.Quarter:
            case NoteDuration.Eighth:
                r1 = new RestSymbol(start, dur);
                result = new RestSymbol[]{ r1 };
                return result;

            case NoteDuration.DottedHalf:
                r1 = new RestSymbol(start, NoteDuration.Half);
                r2 = new RestSymbol(start + time.Quarter*2, 
                                    NoteDuration.Quarter);
                result = new RestSymbol[]{ r1, r2 };
                return result;

            case NoteDuration.DottedQuarter:
                r1 = new RestSymbol(start, NoteDuration.Quarter);
                r2 = new RestSymbol(start + time.Quarter, 
                                    NoteDuration.Eighth);
                result = new RestSymbol[]{ r1, r2 };
                return result; 

            case NoteDuration.DottedEighth:
                r1 = new RestSymbol(start, NoteDuration.Eighth);
                r2 = new RestSymbol(start + time.Quarter/2, 
                                    NoteDuration.Sixteenth);
                result = new RestSymbol[]{ r1, r2 };
                return result;

            default:
                return null;
        }
    }

    /** The current clef is always shown at the beginning of the staff, on
     * the left side.  However, the clef can also change from measure to 
     * measure. When it does, a Clef symbol must be shown to indicate the 
     * change in clef.  This function adds these Clef change symbols.
     * This function does not add the main Clef Symbol that begins each
     * staff.  That is done in the Staff() contructor.
     */
    private
    List<MusicSymbol> AddClefChanges(List<MusicSymbol> symbols,
                                     ClefMeasures clefs,
                                     TimeSignature time) {

        List<MusicSymbol> result = new List<MusicSymbol>( symbols.Count );
        Clef prevclef = clefs.GetClef(0);
        foreach (MusicSymbol symbol in symbols) {
            /* A BarSymbol indicates a new measure */
            if (symbol is BarSymbol) {
                Clef clef = clefs.GetClef(symbol.StartTime);
                if (clef != prevclef) {
                    result.Add(new ClefSymbol(clef, symbol.StartTime-1, true));
                }
                prevclef = clef;
            }
            result.Add(symbol);
        }
        return result;
    }
           

    /** Notes with the same start times in different staffs should be
     * vertically aligned.  The SymbolWidths class is used to help 
     * vertically align symbols.
     *
     * First, each track should have a symbol for every starttime that
     * appears in the Midi File.  If a track doesn't have a symbol for a
     * particular starttime, then add a "blank" symbol for that time.
     *
     * Next, make sure the symbols for each start time all have the same
     * width, across all tracks.  The SymbolWidths class stores
     * - The symbol width for each starttime, for each track
     * - The maximum symbol width for a given starttime, across all tracks.
     *
     * The method SymbolWidths.GetExtraWidth() returns the extra width
     * needed for a track to match the maximum symbol width for a given
     * starttime.
     */
    private
    void AlignSymbols(List<MusicSymbol>[] allsymbols, SymbolWidths widths) {

        for (int track = 0; track < allsymbols.Length; track++) {
            List<MusicSymbol> symbols = allsymbols[track];
            List<MusicSymbol> result = new List<MusicSymbol>();

            int i = 0;

            /* If a track doesn't have a symbol for a starttime,
             * add a blank symbol.
             */
            foreach (int start in widths.StartTimes) {

                /* BarSymbols are not included in the SymbolWidths calculations */
                while (i < symbols.Count && (symbols[i] is BarSymbol) &&
                    symbols[i].StartTime <= start) {
                    result.Add(symbols[i]);
                    i++;
                }

                if (i < symbols.Count && symbols[i].StartTime == start) {

                    while (i < symbols.Count && 
                           symbols[i].StartTime == start) {

                        result.Add(symbols[i]);
                        i++;
                    }
                }
                else {
                    result.Add(new BlankSymbol(start, 0));
                }
            }

            /* For each starttime, increase the symbol width by
             * SymbolWidths.GetExtraWidth().
             */
            i = 0;
            while (i < result.Count) {
                if (result[i] is BarSymbol) {
                    i++;
                    continue;
                }
                int start = result[i].StartTime;
                int extra = widths.GetExtraWidth(track, start);
                result[i].Width += extra;

                /* Skip all remaining symbols with the same starttime. */
                while (i < result.Count && result[i].StartTime == start) {
                    i++;
                }
            } 
            allsymbols[track] = result;
        }
    }

    private static bool IsChord(MusicSymbol symbol) {
        return symbol is ChordSymbol;
    }


    /** Find 2, 3, 4, or 6 chord symbols that occur consecutively (without any
     *  rests or bars in between).  There can be BlankSymbols in between.
     *
     *  The startIndex is the index in the symbols to start looking from.
     *
     *  Store the indexes of the consecutive chords in chordIndexes.
     *  Store the horizontal distance (pixels) between the first and last chord.
     *  If we failed to find consecutive chords, return false.
     */
    private static bool
    FindConsecutiveChords(List<MusicSymbol> symbols, TimeSignature time,
                          int startIndex, int[] chordIndexes, 
                          ref int horizDistance) {

        int i = startIndex;
        int numChords = chordIndexes.Length;

        while (true) {
            horizDistance = 0;

            /* Find the starting chord */
            while (i < symbols.Count - numChords) {
                if (symbols[i] is ChordSymbol) {
                    ChordSymbol c = (ChordSymbol) symbols[i];
                    if (c.Stem != null) {
                        break;
                    }
                }
                i++;
            }
            if (i >= symbols.Count - numChords) {
                chordIndexes[0] = -1;
                return false;
            }
            chordIndexes[0] = i;
            bool foundChords = true;
            for (int chordIndex = 1; chordIndex < numChords; chordIndex++) {
                i++;
                int remaining = numChords - 1 - chordIndex;
                while ((i < symbols.Count - remaining) && (symbols[i] is BlankSymbol)) {
                    horizDistance += symbols[i].Width;
                    i++;
                }
                if (i >= symbols.Count - remaining) {
                    return false;
                }
                if (!(symbols[i] is ChordSymbol)) {
                    foundChords = false;
                    break;
                }
                chordIndexes[chordIndex] = i;
                horizDistance += symbols[i].Width;
            }
            if (foundChords) {
                return true;
            }

            /* Else, start searching again from index i */
        }
    }


    /** Connect chords of the same duration with a horizontal beam.
     *  numChords is the number of chords per beam (2, 3, 4, or 6).
     *  if startBeat is true, the first chord must start on a quarter note beat.
     */
    private static void
    CreateBeamedChords(List<MusicSymbol>[] allsymbols, TimeSignature time,
                       int numChords, bool startBeat) {
        int[] chordIndexes = new int[numChords];
        ChordSymbol[] chords = new ChordSymbol[numChords];

        foreach (List<MusicSymbol> symbols in allsymbols) {
            int startIndex = 0;
            while (true) {
                int horizDistance = 0;
                bool found = FindConsecutiveChords(symbols, time,
                                                   startIndex,
                                                   chordIndexes,
                                                   ref horizDistance);
                if (!found) {
                    break;
                }
                for (int i = 0; i < numChords; i++) {
                    chords[i] = (ChordSymbol)symbols[ chordIndexes[i] ];
                }

                if (ChordSymbol.CanCreateBeam(chords, time, startBeat)) {
                    ChordSymbol.CreateBeam(chords, horizDistance);
                    startIndex = chordIndexes[numChords-1] + 1;
                }
                else {
                    startIndex = chordIndexes[0] + 1;
                }

                /* What is the value of startIndex here?
                 * If we created a beam, we start after the last chord.
                 * If we failed to create a beam, we start after the first chord.
                 */
            }
        }
    }


    /** Connect chords of the same duration with a horizontal beam.
     *
     *  We create beams in the following order:
     *  - 6 connected 8th note chords, in 3/4, 6/8, or 6/4 time
     *  - Triplets that start on quarter note beats
     *  - 3 connected chords that start on quarter note beats (12/8 time only)
     *  - 4 connected chords that start on quarter note beats (4/4 or 2/4 time only)
     *  - 2 connected chords that start on quarter note beats
     *  - 2 connected chords that start on any beat
     */ 
    private static void
    CreateAllBeamedChords(List<MusicSymbol>[] allsymbols, TimeSignature time) {
        if ((time.Numerator == 3 && time.Denominator == 4) ||
            (time.Numerator == 6 && time.Denominator == 8) ||
            (time.Numerator == 6 && time.Denominator == 4) ) {

            CreateBeamedChords(allsymbols, time, 6, true);
        }
        CreateBeamedChords(allsymbols, time, 3, true);
        CreateBeamedChords(allsymbols, time, 4, true);
        CreateBeamedChords(allsymbols, time, 2, true);
        CreateBeamedChords(allsymbols, time, 2, false);
    }

    /** Get the width (in pixels) needed to display the key signature */
    public static int
    KeySignatureWidth(KeySignature key) {
        ClefSymbol clefsym = new ClefSymbol(Clef.Treble, 0, false);
        int result = clefsym.MinWidth;
        AccidSymbol[] keys = key.GetSymbols(Clef.Treble);
        foreach (AccidSymbol symbol in keys) {
            result += symbol.MinWidth;
        }
        return result + SheetMusic.LeftMargin + 5;
    }


    /** Given MusicSymbols for a track, create the staffs for that track.
     *  Each Staff has a maxmimum width of PageWidth (800 pixels).
     *  Also, measures should not span multiple Staffs.
     */
    private List<Staff> 
    CreateStaffsForTrack(List<MusicSymbol> symbols, int measurelen, 
                         KeySignature key, MidiOptions options,
                         int track, int totaltracks) {
        int keysigWidth = KeySignatureWidth(key);
        int startindex = 0;
        List<Staff> thestaffs = new List<Staff>(symbols.Count / 50);

        while (startindex < symbols.Count) {
            /* startindex is the index of the first symbol in the staff.
             * endindex is the index of the last symbol in the staff.
             */
            int endindex = startindex;
            int width = keysigWidth;
            int maxwidth;

            /* If we're scrolling vertically, the maximum width is PageWidth. */
            if (scrollVert) {
                maxwidth = SheetMusic.PageWidth;
            }
            else {
                maxwidth = 2000000;
            }

            while (endindex < symbols.Count &&
                   width + symbols[endindex].Width < maxwidth) {

                width += symbols[endindex].Width;
                endindex++;
            }
            endindex--;

            /* There's 3 possibilities at this point:
             * 1. We have all the symbols in the track.
             *    The endindex stays the same.
             *
             * 2. We have symbols for less than one measure.
             *    The endindex stays the same.
             *
             * 3. We have symbols for 1 or more measures.
             *    Since measures cannot span multiple staffs, we must
             *    make sure endindex does not occur in the middle of a
             *    measure.  We count backwards until we come to the end
             *    of a measure.
             */

            if (endindex == symbols.Count - 1) {
                /* endindex stays the same */
            }
            else if (symbols[startindex].StartTime / measurelen ==
                     symbols[endindex].StartTime / measurelen) {
                /* endindex stays the same */
            }
            else {
                int endmeasure = symbols[endindex+1].StartTime/measurelen;
                while (symbols[endindex].StartTime / measurelen == 
                       endmeasure) {
                    endindex--;
                }
            }
            int range = endindex + 1 - startindex;
            if (scrollVert) {
                width = SheetMusic.PageWidth;
            }
            Staff staff = new Staff(symbols.GetRange(startindex, range),
                                    key, options, track, totaltracks);
            thestaffs.Add(staff);
            startindex = endindex + 1;
        }
        return thestaffs;
    }


    /** Given all the MusicSymbols for every track, create the staffs
     * for the sheet music.  There are two parts to this:
     *
     * - Get the list of staffs for each track.
     *   The staffs will be stored in trackstaffs as:
     *
     *   trackstaffs[0] = { Staff0, Staff1, Staff2, ... } for track 0
     *   trackstaffs[1] = { Staff0, Staff1, Staff2, ... } for track 1
     *   trackstaffs[2] = { Staff0, Staff1, Staff2, ... } for track 2
     *
     * - Store the Staffs in the staffs list, but interleave the
     *   tracks as follows:
     *
     *   staffs = { Staff0 for track 0, Staff0 for track1, Staff0 for track2,
     *              Staff1 for track 0, Staff1 for track1, Staff1 for track2,
     *              Staff2 for track 0, Staff2 for track1, Staff2 for track2,
     *              ... } 
     */
    private List<Staff> 
    CreateStaffs(List<MusicSymbol>[] allsymbols, KeySignature key, 
                 MidiOptions options, int measurelen) {

        List<Staff>[] trackstaffs = new List<Staff>[ allsymbols.Length ];