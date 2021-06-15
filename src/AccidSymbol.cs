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
    public override 