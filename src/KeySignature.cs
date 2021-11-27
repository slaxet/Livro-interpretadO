
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

namespace MidiSheetMusic {


/** @class KeySignature
 * The KeySignature class represents a key signature, like G Major
 * or B-flat Major.  For sheet music, we only care about the number
 * of sharps or flats in the key signature, not whether it is major
 * or minor.
 *
 * The main operations of this class are:
 * - Guessing the key signature, given the notes in a song.
 * - Generating the accidental symbols for the key signature.
 * - Determining whether a particular note requires an accidental
 *   or not.
 *
 */

public class KeySignature {
    /** The number of sharps in each key signature */
    public const int C = 0;
    public const int G = 1;
    public const int D = 2;
    public const int A = 3;
    public const int E = 4;
    public const int B = 5;

    /** The number of flats in each key signature */
    public const int F = 1;
    public const int Bflat = 2;
    public const int Eflat = 3;
    public const int Aflat = 4;
    public const int Dflat = 5;
    public const int Gflat = 6;

    /** The two arrays below are key maps.  They take a major key
     * (like G major, B-flat major) and a note in the scale, and
     * return the Accidental required to display that note in the
     * given key.  In a nutshel, the map is
     *
     *   map[Key][NoteScale] -> Accidental
     */
    private static Accid[][] sharpkeys;
    private static Accid[][] flatkeys;

    private int num_flats;   /** The number of sharps in the key, 0 thru 6 */
    private int num_sharps;  /** The number of flats in the key, 0 thru 6 */

    /** The accidental symbols that denote this key, in a treble clef */
    private AccidSymbol[] treble;

    /** The accidental symbols that denote this key, in a bass clef */
    private AccidSymbol[] bass;

    /** The key map for this key signature:
     *   keymap[notenumber] -> Accidental
     */
    private Accid[] keymap;

    /** The measure used in the previous call to GetAccidental() */
    private int prevmeasure; 


    /** Create new key signature, with the given number of
     * sharps and flats.  One of the two must be 0, you can't
     * have both sharps and flats in the key signature.
     */
    public KeySignature(int num_sharps, int num_flats) {
        if (!(num_sharps == 0 || num_flats == 0)) {
            throw new System.ArgumentException("Bad KeySigature args");
        }
        this.num_sharps = num_sharps;
        this.num_flats = num_flats;

        CreateAccidentalMaps();
        keymap = new Accid[129];
        ResetKeyMap();
        CreateSymbols();
    }

    /** Create new key signature, with the given notescale.  */
    public KeySignature(int notescale) {
        num_sharps = num_flats = 0;
        switch (notescale) {
            case NoteScale.A:     num_sharps = 3; break;
            case NoteScale.Bflat: num_flats = 2;  break;
            case NoteScale.B:     num_sharps = 5; break;
            case NoteScale.C:     break;
            case NoteScale.Dflat: num_flats = 5;  break;
            case NoteScale.D:     num_sharps = 2; break;
            case NoteScale.Eflat: num_flats = 3;  break;
            case NoteScale.E:     num_sharps = 4; break;
            case NoteScale.F:     num_flats = 1;  break;
            case NoteScale.Gflat: num_flats = 6;  break;
            case NoteScale.G:     num_sharps = 1; break;
            case NoteScale.Aflat: num_flats = 4;  break;
            default:              break;
        }

        CreateAccidentalMaps();
        keymap = new Accid[129];
        ResetKeyMap();
        CreateSymbols();
    }


    /** Iniitalize the sharpkeys and flatkeys maps */
    private static void CreateAccidentalMaps() {
        if (sharpkeys != null)
            return; 

        Accid[] map;
        sharpkeys = new Accid[8][];
        flatkeys = new Accid[8][];

        for (int i = 0; i < 8; i++) {
            sharpkeys[i] = new Accid[12];
            flatkeys[i] = new Accid[12];
        }

        map = sharpkeys[C];
        map[ NoteScale.A ]      = Accid.None;
        map[ NoteScale.Asharp ] = Accid.Flat;
        map[ NoteScale.B ]      = Accid.None;
        map[ NoteScale.C ]      = Accid.None;
        map[ NoteScale.Csharp ] = Accid.Sharp;
        map[ NoteScale.D ]      = Accid.None;
        map[ NoteScale.Dsharp ] = Accid.Sharp;
        map[ NoteScale.E ]      = Accid.None;
        map[ NoteScale.F ]      = Accid.None;
        map[ NoteScale.Fsharp ] = Accid.Sharp;
        map[ NoteScale.G ]      = Accid.None;
        map[ NoteScale.Gsharp ] = Accid.Sharp;

        map = sharpkeys[G];
        map[ NoteScale.A ]      = Accid.None;
        map[ NoteScale.Asharp ] = Accid.Flat;
        map[ NoteScale.B ]      = Accid.None;
        map[ NoteScale.C ]      = Accid.None;
        map[ NoteScale.Csharp ] = Accid.Sharp;
        map[ NoteScale.D ]      = Accid.None;
        map[ NoteScale.Dsharp ] = Accid.Sharp;
        map[ NoteScale.E ]      = Accid.None;
        map[ NoteScale.F ]      = Accid.Natural;
        map[ NoteScale.Fsharp ] = Accid.None;
        map[ NoteScale.G ]      = Accid.None;
        map[ NoteScale.Gsharp ] = Accid.Sharp;

        map = sharpkeys[D];
        map[ NoteScale.A ]      = Accid.None;
        map[ NoteScale.Asharp ] = Accid.Flat;
        map[ NoteScale.B ]      = Accid.None;
        map[ NoteScale.C ]      = Accid.Natural;
        map[ NoteScale.Csharp ] = Accid.None;
        map[ NoteScale.D ]      = Accid.None;
        map[ NoteScale.Dsharp ] = Accid.Sharp;
        map[ NoteScale.E ]      = Accid.None;
        map[ NoteScale.F ]      = Accid.Natural;
        map[ NoteScale.Fsharp ] = Accid.None;
        map[ NoteScale.G ]      = Accid.None;
        map[ NoteScale.Gsharp ] = Accid.Sharp;

        map = sharpkeys[A];
        map[ NoteScale.A ]      = Accid.None;
        map[ NoteScale.Asharp ] = Accid.Flat;
        map[ NoteScale.B ]      = Accid.None;
        map[ NoteScale.C ]      = Accid.Natural;
        map[ NoteScale.Csharp ] = Accid.None;
        map[ NoteScale.D ]      = Accid.None;
        map[ NoteScale.Dsharp ] = Accid.Sharp;
        map[ NoteScale.E ]      = Accid.None;
        map[ NoteScale.F ]      = Accid.Natural;
        map[ NoteScale.Fsharp ] = Accid.None;
        map[ NoteScale.G ]      = Accid.Natural;
        map[ NoteScale.Gsharp ] = Accid.None;

        map = sharpkeys[E];
        map[ NoteScale.A ]      = Accid.None;
        map[ NoteScale.Asharp ] = Accid.Flat;
        map[ NoteScale.B ]      = Accid.None;
        map[ NoteScale.C ]      = Accid.Natural;
        map[ NoteScale.Csharp ] = Accid.None;
        map[ NoteScale.D ]      = Accid.Natural;
        map[ NoteScale.Dsharp ] = Accid.None;
        map[ NoteScale.E ]      = Accid.None;
        map[ NoteScale.F ]      = Accid.Natural;
        map[ NoteScale.Fsharp ] = Accid.None;
        map[ NoteScale.G ]      = Accid.Natural;
        map[ NoteScale.Gsharp ] = Accid.None;

        map = sharpkeys[B];
        map[ NoteScale.A ]      = Accid.Natural;
        map[ NoteScale.Asharp ] = Accid.None;
        map[ NoteScale.B ]      = Accid.None;
        map[ NoteScale.C ]      = Accid.Natural;
        map[ NoteScale.Csharp ] = Accid.None;
        map[ NoteScale.D ]      = Accid.Natural;
        map[ NoteScale.Dsharp ] = Accid.None;
        map[ NoteScale.E ]      = Accid.None;
        map[ NoteScale.F ]      = Accid.Natural;
        map[ NoteScale.Fsharp ] = Accid.None;
        map[ NoteScale.G ]      = Accid.Natural;
        map[ NoteScale.Gsharp ] = Accid.None;

        /* Flat keys */
        map = flatkeys[C];
        map[ NoteScale.A ]      = Accid.None;
        map[ NoteScale.Asharp ] = Accid.Flat;
        map[ NoteScale.B ]      = Accid.None;
        map[ NoteScale.C ]      = Accid.None;
        map[ NoteScale.Csharp ] = Accid.Sharp;
        map[ NoteScale.D ]      = Accid.None;
        map[ NoteScale.Dsharp ] = Accid.Sharp;
        map[ NoteScale.E ]      = Accid.None;
        map[ NoteScale.F ]      = Accid.None;
        map[ NoteScale.Fsharp ] = Accid.Sharp;
        map[ NoteScale.G ]      = Accid.None;
        map[ NoteScale.Gsharp ] = Accid.Sharp;

        map = flatkeys[F];
        map[ NoteScale.A ]      = Accid.None;
        map[ NoteScale.Bflat ]  = Accid.None;
        map[ NoteScale.B ]      = Accid.Natural;
        map[ NoteScale.C ]      = Accid.None;
        map[ NoteScale.Csharp ] = Accid.Sharp;
        map[ NoteScale.D ]      = Accid.None;
        map[ NoteScale.Eflat ]  = Accid.Flat;
        map[ NoteScale.E ]      = Accid.None;
        map[ NoteScale.F ]      = Accid.None;
        map[ NoteScale.Fsharp ] = Accid.Sharp;
        map[ NoteScale.G ]      = Accid.None;
        map[ NoteScale.Aflat ]  = Accid.Flat;

        map = flatkeys[Bflat];
        map[ NoteScale.A ]      = Accid.None;
        map[ NoteScale.Bflat ]  = Accid.None;
        map[ NoteScale.B ]      = Accid.Natural;
        map[ NoteScale.C ]      = Accid.None;
        map[ NoteScale.Csharp ] = Accid.Sharp;
        map[ NoteScale.D ]      = Accid.None;
        map[ NoteScale.Eflat ]  = Accid.None;
        map[ NoteScale.E ]      = Accid.Natural;
        map[ NoteScale.F ]      = Accid.None;
        map[ NoteScale.Fsharp ] = Accid.Sharp;
        map[ NoteScale.G ]      = Accid.None;
        map[ NoteScale.Aflat ]  = Accid.Flat;

        map = flatkeys[Eflat];
        map[ NoteScale.A ]      = Accid.Natural;
        map[ NoteScale.Bflat ]  = Accid.None;
        map[ NoteScale.B ]      = Accid.Natural;
        map[ NoteScale.C ]      = Accid.None;
        map[ NoteScale.Dflat ]  = Accid.Flat;
        map[ NoteScale.D ]      = Accid.None;
        map[ NoteScale.Eflat ]  = Accid.None;
        map[ NoteScale.E ]      = Accid.Natural;
        map[ NoteScale.F ]      = Accid.None;
        map[ NoteScale.Fsharp ] = Accid.Sharp;
        map[ NoteScale.G ]      = Accid.None;
        map[ NoteScale.Aflat ]  = Accid.None;

        map = flatkeys[Aflat];
        map[ NoteScale.A ]      = Accid.Natural;
        map[ NoteScale.Bflat ]  = Accid.None;
        map[ NoteScale.B ]      = Accid.Natural;
        map[ NoteScale.C ]      = Accid.None;
        map[ NoteScale.Dflat ]  = Accid.None;
        map[ NoteScale.D ]      = Accid.Natural;
        map[ NoteScale.Eflat ]  = Accid.None;
        map[ NoteScale.E ]      = Accid.Natural;
        map[ NoteScale.F ]      = Accid.None;
        map[ NoteScale.Fsharp ] = Accid.Sharp;
        map[ NoteScale.G ]      = Accid.None;
        map[ NoteScale.Aflat ]  = Accid.None;

        map = flatkeys[Dflat];
        map[ NoteScale.A ]      = Accid.Natural;
        map[ NoteScale.Bflat ]  = Accid.None;
        map[ NoteScale.B ]      = Accid.Natural;
        map[ NoteScale.C ]      = Accid.None;
        map[ NoteScale.Dflat ]  = Accid.None;
        map[ NoteScale.D ]      = Accid.Natural;
        map[ NoteScale.Eflat ]  = Accid.None;
        map[ NoteScale.E ]      = Accid.Natural;
        map[ NoteScale.F ]      = Accid.None;
        map[ NoteScale.Gflat ]  = Accid.None;
        map[ NoteScale.G ]      = Accid.Natural;
        map[ NoteScale.Aflat ]  = Accid.None;

        map = flatkeys[Gflat];
        map[ NoteScale.A ]      = Accid.Natural;
        map[ NoteScale.Bflat ]  = Accid.None;
        map[ NoteScale.B ]      = Accid.None;
        map[ NoteScale.C ]      = Accid.Natural;
        map[ NoteScale.Dflat ]  = Accid.None;
        map[ NoteScale.D ]      = Accid.Natural;
        map[ NoteScale.Eflat ]  = Accid.None;
        map[ NoteScale.E ]      = Accid.Natural;
        map[ NoteScale.F ]      = Accid.None;
        map[ NoteScale.Gflat ]  = Accid.None;
        map[ NoteScale.G ]      = Accid.Natural;
        map[ NoteScale.Aflat ]  = Accid.None;


    }

    /** The keymap tells what accidental symbol is needed for each
     *  note in the scale.  Reset the keymap to the values of the
     *  key signature.
     */
    private void ResetKeyMap()
    {
        Accid[] key;
        if (num_flats > 0)
            key = flatkeys[num_flats];
        else
            key = sharpkeys[num_sharps];

        for (int notenumber = 0; notenumber < 128; notenumber++) {
            keymap[notenumber] = key[NoteScale.FromNumber(notenumber)];
        }
    }


    /** Create the Accidental symbols for this key, for
     * the treble and bass clefs.
     */
    private void CreateSymbols() {
        int count = Math.Max(num_sharps, num_flats);
        treble = new AccidSymbol[count];
        bass = new AccidSymbol[count];

        if (count == 0) {
            return;
        }

        WhiteNote[] treblenotes = null;
        WhiteNote[] bassnotes = null;

        if (num_sharps > 0)  {