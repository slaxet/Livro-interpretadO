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
    private int width;              /** The width in pix