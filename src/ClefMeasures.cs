/*
 * Copyright (c) 2007-2008 Madhav Vaidyanathan
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

/** @class ClefMeasures
 * The ClefMeasures class is used to report what Clef (Treble or Bass) a
 * given measure uses.
 */
public class ClefMeasures {
    private List<Clef> clefs;  /** The clefs used for each measure (for a single track) */
    private int measure;       /** The length of a measure, in pulses */

 
    /** Given the notes in a track, calculate the appropriate Clef to use
     * for each measure.  Store the result in the clefs list.
     * @param notes  The midi notes
     * @param measurelen The length of a measure, in pulses
     */
    public ClefMeasures(List<MidiNote> notes, int measurelen) {
        measure = measurelen;
        Clef mainclef = MainClef(notes);
        int nextmeasure = measurelen;
        int pos = 0;
        Clef clef = mainclef;

        clefs = new List<Clef>();

        while (pos < notes.Count) {
            /* Sum all the notes in the current measure */
            int sumnotes = 0;
            int notecount = 0;
            while (pos < notes.Count && notes[pos].StartTime < nextmeasure) {
                sumnotes += notes[pos].Number;
                notecount++;
                pos++;
            }
            if (notecount == 0)
                notecount = 1;

            /* Calculate the "average" note in t