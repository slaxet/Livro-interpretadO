
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

/* This file contains the classes for parsing and modifying
 * MIDI music files.
 */

/* MIDI file format.
 *
 * The Midi File format is described below.  The description uses
 * the following abbreviations.
 *
 * u1     - One byte
 * u2     - Two bytes (big endian)
 * u4     - Four bytes (big endian)
 * varlen - A variable length integer, that can be 1 to 4 bytes. The 
 *          integer ends when you encounter a byte that doesn't have 
 *          the 8th bit set (a byte less than 0x80).
 * len?   - The length of the data depends on some code
 *          
 *
 * The Midi files begins with the main Midi header
 * u4 = The four ascii characters 'MThd'
 * u4 = The length of the MThd header = 6 bytes
 * u2 = 0 if the file contains a single track
 *      1 if the file contains one or more simultaneous tracks
 *      2 if the file contains one or more independent tracks
 * u2 = number of tracks
 * u2 = if >  0, the number of pulses per quarter note
 *      if <= 0, then ???
 *
 * Next come the individual Midi tracks.  The total number of Midi
 * tracks was given above, in the MThd header.  Each track starts
 * with a header:
 *
 * u4 = The four ascii characters 'MTrk'
 * u4 = Amount of track data, in bytes.
 * 
 * The track data consists of a series of Midi events.  Each Midi event
 * has the following format:
 *
 * varlen  - The time between the previous event and this event, measured
 *           in "pulses".  The number of pulses per quarter note is given
 *           in the MThd header.
 * u1      - The Event code, always betwee 0x80 and 0xFF
 * len?    - The event data.  The length of this data is determined by the
 *           event code.  The first byte of the event data is always < 0x80.
 *
 * The event code is optional.  If the event code is missing, then it
 * defaults to the previous event code.  For example:
 *
 *   varlen, eventcode1, eventdata,
 *   varlen, eventcode2, eventdata,
 *   varlen, eventdata,  // eventcode is eventcode2
 *   varlen, eventdata,  // eventcode is eventcode2
 *   varlen, eventcode3, eventdata,
 *   ....
 *
 *   How do you know if the eventcode is there or missing? Well:
 *   - All event codes are between 0x80 and 0xFF
 *   - The first byte of eventdata is always less than 0x80.
 *   So, after the varlen delta time, if the next byte is between 0x80
 *   and 0xFF, its an event code.  Otherwise, its event data.
 *
 * The Event codes and event data for each event code are shown below.
 *
 * Code:  u1 - 0x80 thru 0x8F - Note Off event.
 *             0x80 is for channel 1, 0x8F is for channel 16.
 * Data:  u1 - The note number, 0-127.  Middle C is 60 (0x3C)
 *        u1 - The note velocity.  This should be 0
 * 
 * Code:  u1 - 0x90 thru 0x9F - Note On event.
 *             0x90 is for channel 1, 0x9F is for channel 16.
 * Data:  u1 - The note number, 0-127.  Middle C is 60 (0x3C)
 *        u1 - The note velocity, from 0 (no sound) to 127 (loud).
 *             A value of 0 is equivalent to a Note Off.