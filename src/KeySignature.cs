
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

namespace MidiSheetMusic {


/** @class KeySignature
 * The KeySignature class represents a key signature, like G Major
 * or B-flat Major.  For sheet music, we only care about the number
 * of sharps or flats in the key signature, not whether it is major
 * or minor.
 *
 * The main operations of this class are:
 * - Guessing the key signature, given the notes in a song.
 * - Generating the accidental symbols for the key signature.
 * - Determining whether a particular note requires an accidental
 *   or not.
 *
 */

public class KeySignature {
    /** The number of sharps in each key signature */