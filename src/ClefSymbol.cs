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

/** The possible clefs, Treble or Bass */
public enum Clef { Treble, Bass };

/** @class ClefSymbol 
 * A ClefSymbol represents either a Treble or Bass Clef image.
 * The clef can be either normal or small size.  Normal size is
 * used at the beginning of a new staff, on the left side.  The
 * small symbols are used to show clef changes within a staff.
 */

public class ClefSymbol : MusicSym