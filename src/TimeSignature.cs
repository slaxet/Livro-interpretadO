
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

namespace MidiSheetMusic {

/** The possible note durations */
public enum NoteDuration {
  ThirtySecond, Sixteenth, Triplet, Eighth,
  DottedEighth, Quarter, DottedQuarter,
  Half, DottedHalf, Whole
};

/** @class TimeSignature
 * The TimeSignature class represents
 * - The time signature of the song, such as 4/4, 3/4, or 6/8 time, and
 * - The number of pulses per quarter note
 * - The number of microseconds per quarter note
 *
 * In midi files, all time is measured in "pulses".  Each note has
 * a start time (measured in pulses), and a duration (measured in 
 * pulses).  This class is used mainly to convert pulse durations
 * (like 120, 240, etc) into note durations (half, quarter, eighth, etc).
 */

public class TimeSignature {
    private int numerator;      /** Numerator of the time signature */
    private int denominator;    /** Denominator of the time signature */
    private int quarternote;    /** Number of pulses per quarter note */
    private int measure;        /** Number of pulses per measure */
    private int tempo;          /** Number of microseconds per quarter note */

    /** Get the numerator of the time signature */
    public int Numerator {
        get { return numerator; }
    }

    /** Get the denominator of the time signature */
    public int Denominator {
        get { return denominator; }
    }