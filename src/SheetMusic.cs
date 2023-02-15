
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