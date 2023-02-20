
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

/** @class Stem
 * The Stem class is used by ChordSymbol to draw the stem portion of
 * the chord.  The stem has the following fields:
 *
 * duration  - The duration of the stem.
 * direction - Either Up or Down
 * side      - Either left or right
 * top       - The topmost note in the chord
 * bottom    - The bottommost note in the chord
 * end       - The note position where the stem ends.  This is usually
 *             six notes past the last note in the chord.  For 8th/16th
 *             notes, the stem must extend even more.
 *
 * The SheetMusic class can change the direction of a stem after it
 * has been created.  The side and end fields may also change due to
 * the direction change.  But other fields will not change.
 */
 
public class Stem {
    public const int Up =   1;      /* The stem points up */
    public const int Down = 2;      /* The stem points down */
    public const int LeftSide = 1;  /* The stem is to the left of the note */
    public const int RightSide = 2; /* The stem is to the right of the note */

    private NoteDuration duration; /** Duration of the stem. */
    private int direction;         /** Up, Down, or None */
    private WhiteNote top;         /** Topmost note in chord */
    private WhiteNote bottom;      /** Bottommost note in chord */
    private WhiteNote end;         /** Location of end of the stem */
    private bool notesoverlap;     /** Do the chord notes overlap */
    private int side;              /** Left side or right side of note */

    private Stem pair;              /** If pair != null, this is a horizontal 
                                     * beam stem to another chord */
    private int width_to_pair;      /** The width (in pixels) to the chord pair */
    private bool receiver_in_pair;  /** This stem is the receiver of a horizontal
                                    * beam stem from another chord. */

    /** Get/Set the direction of the stem (Up or Down) */
    public int Direction {
        get { return direction; }
        set { ChangeDirection(value); }
    }

    /** Get the duration of the stem (Eigth, Sixteenth, ThirtySecond) */
    public NoteDuration Duration {
        get { return duration; }
    }

    /** Get the top note in the chord. This is needed to determine the stem direction */
    public WhiteNote Top {
        get { return top; }
    }

    /** Get the bottom note in the chord. This is needed to determine the stem direction */
    public WhiteNote Bottom {
        get { return bottom; }
    }

    /** Get/Set the location where the stem ends.  This is usually six notes
     * past the last note in the chord. See method CalculateEnd.
     */
    public WhiteNote End {
        get { return end; }
        set { end = value; }
    }

    /** Set this Stem to be the receiver of a horizontal beam, as part
     * of a chord pair.  In Draw(), if this stem is a receiver, we
     * don't draw a curvy stem, we only draw the vertical line.
     */
    public bool Receiver {
        get { return receiver_in_pair; }
        set { receiver_in_pair = value; }
    }

    /** Create a new stem.  The top note, bottom note, and direction are 
     * needed for drawing the vertical line of the stem.  The duration is 
     * needed to draw the tail of the stem.  The overlap boolean is true
     * if the notes in the chord overlap.  If the notes overlap, the
     * stem must be drawn on the right side.