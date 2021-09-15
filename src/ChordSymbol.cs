
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