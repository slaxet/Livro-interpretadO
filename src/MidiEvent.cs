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

/** @class MidiEvent
 * A MidiEvent represents a single event (such as EventNoteOn) in the
 * Midi file. It includes the delta time of the event.
 */
public class MidiEvent : IComparer<MidiEvent> {

    public int    DeltaTime;     /** The time between the previous event and this on */
    public int    StartTime;     /** The absolute time this event occurs */
    public bool   HasEventflag;  /** False if this is using the previous eventflag */
    public byte   EventFlag;     /** NoteOn, NoteOff, etc.  Full list is in class MidiFile */
    public byte   Channel;  