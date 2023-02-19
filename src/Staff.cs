
/*
 * Copyright (c) 2007-2011 Madhav Vaidyanathan
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

/* @class Staff
 * The Staff is used to draw a single Staff (a row of measures) in the 
 * SheetMusic Control. A Staff needs to draw
 * - The Clef
 * - The key signature
 * - The horizontal lines
 * - A list of MusicSymbols
 * - The left and right vertical lines
 *
 * The height of the Staff is determined by the number of pixels each
 * MusicSymbol extends above and below the staff.
 *
 * The vertical lines (left and right sides) of the staff are joined
 * with the staffs above and below it, with one exception.  
 * The last track is not joined with the first track.
 */

public class Staff {
    private List<MusicSymbol> symbols;  /** The music symbols in this staff */
    private List<LyricSymbol> lyrics;   /** The lyrics to display (can be null) */
    private int ytop;                   /** The y pixel of the top of the staff */
    private ClefSymbol clefsym;         /** The left-side Clef symbol */
    private AccidSymbol[] keys;         /** The key signature symbols */
    private bool showMeasures;          /** If true, show the measure numbers */
    private int keysigWidth;            /** The width of the clef and key signature */
    private int width;                  /** The width of the staff in pixels */
    private int height;                 /** The height of the staff in pixels */
    private int tracknum;               /** The track this staff represents */
    private int totaltracks;            /** The total number of tracks */
    private int starttime;              /** The time (in pulses) of first symbol */
    private int endtime;                /** The time (in pulses) of last symbol */
    private int measureLength;          /** The time (in pulses) of a measure */

    /** Create a new staff with the given list of music symbols,
     * and the given key signature.  The clef is determined by
     * the clef of the first chord symbol. The track number is used
     * to determine whether to join this left/right vertical sides
     * with the staffs above and below. The SheetMusicOptions are used
     * to check whether to display measure numbers or not.
     */
    public Staff(List<MusicSymbol> symbols, KeySignature key, 
                 MidiOptions options,
                 int tracknum, int totaltracks)  {

        keysigWidth = SheetMusic.KeySignatureWidth(key);
        this.tracknum = tracknum;
        this.totaltracks = totaltracks;
        showMeasures = (options.showMeasures && tracknum == 0);
        measureLength = options.time.Measure;
        Clef clef = FindClef(symbols);

        clefsym = new ClefSymbol(clef, 0, false);
        keys = key.GetSymbols(clef);
        this.symbols = symbols;
        CalculateWidth(options.scrollVert);
        CalculateHeight();
        CalculateStartEndTime();
        FullJustify();
    }

    /** Return the width of the staff */
    public int Width {
        get { return width; }
    }

    /** Return the height of the staff */
    public int Height {
        get { return height; }
    }

    /** Return the track number of this staff (starting from 0 */
    public int Track {
        get { return tracknum; }
    }

    /** Return the starting time of the staff, the start time of
     *  the first symbol.  This is used during playback, to 
     *  automatically scroll the music while playing.
     */
    public int StartTime {
        get { return starttime; }
    }

    /** Return the ending time of the staff, the endtime of
     *  the last symbol.  This is used during playback, to 
     *  automatically scroll the music while playing.
     */
    public int EndTime {
        get { return endtime; }
        set { endtime = value; }
    }

    /** Find the initial clef to use for this staff.  Use the clef of
     * the first ChordSymbol.
     */
    private Clef FindClef(List<MusicSymbol> list) {
        foreach (MusicSymbol m in list) {
            if (m is ChordSymbol) {
                ChordSymbol c = (ChordSymbol) m;
                return c.Clef;
            }
        }
        return Clef.Treble;
    }

    /** Calculate the height of this staff.  Each MusicSymbol contains the
     * number of pixels it needs above and below the staff.  Get the maximum
     * values above and below the staff.
     */
    public void CalculateHeight() {
        int above = 0;
        int below = 0;

        foreach (MusicSymbol s in symbols) {
            above = Math.Max(above, s.AboveStaff);
            below = Math.Max(below, s.BelowStaff);
        }
        above = Math.Max(above, clefsym.AboveStaff);
        below = Math.Max(below, clefsym.BelowStaff);
        ytop = above + SheetMusic.NoteHeight;
        height = SheetMusic.NoteHeight*5 + ytop + below;
        if (showMeasures || lyrics != null) {
            height += SheetMusic.NoteHeight * 3/2;
        }

        /* Add some extra vertical space between the last track
         * and first track.
         */
        if (tracknum == totaltracks-1)
            height += SheetMusic.NoteHeight * 3;
    }

    /** Calculate the width of this staff */
    private void CalculateWidth(bool scrollVert) {
        if (scrollVert) {
            width = SheetMusic.PageWidth;
            return;
        }
        width = keysigWidth;
        foreach (MusicSymbol s in symbols) {
            width += s.Width;
        }
    }


    /** Calculate the start and end time of this staff. */
    private void CalculateStartEndTime() {