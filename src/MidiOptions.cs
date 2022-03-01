
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
using System.Drawing;

namespace MidiSheetMusic {

/** @class MidiOptions
 *
 * The MidiOptions class contains the available options for
 * modifying the sheet music and sound.  These options are
 * collected from the menu/dialog settings, and then are passed
 * to the SheetMusic and MidiPlayer classes.
 */
public class MidiOptions {

    // The possible values for showNoteLetters
    public const int NoteNameNone           = 0;
    public const int NoteNameLetter         = 1;
    public const int NoteNameFixedDoReMi    = 2;
    public const int NoteNameMovableDoReMi  = 3;
    public const int NoteNameFixedNumber    = 4;
    public const int NoteNameMovableNumber  = 5;