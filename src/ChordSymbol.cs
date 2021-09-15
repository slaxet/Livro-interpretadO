
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
using System.Drawing.Drawing2D;

namespace MidiSheetMusic {

public enum StemDir { Up, Down };

/** @class NoteData
 *  Contains fields for displaying a single note in a chord.
 */
public class NoteData {
    public int number;             /** The Midi note number, used to determine the color */
    public WhiteNote whitenote;    /** The white note location to draw */
    public NoteDuration duration;  /** The duration of the note */
    public bool leftside;          /** Whether to draw note to the left or right of the stem */
    public Accid accid;            /** Used to create the AccidSymbols for the chord */
};

/** @class ChordSymbol
 * A chord symbol represents a group of notes that are played at the same
 * time.  A chord includes the notes, the accidental symbols for each
 * note, and the stem (or stems) to use.  A single chord may have two 
 * stems if the notes have different durations (e.g. if one note is a
 * quarter note, and another is an eighth note).
 */
public class ChordSymbol : MusicSymbol {
    private Clef clef;             /** Which clef the chord is being drawn in */
    private int starttime;         /** The time (in pulses) the notes occurs at */
    private int endtime;           /** The starttime plus the longest note duration */
    private NoteData[] notedata;   /** The notes to draw */
    private AccidSymbol[] accidsymbols;   /** The accidental symbols to draw */
    private int width;             /** The width of the chord */
    private Stem stem1;            /** The stem of the chord. Can be null. */
    private Stem stem2;            /** The second stem of the chord. Can be null */
    private bool hastwostems;      /** True if this chord has two stems */
    private SheetMusic sheetmusic; /** Used to get colors and other options */


    /** Create a new Chord Symbol from the given list of midi notes.
     * All the midi notes will have the same start time.  Use the
     * key signature to get the white key and accidental symbol for
     * each note.  Use the time signature to calculate the duration
     * of the notes. Use the clef when drawing the chord.
     */
    public ChordSymbol(List<MidiNote> midinotes, KeySignature key, 
                       TimeSignature time, Clef c, SheetMusic sheet) {

        int len = midinotes.Count;
        int i;

        hastwostems = false;
        clef = c;
        sheetmusic = sheet;

        starttime = midinotes[0].StartTime;
        endtime = midinotes[0].EndTime;

        for (i = 0; i < midinotes.Count; i++) {
            if (i > 1) {
                if (midinotes[i].Number < midinotes[i-1].Number) {
                    throw new System.ArgumentException("Chord notes not in increasing order by number");
                }
            }
            endtime = Math.Max(endtime, midinotes[i].EndTime);
        }

        notedata = CreateNoteData(midinotes, key, time);
        accidsymbols = CreateAccidSymbols(notedata, clef);


        /* Find out how many stems we need (1 or 2) */
        NoteDuration dur1 = notedata[0].duration;
        NoteDuration dur2 = dur1;
        int change = -1;
        for (i = 0; i < notedata.Length; i++) {
            dur2 = notedata[i].duration;
            if (dur1 != dur2) {
                change = i;
                break;
            }
        }

        if (dur1 != dur2) {
            /* We have notes with different durations.  So we will need
             * two stems.  The first stem points down, and contains the
             * bottom note up to the note with the different duration.
             *
             * The second stem points up, and contains the note with the
             * different duration up to the top note.
             */
            hastwostems = true;
            stem1 = new Stem(notedata[0].whitenote, 
                             notedata[change-1].whitenote,
                             dur1, 
                             Stem.Down,
                             NotesOverlap(notedata, 0, change)
                            );

            stem2 = new Stem(notedata[change].whitenote, 
                             notedata[notedata.Length-1].whitenote,
                             dur2, 
                             Stem.Up,
                             NotesOverlap(notedata, change, notedata.Length)
                            );
        }
        else {
            /* All notes have the same duration, so we only need one stem. */
            int direction = StemDirection(notedata[0].whitenote, 
                                          notedata[notedata.Length-1].whitenote,
                                          clef);

            stem1 = new Stem(notedata[0].whitenote,
                             notedata[notedata.Length-1].whitenote,
                             dur1, 
                             direction,
                             NotesOverlap(notedata, 0, notedata.Length)
                            );
            stem2 = null;
        }

        /* For whole notes, no stem is drawn. */
        if (dur1 == NoteDuration.Whole)
            stem1 = null;
        if (dur2 == NoteDuration.Whole)
            stem2 = null;

        width = MinWidth;
    }


    /** Given the raw midi notes (the note number and duration in pulses),
     * calculate the following note data:
     * - The white key
     * - The accidental (if any)
     * - The note duration (half, quarter, eighth, etc)
     * - The side it should be drawn (left or side)
     * By default, notes are drawn on the left side.  However, if two notes
     * overlap (like A and B) you cannot draw the next note directly above it.
     * Instead you must shift one of the notes to the right.
     *
     * The KeySignature is used to determine the white key and accidental.
     * The TimeSignature is used to determine the duration.
     */
 
    private static NoteData[] 
    CreateNoteData(List<MidiNote> midinotes, KeySignature key,
                              TimeSignature time) {

        int len = midinotes.Count;
        NoteData[] notedata = new NoteData[len];

        for (int i = 0; i < len; i++) {
            MidiNote midi = midinotes[i];
            notedata[i] = new NoteData();
            notedata[i].number = midi.Number;
            notedata[i].leftside = true;
            notedata[i].whitenote = key.GetWhiteNote(midi.Number);
            notedata[i].duration = time.GetNoteDuration(midi.EndTime - midi.StartTime);
            notedata[i].accid = key.GetAccidental(midi.Number, midi.StartTime / time.Measure);
            
            if (i > 0 && (notedata[i].whitenote.Dist(notedata[i-1].whitenote) == 1)) {
                /* This note (notedata[i]) overlaps with the previous note.
                 * Change the side of this note.
                 */

                if (notedata[i-1].leftside) {
                    notedata[i].leftside = false;
                } else {
                    notedata[i].leftside = true;
                }
            } else {
                notedata[i].leftside = true;
            }
        }
        return notedata;
    }


    /** Given the note data (the white keys and accidentals), create 
     * the Accidental Symbols and return them.
     */
    private static AccidSymbol[] 
    CreateAccidSymbols(NoteData[] notedata, Clef clef) {
        int count = 0;
        foreach (NoteData n in notedata) {
            if (n.accid != Accid.None) {
                count++;
            }
        }
        AccidSymbol[] symbols = new AccidSymbol[count];
        int i = 0;
        foreach (NoteData n in notedata) {
            if (n.accid != Accid.None) {
                symbols[i] = new AccidSymbol(n.accid, n.whitenote, clef);
                i++;
            }
        }
        return symbols;
    }

    /** Calculate the stem direction (Up or down) based on the top and
     * bottom note in the chord.  If the average of the notes is above
     * the middle of the staff, the direction is down.  Else, the
     * direction is up.
     */
    private static int 
    StemDirection(WhiteNote bottom, WhiteNote top, Clef clef) {
        WhiteNote middle;
        if (clef == Clef.Treble)
            middle = new WhiteNote(WhiteNote.B, 5);
        else
            middle = new WhiteNote(WhiteNote.D, 3);

        int dist = middle.Dist(bottom) + middle.Dist(top);
        if (dist >= 0)
            return Stem.Up;
        else 
            return Stem.Down;
    }

    /** Return whether any of the notes in notedata (between start and
     * end indexes) overlap.  This is needed by the Stem class to
     * determine the position of the stem (left or right of notes).
     */
    private static bool NotesOverlap(NoteData[] notedata, int start, int end) {
        for (int i = start; i < end; i++) {
            if (!notedata[i].leftside) {
                return true;
            }
        }
        return false;
    }

    /** Get the time (in pulses) this symbol occurs at.
     * This is used to determine the measure this symbol belongs to.
     */
    public override int StartTime { 
        get { return starttime; }
    }

    /** Get the end time (in pulses) of the longest note in the chord.
     * Used to determine whether two adjacent chords can be joined
     * by a stem.
     */
    public int EndTime { 
        get { return endtime; }
    }

    /** Return the clef this chord is drawn in. */
    public Clef Clef { 
        get { return clef; }
    }

    /** Return true if this chord has two stems */
    public bool HasTwoStems {
        get { return hastwostems; }
    }

    /* Return the stem will the smallest duration.  This property
     * is used when making chord pairs (chords joined by a horizontal
     * beam stem). The stem durations must match in order to make
     * a chord pair.  If a chord has two stems, we always return
     * the one with a smaller duration, because it has a better 
     * chance of making a pair.
     */
    public Stem Stem {
        get { 
            if (stem1 == null) { return stem2; }
            else if (stem2 == null) { return stem1; }
            else if (stem1.Duration < stem2.Duration) { return stem1; }
            else { return stem2; }
        }
    }

    /** Get/Set the width (in pixels) of this symbol. The width is set
     * in SheetMusic.AlignSymbols() to vertically align symbols.
     */
    public override int Width {
        get { return width; }
        set { width = value; }
    }

    /** Get the minimum width (in pixels) needed to draw this symbol */
    public override int MinWidth {
        get { return GetMinWidth(); }
    }

    /* Return the minimum width needed to display this chord.
     *
     * The accidental symbols can be drawn above one another as long
     * as they don't overlap (they must be at least 6 notes apart).
     * If two accidental symbols do overlap, the accidental symbol
     * on top must be shifted to the right.  So the width needed for
     * accidental symbols depends on whether they overlap or not.
     *
     * If we are also displaying the letters, include extra width.
     */
    int GetMinWidth() {
        /* The width needed for the note circles */
        int result = 2*SheetMusic.NoteHeight + SheetMusic.NoteHeight*3/4;

        if (accidsymbols.Length > 0) {
            result += accidsymbols[0].MinWidth;
            for (int i = 1; i < accidsymbols.Length; i++) {
                AccidSymbol accid = accidsymbols[i];
                AccidSymbol prev = accidsymbols[i-1];
                if (accid.Note.Dist(prev.Note) < 6) {
                    result += accid.MinWidth;
                }
            }
        }
        if (sheetmusic != null && sheetmusic.ShowNoteLetters != MidiOptions.NoteNameNone) {
            result += 8;
        }
        return result;
    }


    /** Get the number of pixels this symbol extends above the staff. Used
     *  to determine the minimum height needed for the staff (Staff.FindBounds).
     */
    public override int AboveStaff {
        get { return GetAboveStaff(); }
    }

    private int GetAboveStaff() {
        /* Find the topmost note in the chord */
        WhiteNote topnote = notedata[ notedata.Length-1 ].whitenote;

        /* The stem.End is the note position where the stem ends.
         * Check if the stem end is higher than the top note.
         */
        if (stem1 != null)
            topnote = WhiteNote.Max(topnote, stem1.End);
        if (stem2 != null)
            topnote = WhiteNote.Max(topnote, stem2.End);

        int dist = topnote.Dist(WhiteNote.Top(clef)) * SheetMusic.NoteHeight/2;
        int result = 0;
        if (dist > 0)
            result = dist;

        /* Check if any accidental symbols extend above the staff */
        foreach (AccidSymbol symbol in accidsymbols) {
            if (symbol.AboveStaff > result) {
                result = symbol.AboveStaff;
            }
        }
        return result;
    }

    /** Get the number of pixels this symbol extends below the staff. Used
     *  to determine the minimum height needed for the staff (Staff.FindBounds).
     */
    public override int BelowStaff {
        get { return GetBelowStaff(); }
    }

    private int GetBelowStaff() {
        /* Find the bottom note in the chord */
        WhiteNote bottomnote = notedata[0].whitenote;

        /* The stem.End is the note position where the stem ends.
         * Check if the stem end is lower than the bottom note.
         */
        if (stem1 != null)
            bottomnote = WhiteNote.Min(bottomnote, stem1.End);
        if (stem2 != null)
            bottomnote = WhiteNote.Min(bottomnote, stem2.End);

        int dist = WhiteNote.Bottom(clef).Dist(bottomnote) *
                   SheetMusic.NoteHeight/2;

        int result = 0;
        if (dist > 0)
            result = dist;

        /* Check if any accidental symbols extend below the staff */ 
        foreach (AccidSymbol symbol in accidsymbols) {
            if (symbol.BelowStaff > result) {
                result = symbol.BelowStaff;
            }
        }
        return result;
    }

    /** Get the name for this note */
    private string NoteName(int notenumber, WhiteNote whitenote) {
        if (sheetmusic.ShowNoteLetters == MidiOptions.NoteNameLetter) {
            return Letter(notenumber, whitenote);
        }
        else if (sheetmusic.ShowNoteLetters == MidiOptions.NoteNameFixedDoReMi) {
            string[] fixedDoReMi = {
                "La", "Li", "Ti", "Do", "Di", "Re", "Ri", "Mi", "Fa", "Fi", "So", "Si" 
            };
            int notescale = NoteScale.FromNumber(notenumber);
            return fixedDoReMi[notescale];
        }
        else if (sheetmusic.ShowNoteLetters == MidiOptions.NoteNameMovableDoReMi) {
            string[] fixedDoReMi = {
                "La", "Li", "Ti", "Do", "Di", "Re", "Ri", "Mi", "Fa", "Fi", "So", "Si" 
            };
            int mainscale = sheetmusic.MainKey.Notescale();
            int diff = NoteScale.C - mainscale;
            notenumber += diff;
            if (notenumber < 0) {
                notenumber += 12;
            }
            int notescale = NoteScale.FromNumber(notenumber);
            return fixedDoReMi[notescale];
        }
        else if (sheetmusic.ShowNoteLetters == MidiOptions.NoteNameFixedNumber) {
            string[] num = {
                "10", "11", "12", "1", "2", "3", "4", "5", "6", "7", "8", "9" 
            };
            int notescale = NoteScale.FromNumber(notenumber);
            return num[notescale];
        }
        else if (sheetmusic.ShowNoteLetters == MidiOptions.NoteNameMovableNumber) {
            string[] num = {
                "10", "11", "12", "1", "2", "3", "4", "5", "6", "7", "8", "9" 
            };
            int mainscale = sheetmusic.MainKey.Notescale();
            int diff = NoteScale.C - mainscale;
            notenumber += diff;
            if (notenumber < 0) {
                notenumber += 12;
            }
            int notescale = NoteScale.FromNumber(notenumber);
            return num[notescale];
        }
        else {
            return "";
        }
    }

    /** Get the letter (A, A#, Bb) representing this note */
    private string Letter(int notenumber, WhiteNote whitenote) {
        int notescale = NoteScale.FromNumber(notenumber);
        switch(notescale) {
            case NoteScale.A: return "A";
            case NoteScale.B: return "B";
            case NoteScale.C: return "C";
            case NoteScale.D: return "D";
            case NoteScale.E: return "E";
            case NoteScale.F: return "F";
            case NoteScale.G: return "G";
            case NoteScale.Asharp:
                if (whitenote.Letter == WhiteNote.A)
                    return "A#";
                else
                    return "Bb";
            case NoteScale.Csharp:
                if (whitenote.Letter == WhiteNote.C)
                    return "C#";
                else
                    return "Db";
            case NoteScale.Dsharp:
                if (whitenote.Letter == WhiteNote.D)
                    return "D#";
                else
                    return "Eb";
            case NoteScale.Fsharp:
                if (whitenote.Letter == WhiteNote.F)
                    return "F#";
                else
                    return "Gb";
            case NoteScale.Gsharp:
                if (whitenote.Letter == WhiteNote.G)
                    return "G#";
                else
                    return "Ab";
            default:
                return "";
        }
    }

    /** Draw the Chord Symbol:
     * - Draw the accidental symbols.
     * - Draw the black circle notes.
     * - Draw the stems.
      @param ytop The ylocation (in pixels) where the top of the staff starts.
     */
    public override void Draw(Graphics g, Pen pen, int ytop) {
        /* Align the chord to the right */
        g.TranslateTransform(Width - MinWidth, 0);

        /* Draw the accidentals. */
        WhiteNote topstaff = WhiteNote.Top(clef);
        int xpos = DrawAccid(g, pen, ytop);

        /* Draw the notes */
        g.TranslateTransform(xpos, 0);
        DrawNotes(g, pen, ytop, topstaff);
        if (sheetmusic != null && sheetmusic.ShowNoteLetters != 0) {
            DrawNoteLetters(g, pen, ytop, topstaff);
        }

        /* Draw the stems */
        if (stem1 != null)
            stem1.Draw(g, pen, ytop, topstaff);
        if (stem2 != null)
            stem2.Draw(g, pen, ytop, topstaff);

        g.TranslateTransform(-xpos, 0);
        g.TranslateTransform(-(Width - MinWidth), 0);
    }

    /* Draw the accidental symbols.  If two symbols overlap (if they
     * are less than 6 notes apart), we cannot draw the symbol directly
     * above the previous one.  Instead, we must shift it to the right.
     * @param ytop The ylocation (in pixels) where the top of the staff starts.
     * @return The x pixel width used by all the accidentals.
     */
    public int DrawAccid(Graphics g, Pen pen, int ytop) {
        int xpos = 0;

        AccidSymbol prev = null;
        foreach (AccidSymbol symbol in accidsymbols) {
            if (prev != null && symbol.Note.Dist(prev.Note) < 6) {
                xpos += symbol.Width;
            }
            g.TranslateTransform(xpos, 0);
            symbol.Draw(g, pen, ytop);
            g.TranslateTransform(-xpos, 0);
            prev = symbol;
        }
        if (prev != null) {
            xpos += prev.Width;
        }
        return xpos;
    }

    /** Draw the black circle notes.
     * @param ytop The ylocation (in pixels) where the top of the staff starts.
     * @param topstaff The white note of the top of the staff.
     */
    public void DrawNotes(Graphics g, Pen pen, int ytop, WhiteNote topstaff) {
        pen.Width = 1;
        foreach (NoteData note in notedata) {
            /* Get the x,y position to draw the note */
            int ynote = ytop + topstaff.Dist(note.whitenote) * 
                        SheetMusic.NoteHeight/2;

            int xnote = SheetMusic.LineSpace/4;
            if (!note.leftside)
                xnote += SheetMusic.NoteWidth;

            /* Draw rotated ellipse.  You must first translate (0,0)
             * to the center of the ellipse.
             */