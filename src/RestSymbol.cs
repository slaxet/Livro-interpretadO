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


/* @class RestSymbol
 * A Rest symbol represents a rest - whole, half, quarter, or eighth.
 * The Rest symbol has a starttime and a duration, just like a regular
 * note.
 */
public class RestSymbol : MusicSymbol {
    private int starttime;          /** The starttime of the rest */
    private NoteDuration duration;  /** The rest duration (eighth, quarter, half, whole) */
    private int width;              /** The width in pixels */

    /** Create a new rest symbol with the given start time and duration */
    public RestSymbol(int start, NoteDuration dur) {
        starttime = start;
        duration = dur; 
        width = MinWidth;
    }

    /** Get the time (in pulses) this symbol occurs at.
     * This is used to determine the measure this symbol belongs to.
     */
    public override int StartTime { 
        get { return starttime; }
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
        get { return 2 * SheetMusic.NoteHeight + 
              SheetMusic.NoteHeight/2;
        }
    }

    /** Get the number of pixels this symbol extends above the staff. Used
     *  to determine the minimum height needed for the staff (Staff.FindBounds).
     */
    public override int AboveStaff { 
        get { return 0; }
    }

    /** Get the number of pixels this symbol extends below the staff. Used
     *  to determine the minimum height needed for the staff (Staff.FindBounds).
     */
    public override int BelowStaff { 
        get { return 0; }
    }

    /** Draw the symbol.
     * @param ytop The ylocation (in pixels) where the top of the staff starts.
     */
    public override 
    void Draw(Graphics g, Pen pen, int ytop) {
        /* Align the rest symbol to the right */
        g.TranslateTransform(Width - MinWidth, 0);
        g.TranslateTransform(SheetMusic.NoteHeight/2, 0);

        if (duration == NoteDuration.Whole) {
            DrawWhole(g, pen, ytop);
        }
        else if (duration == NoteDuration.Half) {
            DrawHalf(g, pen, ytop);
        }
        else if (duration == NoteDuration.Quarter) {
            DrawQuarter(g, pen, ytop);
        }
        else if (duration == NoteDuration.Eighth) {
            DrawEighth(g, pen, ytop);
        }
        g.TranslateTransform(-SheetMusic.NoteHeight/2, 0);
        g.TranslateTransform(-(Width - MinWidth), 0);
    }


    /** Draw a whole rest symbol, a rectangle below a staff line.
     * @param ytop The ylocation (in pixels) where the top of the staff starts.
     */
    public void DrawWhole(Graphics g, Pen pen, int ytop) {
        int y = ytop + SheetMusic.NoteHeight;

        g.FillRectangle(Brushes.Black, 0, y, 
                        SheetMusic.NoteWidth, SheetMusic.NoteHeight/2);
    }

   