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


/** Accidentals */
public enum Accid {
    None, Sharp, Flat, Natural
}

/** @class AccidSymbol
 * An accidental (accid) symbol represents a sharp, flat, or natural
 * accidental that is displayed at a specific position (note and clef).
 */
public class AccidSymbol : MusicSymbol {
    private Accid accid;          /** The accidental (sharp, flat, natural) */
    private WhiteNote whitenote;  /** The white note where the symbol occurs */
    private Clef clef;            /** Which clef the symbols is in */
    private int width;            /** Width of symbol */

    /** 
     * Create a new AccidSymbol with the given accidental, that is
     * displayed at the given note in the given clef.
     */
    public AccidSymbol(Accid accid, WhiteNote note, Clef clef) {
        this.accid = accid;
        this.whitenote = note;
        this.clef = clef;
        width = MinWidth;
    }

    /** Return the white note this accidental is displayed at */
    public WhiteNote Note  {
        get { return whitenote; }
    }

    /** Get the time (in pulses) this symbol occurs at.
     * Not used.  Instead, the StartTime of the ChordSymbol containing this
     * AccidSymbol is used.
     */
    public override int StartTime { 
        get { return -1; }  
    }

    /** Get the minimum width (in pixels) needed to draw this symbol */
    public override int MinWidth { 
        get { return 3*SheetMusic.NoteHeight/2; }
    }

    /** Get/Set the width (in pixels) of this symbol. The width is set
     * in SheetMusic.AlignSymbols() to vertically align symbols.
     */
    public override int Width {
        get { return width; }
        set { width = value; }
    }

    /** Get the number of pixels this symbol extends above the staff. Used
     *  to determine the minimum height needed for the staff (Staff.FindBounds).
     */
    public override int AboveStaff {
        get { return GetAboveStaff(); }
    }

    int GetAboveStaff() {
        int dist = WhiteNote.Top(clef).Dist(whitenote) * 
                   SheetMusic.NoteHeight/2;
        if (accid == Accid.Sharp || accid == Accid.Natural)
            dist -= SheetMusic.NoteHeight;
        else if (accid == Accid.Flat)
            dist -= 3*SheetMusic.NoteHeight/2;

        if (dist < 0)
            return -dist;
        else
            return 0;
    }

    /** Get the number of pixels this symbol extends below the staff. Used
     *  to determine the minimum height needed for the staff (Staff.FindBounds).
     */
    public override int BelowStaff {
        get { return GetBelowStaff(); }
    }

    private int GetBelowStaff() {
        int dist = WhiteNote.Bottom(clef).Dist(whitenote) * 
                   SheetMusic.NoteHeight/2 + 
                   SheetMusic.NoteHeight;
        if (accid == Accid.Sharp || accid == Accid.Natural) 
            dist += SheetMusic.NoteHeight;

        if (dist > 0)
            return dist;
        else 
            return 0;
    }

    /** Draw the symbol.
     * @param ytop The ylocation (in pixels) where the top of the staff starts.
     */
    public override void Draw(Graphics g, Pen pen, int ytop) {
        /* Align the symbol to the right */
        g.TranslateTransform(Width - MinWidth, 0);

        /* Store the y-pixel value of the top of the whitenote in ynote. */
        int ynote = ytop + WhiteNote.Top(clef).Dist(whitenote) * 
                    SheetMusic.NoteHeight/2;

        if (accid == Accid.Sharp)
            DrawSharp(g, pen, ynote);
        else if (accid == Accid.Flat)
            DrawFlat(g, pen, ynote);
        else if (accid == Accid.Natural)
            DrawNatural(g, pen, ynote);

        g.TranslateTransform(-(Width - MinWidth), 0);
    }

    /** Draw a sharp symbol. 
     * @param ynote The pixel location of the top of the accidental's note. 
     */
    public void DrawSharp(Graphics g, Pen pen, int ynote) {

        /* Draw the two vertical lines */
        int ystart = ynote - SheetMusic.NoteHeight;
        int yend = ynote