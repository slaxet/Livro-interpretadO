
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
using System.Collections.Generic;

namespace MidiSheetMusic {

/** Enumeration of the notes in a scale (A, A#, ... G#) */
public class NoteScale {
    public const int A      = 0;
    public const int Asharp = 1;
    public const int Bflat  = 1;
    public const int B      = 2;
    public const int C      = 3;
    public const int Csharp = 4;
    public const int Dflat  = 4;
    public const int D      = 5;
    public const int Dsharp = 6;
    public const int Eflat  = 6;
    public const int E      = 7;
    public const int F      = 8;
    public const int Fsharp = 9;
    public const int Gflat  = 9;
    public const int G      = 10;
    public const int Gsharp = 11;
    public const int Aflat  = 11;

    /** Convert a note (A, A#, B, etc) and octave into a
     * Midi Note number.
     */
    public static int ToNumber(int notescale, int octave) {
        return 9 + notescale + octave * 12;
    }

    /** Convert a Midi note number into a notescale (A, A#, B) */
    public static int FromNumber(int number) {
        return (number + 3) % 12;
    }

    /** Return true if this notescale number is a black key */
    public static bool IsBlackKey(int notescale) {
        if (notescale == Asharp ||
            notescale == Csharp ||
            notescale == Dsharp ||
            notescale == Fsharp ||
            notescale == Gsharp) {

            return true;
        }
        else {
            return false;
        }
    }
}


/** @class WhiteNote
 * The WhiteNote class represents a white key note, a non-sharp,
 * non-flat note.  To display midi notes as sheet music, the notes
 * must be converted to white notes and accidentals. 
 *
 * White notes consist of a letter (A thru G) and an octave (0 thru 10).
 * The octave changes from G to A.  After G2 comes A3.  Middle-C is C4.
 *
 * The main operations are calculating distances between notes, and comparing notes.
 */ 

public class WhiteNote : IComparer<WhiteNote> {

    /* The possible note letters */
    public const int A = 0;
    public const int B = 1;
    public const int C = 2;
    public const int D = 3;
    public const int E = 4;
    public const int F = 5;
    public const int G = 6;

    /* Common white notes used in calculations */
    public static WhiteNote TopTreble = new WhiteNote(WhiteNote.E, 5);
    public static WhiteNote BottomTreble = new WhiteNote(WhiteNote.F, 4);