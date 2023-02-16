
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