/*
 * Copyright (c) 2007-2012 Madhav Vaidyanathan
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


namespace MidiSheetMusic {

/** @class LyricSymbol
 *  A lyric contains the lyric to display, the start time the lyric occurs at,
 *  the the x-coordinate where it will be displayed.
 */
public class LyricSymbol {
    private int starttime;   /** The start time, in pulses */
    private string text;     /** The lyric text */
    private int x;           /** The x (horizontal) position within the staff */

    public LyricSym