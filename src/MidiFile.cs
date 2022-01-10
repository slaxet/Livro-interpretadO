
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
 *
 * Code:  u1 - 0xA0 thru 0xAF - Key Pressure
 * Data:  u1 - The note number, 0-127.
 *        u1 - The pressure.
 *
 * Code:  u1 - 0xB0 thru 0xBF - Control Change
 * Data:  u1 - The controller number
 *        u1 - The value
 *
 * Code:  u1 - 0xC0 thru 0xCF - Program Change
 * Data:  u1 - The program number.
 *
 * Code:  u1 - 0xD0 thru 0xDF - Channel Pressure
 *        u1 - The pressure.
 *
 * Code:  u1 - 0xE0 thru 0xEF - Pitch Bend
 * Data:  u2 - Some data
 *
 * Code:  u1     - 0xFF - Meta Event
 * Data:  u1     - Metacode
 *        varlen - Length of meta event
 *        u1[varlen] - Meta event data.
 *
 *
 * The Meta Event codes are listed below:
 *
 * Metacode: u1         - 0x0  Sequence Number
 *           varlen     - 0 or 2
 *           u1[varlen] - Sequence number
 *
 * Metacode: u1         - 0x1  Text
 *           varlen     - Length of text
 *           u1[varlen] - Text
 *
 * Metacode: u1         - 0x2  Copyright
 *           varlen     - Length of text
 *           u1[varlen] - Text
 *
 * Metacode: u1         - 0x3  Track Name
 *           varlen     - Length of name
 *           u1[varlen] - Track Name
 *
 * Metacode: u1         - 0x58  Time Signature
 *           varlen     - 4 
 *           u1         - numerator
 *           u1         - log2(denominator)
 *           u1         - clocks in metronome click
 *           u1         - 32nd notes in quarter note (usually 8)
 *
 * Metacode: u1         - 0x59  Key Signature
 *           varlen     - 2
 *           u1         - if >= 0, then number of sharps
 *                        if < 0, then number of flats * -1
 *           u1         - 0 if major key
 *                        1 if minor key
 *
 * Metacode: u1         - 0x51  Tempo
 *           varlen     - 3  
 *           u3         - quarter note length in microseconds
 */


/** @class MidiFile
 *
 * The MidiFile class contains the parsed data from the Midi File.
 * It contains:
 * - All the tracks in the midi file, including all MidiNotes per track.
 * - The time signature (e.g. 4/4, 3/4, 6/8)
 * - The number of pulses per quarter note.
 * - The tempo (number of microseconds per quarter note).
 *
 * The constructor takes a filename as input, and upon returning,
 * contains the parsed data from the midi file.
 *
 * The methods ReadTrack() and ReadMetaEvent() are helper functions called
 * by the constructor during the parsing.
 *
 * After the MidiFile is parsed and created, the user can retrieve the 
 * tracks and notes by using the property Tracks and Tracks.Notes.
 *
 * There are two methods for modifying the midi data based on the menu
 * options selected:
 *
 * - ChangeMidiNotes()
 *   Apply the menu options to the parsed MidiFile.  This uses the helper functions:
 *     SplitTrack()
 *     CombineToTwoTracks()
 *     ShiftTime()
 *     Transpose()
 *     RoundStartTimes()
 *     RoundDurations()
 *
 * - ChangeSound()
 *   Apply the menu options to the MIDI music data, and save the modified midi data 
 *   to a file, for playback. 
 *   
 */

public class MidiFile {
    private string filename;          /** The Midi file name */
    private List<MidiEvent>[] events; /** The raw MidiEvents, one list per track */
    private List<MidiTrack> tracks ;  /** The tracks of the midifile that have notes */
    private ushort trackmode;         /** 0 (single track), 1 (simultaneous tracks) 2 (independent tracks) */
    private TimeSignature timesig;    /** The time signature */
    private int quarternote;          /** The number of pulses per quarter note */
    private int totalpulses;          /** The total length of the song, in pulses */
    private bool trackPerChannel;     /** True if we've split each channel into a track */

    /* The list of Midi Events */
    public const int EventNoteOff         = 0x80;
    public const int EventNoteOn          = 0x90;
    public const int EventKeyPressure     = 0xA0;
    public const int EventControlChange   = 0xB0;
    public const int EventProgramChange   = 0xC0;
    public const int EventChannelPressure = 0xD0;
    public const int EventPitchBend       = 0xE0;
    public const int SysexEvent1          = 0xF0;
    public const int SysexEvent2          = 0xF7;
    public const int MetaEvent            = 0xFF;

    /* The list of Meta Events */
    public const int MetaEventSequence      = 0x0;
    public const int MetaEventText          = 0x1;
    public const int MetaEventCopyright     = 0x2;
    public const int MetaEventSequenceName  = 0x3;
    public const int MetaEventInstrument    = 0x4;
    public const int MetaEventLyric         = 0x5;
    public const int MetaEventMarker        = 0x6;
    public const int MetaEventEndOfTrack    = 0x2F;
    public const int MetaEventTempo         = 0x51;
    public const int MetaEventSMPTEOffset   = 0x54;
    public const int MetaEventTimeSignature = 0x58;
    public const int MetaEventKeySignature  = 0x59;

    /* The Program Change event gives the instrument that should
     * be used for a particular channel.  The following table
     * maps each instrument number (0 thru 128) to an instrument
     * name.
     */
    public static string[] Instruments = {

        "Acoustic Grand Piano",
        "Bright Acoustic Piano",
        "Electric Grand Piano",
        "Honky-tonk Piano",
        "Electric Piano 1",
        "Electric Piano 2",
        "Harpsichord",
        "Clavi",
        "Celesta",
        "Glockenspiel",
        "Music Box",
        "Vibraphone",
        "Marimba",
        "Xylophone",
        "Tubular Bells",
        "Dulcimer",
        "Drawbar Organ",
        "Percussive Organ",
        "Rock Organ",
        "Church Organ",
        "Reed Organ",
        "Accordion",
        "Harmonica",
        "Tango Accordion",
        "Acoustic Guitar (nylon)",
        "Acoustic Guitar (steel)",
        "Electric Guitar (jazz)",
        "Electric Guitar (clean)",
        "Electric Guitar (muted)",
        "Overdriven Guitar",
        "Distortion Guitar",
        "Guitar harmonics",
        "Acoustic Bass",
        "Electric Bass (finger)",
        "Electric Bass (pick)",
        "Fretless Bass",
        "Slap Bass 1",
        "Slap Bass 2",
        "Synth Bass 1",
        "Synth Bass 2",
        "Violin",
        "Viola",
        "Cello",
        "Contrabass",
        "Tremolo Strings",
        "Pizzicato Strings",
        "Orchestral Harp",
        "Timpani",
        "String Ensemble 1",
        "String Ensemble 2",
        "SynthStrings 1",
        "SynthStrings 2",
        "Choir Aahs",
        "Voice Oohs",
        "Synth Voice",
        "Orchestra Hit",
        "Trumpet",
        "Trombone",
        "Tuba",
        "Muted Trumpet",
        "French Horn",
        "Brass Section",
        "SynthBrass 1",
        "SynthBrass 2",
        "Soprano Sax",
        "Alto Sax",
        "Tenor Sax",
        "Baritone Sax",
        "Oboe",
        "English Horn",
        "Bassoon",
        "Clarinet",
        "Piccolo",
        "Flute",
        "Recorder",
        "Pan Flute",
        "Blown Bottle",
        "Shakuhachi",
        "Whistle",
        "Ocarina",
        "Lead 1 (square)",
        "Lead 2 (sawtooth)",
        "Lead 3 (calliope)",
        "Lead 4 (chiff)",
        "Lead 5 (charang)",
        "Lead 6 (voice)",
        "Lead 7 (fifths)",
        "Lead 8 (bass + lead)",
        "Pad 1 (new age)",
        "Pad 2 (warm)",
        "Pad 3 (polysynth)",
        "Pad 4 (choir)",
        "Pad 5 (bowed)",
        "Pad 6 (metallic)",
        "Pad 7 (halo)",
        "Pad 8 (sweep)",
        "FX 1 (rain)",
        "FX 2 (soundtrack)",
        "FX 3 (crystal)",
        "FX 4 (atmosphere)",
        "FX 5 (brightness)",
        "FX 6 (goblins)",
        "FX 7 (echoes)",
        "FX 8 (sci-fi)",
        "Sitar",
        "Banjo",
        "Shamisen",
        "Koto",
        "Kalimba",
        "Bag pipe",
        "Fiddle",
        "Shanai",
        "Tinkle Bell",
        "Agogo",
        "Steel Drums",
        "Woodblock",
        "Taiko Drum",
        "Melodic Tom",
        "Synth Drum",
        "Reverse Cymbal",
        "Guitar Fret Noise",
        "Breath Noise",
        "Seashore",
        "Bird Tweet",
        "Telephone Ring",
        "Helicopter",
        "Applause",
        "Gunshot",
        "Percussion"
    };
    /* End Instruments */

    /** Return a string representation of a Midi event */
    private string EventName(int ev) {
        if (ev >= EventNoteOff && ev < EventNoteOff + 16)
            return "NoteOff";
        else if (ev >= EventNoteOn && ev < EventNoteOn + 16) 
            return "NoteOn";
        else if (ev >= EventKeyPressure && ev < EventKeyPressure + 16) 
            return "KeyPressure";
        else if (ev >= EventControlChange && ev < EventControlChange + 16) 
            return "ControlChange";
        else if (ev >= EventProgramChange && ev < EventProgramChange + 16) 
            return "ProgramChange";
        else if (ev >= EventChannelPressure && ev < EventChannelPressure + 16)
            return "ChannelPressure";
        else if (ev >= EventPitchBend && ev < EventPitchBend + 16)
            return "PitchBend";
        else if (ev == MetaEvent)
            return "MetaEvent";
        else if (ev == SysexEvent1 || ev == SysexEvent2)
            return "SysexEvent";
        else
            return "Unknown";
    }

    /** Return a string representation of a meta-event */
    private string MetaName(int ev) {
        if (ev == MetaEventSequence)
            return "MetaEventSequence";
        else if (ev == MetaEventText)
            return "MetaEventText";
        else if (ev == MetaEventCopyright)
            return "MetaEventCopyright";
        else if (ev == MetaEventSequenceName)
            return "MetaEventSequenceName";
        else if (ev == MetaEventInstrument)
            return "MetaEventInstrument";
        else if (ev == MetaEventLyric)
            return "MetaEventLyric";
        else if (ev == MetaEventMarker)
            return "MetaEventMarker";
        else if (ev == MetaEventEndOfTrack)
            return "MetaEventEndOfTrack";
        else if (ev == MetaEventTempo)
            return "MetaEventTempo";
        else if (ev == MetaEventSMPTEOffset)
            return "MetaEventSMPTEOffset";
        else if (ev == MetaEventTimeSignature)
            return "MetaEventTimeSignature";
        else if (ev == MetaEventKeySignature)
            return "MetaEventKeySignature";
        else
            return "Unknown";
    }


    /** Get the list of tracks */
    public List<MidiTrack> Tracks {
        get { return tracks; }
    }

    /** Get the time signature */
    public TimeSignature Time {
        get { return timesig; }
    }

    /** Get the file name */
    public string FileName {
        get { return filename; }
    }

    /** Get the total length (in pulses) of the song */
    public int TotalPulses {
        get { return totalpulses; }
    }


    /** Create a new MidiFile from the file. */
    public MidiFile(string filename) {
        MidiFileReader file = new MidiFileReader(filename);
        parse(file, filename);
    }

    /** Create a new MidiFile from the byte[]. */
    public MidiFile(byte[] data, string title) {
        MidiFileReader file = new MidiFileReader(data);
        if (title == null)
            title = "";
        parse(file, title);
    }

    /** Parse the given Midi file, and return an instance of this MidiFile
     * class.  After reading the midi file, this object will contain:
     * - The raw list of midi events
     * - The Time Signature of the song
     * - All the tracks in the song which contain notes. 
     * - The number, starttime, and duration of each note.
     */
    public void parse(MidiFileReader file, string filename) {
        string id;
        int len;

        this.filename = filename;
        tracks = new List<MidiTrack>();
        trackPerChannel = false;

        id = file.ReadAscii(4);
        if (id != "MThd") {
            throw new MidiFileException("Doesn't start with MThd", 0);
        }
        len = file.ReadInt(); 
        if (len !=  6) {
            throw new MidiFileException("Bad MThd header", 4);
        }
        trackmode = file.ReadShort();
        int num_tracks = file.ReadShort();
        quarternote = file.ReadShort(); 

        events = new List<MidiEvent>[num_tracks];
        for (int tracknum = 0; tracknum < num_tracks; tracknum++) {
            events[tracknum] = ReadTrack(file);
            MidiTrack track = new MidiTrack(events[tracknum], tracknum);
            if (track.Notes.Count > 0) {
                tracks.Add(track);
            }
        }

        /* Get the length of the song in pulses */
        foreach (MidiTrack track in tracks) {
            MidiNote last = track.Notes[track.Notes.Count-1];
            if (this.totalpulses < last.StartTime + last.Duration) {
                this.totalpulses = last.StartTime + last.Duration;
            }
        }

        /* If we only have one track with multiple channels, then treat
         * each channel as a separate track.
         */
        if (tracks.Count == 1 && HasMultipleChannels(tracks[0])) {
            tracks = SplitChannels(tracks[0], events[tracks[0].Number]);
            trackPerChannel = true;
        }

        CheckStartTimes(tracks);

        /* Determine the time signature */
        int tempo = 0;
        int numer = 0;
        int denom = 0;
        foreach (List<MidiEvent> list in events) {
            foreach (MidiEvent mevent in list) {
                if (mevent.Metaevent == MetaEventTempo && tempo == 0) {
                    tempo = mevent.Tempo;
                }
                if (mevent.Metaevent == MetaEventTimeSignature && numer == 0) {
                    numer = mevent.Numerator;
                    denom = mevent.Denominator;
                }
            }
        }
        if (tempo == 0) {
            tempo = 500000; /* 500,000 microseconds = 0.05 sec */
        }
        if (numer == 0) {
            numer = 4; denom = 4;
        }
        timesig = new TimeSignature(numer, denom, quarternote, tempo);
    }

    /** Parse a single Midi track into a list of MidiEvents.
     * Entering this function, the file offset should be at the start of
     * the MTrk header.  Upon exiting, the file offset should be at the
     * start of the next MTrk header.
     */
    private List<MidiEvent> ReadTrack(MidiFileReader file) {
        List<MidiEvent> result = new List<MidiEvent>(20);
        int starttime = 0;
        string id = file.ReadAscii(4);

        if (id != "MTrk") {
            throw new MidiFileException("Bad MTrk header", file.GetOffset() - 4);
        }
        int tracklen = file.ReadInt();
        int trackend = tracklen + file.GetOffset();

        int eventflag = 0;

        while (file.GetOffset() < trackend) {

            // If the midi file is truncated here, we can still recover.
            // Just return what we've parsed so far.

            int startoffset, deltatime;
            byte peekevent;
            try {
                startoffset = file.GetOffset();
                deltatime = file.ReadVarlen();
                starttime += deltatime;
                peekevent = file.Peek();
            }
            catch (MidiFileException e) {
                return result;
            }

            MidiEvent mevent = new MidiEvent();
            result.Add(mevent);
            mevent.DeltaTime = deltatime;
            mevent.StartTime = starttime;

            if (peekevent >= EventNoteOff) { 
                mevent.HasEventflag = true; 
                eventflag = file.ReadByte();
            }

            // Console.WriteLine("offset {0}: event {1} {2} start {3} delta {4}", 
            //                   startoffset, eventflag, EventName(eventflag), 
            //                   starttime, mevent.DeltaTime);

            if (eventflag >= EventNoteOn && eventflag < EventNoteOn + 16) {
                mevent.EventFlag = EventNoteOn;
                mevent.Channel = (byte)(eventflag - EventNoteOn);
                mevent.Notenumber = file.ReadByte();
                mevent.Velocity = file.ReadByte();
            }
            else if (eventflag >= EventNoteOff && eventflag < EventNoteOff + 16) {
                mevent.EventFlag = EventNoteOff;
                mevent.Channel = (byte)(eventflag - EventNoteOff);
                mevent.Notenumber = file.ReadByte();
                mevent.Velocity = file.ReadByte();
            }
            else if (eventflag >= EventKeyPressure && 
                     eventflag < EventKeyPressure + 16) {
                mevent.EventFlag = EventKeyPressure;
                mevent.Channel = (byte)(eventflag - EventKeyPressure);
                mevent.Notenumber = file.ReadByte();
                mevent.KeyPressure = file.ReadByte();
            }
            else if (eventflag >= EventControlChange && 
                     eventflag < EventControlChange + 16) {
                mevent.EventFlag = EventControlChange;
                mevent.Channel = (byte)(eventflag - EventControlChange);
                mevent.ControlNum = file.ReadByte();
                mevent.ControlValue = file.ReadByte();
            }
            else if (eventflag >= EventProgramChange && 
                     eventflag < EventProgramChange + 16) {
                mevent.EventFlag = EventProgramChange;
                mevent.Channel = (byte)(eventflag - EventProgramChange);
                mevent.Instrument = file.ReadByte();
            }
            else if (eventflag >= EventChannelPressure && 
                     eventflag < EventChannelPressure + 16) {
                mevent.EventFlag = EventChannelPressure;
                mevent.Channel = (byte)(eventflag - EventChannelPressure);
                mevent.ChanPressure = file.ReadByte();
            }
            else if (eventflag >= EventPitchBend && 
                     eventflag < EventPitchBend + 16) {
                mevent.EventFlag = EventPitchBend;
                mevent.Channel = (byte)(eventflag - EventPitchBend);
                mevent.PitchBend = file.ReadShort();
            }
            else if (eventflag == SysexEvent1) {
                mevent.EventFlag = SysexEvent1;
                mevent.Metalength = file.ReadVarlen();
                mevent.Value = file.ReadBytes(mevent.Metalength);
            }
            else if (eventflag == SysexEvent2) {
                mevent.EventFlag = SysexEvent2;
                mevent.Metalength = file.ReadVarlen();
                mevent.Value = file.ReadBytes(mevent.Metalength);
            }
            else if (eventflag == MetaEvent) {
                mevent.EventFlag = MetaEvent;
                mevent.Metaevent = file.ReadByte();
                mevent.Metalength = file.ReadVarlen();
                mevent.Value = file.ReadBytes(mevent.Metalength);
                if (mevent.Metaevent == MetaEventTimeSignature) {
                    if (mevent.Metalength < 2) {
                        // throw new MidiFileException(
                        //  "Meta Event Time Signature len == " + mevent.Metalength  + 
                        //  " != 4", file.GetOffset());
                        mevent.Numerator = (byte)0;
                        mevent.Denominator = (byte)4;
                    }
                    else if (mevent.Metalength >= 2 && mevent.Metalength < 4) {
                        mevent.Numerator = (byte)mevent.Value[0];
                        mevent.Denominator = (byte)System.Math.Pow(2, mevent.Value[1]);
                    }
                    else {
                        mevent.Numerator = (byte)mevent.Value[0];
                        mevent.Denominator = (byte)System.Math.Pow(2, mevent.Value[1]);
                    }
                }
                else if (mevent.Metaevent == MetaEventTempo) {
                    if (mevent.Metalength != 3) {
                        throw new MidiFileException(
                          "Meta Event Tempo len == " + mevent.Metalength +
                          " != 3", file.GetOffset());
                    }
                    mevent.Tempo = ( (mevent.Value[0] << 16) | (mevent.Value[1] << 8) | mevent.Value[2]);
                }
                else if (mevent.Metaevent == MetaEventEndOfTrack) {
                    /* break;  */
                }
            }
            else {
                throw new MidiFileException("Unknown event " + mevent.EventFlag,
                                             file.GetOffset()-1); 
            }
        }

        return result;
    }

    /** Return true if this track contains multiple channels.
     * If a MidiFile contains only one track, and it has multiple channels,
     * then we treat each channel as a separate track.
     */
    static bool HasMultipleChannels(MidiTrack track) {
        int channel = track.Notes[0].Channel;
        foreach (MidiNote note in track.Notes) {
            if (note.Channel != channel) {
                return true;
            }
        }
        return false;
    }

    /** Write a variable length number to the buffer at the given offset.
     * Return the number of bytes written.
     */
    static int VarlenToBytes(int num, byte[] buf, int offset) {
        byte b1 = (byte) ((num >> 21) & 0x7F);
        byte b2 = (byte) ((num >> 14) & 0x7F);
        byte b3 = (byte) ((num >>  7) & 0x7F);
        byte b4 = (byte) (num & 0x7F);

        if (b1 > 0) {
            buf[offset]   = (byte)(b1 | 0x80);
            buf[offset+1] = (byte)(b2 | 0x80);
            buf[offset+2] = (byte)(b3 | 0x80);
            buf[offset+3] = b4;
            return 4;
        }
        else if (b2 > 0) {
            buf[offset]   = (byte)(b2 | 0x80);
            buf[offset+1] = (byte)(b3 | 0x80);
            buf[offset+2] = b4;
            return 3;
        }
        else if (b3 > 0) {
            buf[offset]   = (byte)(b3 | 0x80);
            buf[offset+1] = b4;
            return 2;
        }
        else {
            buf[offset] = b4;
            return 1;
        }
    }

    /** Write a 4-byte integer to data[offset : offset+4] */
    private static void IntToBytes(int value, byte[] data, int offset) {
        data[offset] = (byte)( (value >> 24) & 0xFF );
        data[offset+1] = (byte)( (value >> 16) & 0xFF );
        data[offset+2] = (byte)( (value >> 8) & 0xFF );
        data[offset+3] = (byte)( value & 0xFF );
    }

    /** Calculate the track length (in bytes) given a list of Midi events */
    private static int GetTrackLength(List<MidiEvent> events) {
        int len = 0;
        byte[] buf = new byte[1024];
        foreach (MidiEvent mevent in events) {
            len += VarlenToBytes(mevent.DeltaTime, buf, 0);
            len += 1;  /* for eventflag */
            switch (mevent.EventFlag) {
                case EventNoteOn: len += 2; break;
                case EventNoteOff: len += 2; break;
                case EventKeyPressure: len += 2; break;
                case EventControlChange: len += 2; break;
                case EventProgramChange: len += 1; break;
                case EventChannelPressure: len += 1; break;
                case EventPitchBend: len += 2; break;

                case SysexEvent1: 
                case SysexEvent2:
                    len += VarlenToBytes(mevent.Metalength, buf, 0); 
                    len += mevent.Metalength;
                    break;
                case MetaEvent: 
                    len += 1; 
                    len += VarlenToBytes(mevent.Metalength, buf, 0); 
                    len += mevent.Metalength;
                    break;
                default: break;
            }
        }
        return len;
    }
            

    /** Write the given list of Midi events to a stream/file.
     *  This method is used for sound playback, for creating new Midi files
     *  with the tempo, transpose, etc changed.
     *
     *  Return true on success, and false on error.
     */
    private static bool 
    WriteEvents(Stream file, List<MidiEvent>[] events, int trackmode, int quarter) {
        try {
            byte[] buf = new byte[4096];

            /* Write the MThd, len = 6, track mode, number tracks, quarter note */
            file.Write(ASCIIEncoding.ASCII.GetBytes("MThd"), 0, 4);
            IntToBytes(6, buf, 0);
            file.Write(buf, 0, 4);
            buf[0] = (byte)(trackmode >> 8); 
            buf[1] = (byte)(trackmode & 0xFF);
            file.Write(buf, 0, 2);
            buf[0] = 0; 
            buf[1] = (byte)events.Length;
            file.Write(buf, 0, 2);
            buf[0] = (byte)(quarter >> 8); 
            buf[1] = (byte)(quarter & 0xFF);
            file.Write(buf, 0, 2);

            foreach (List<MidiEvent> list in events) {
                /* Write the MTrk header and track length */
                file.Write(ASCIIEncoding.ASCII.GetBytes("MTrk"), 0, 4);
                int len = GetTrackLength(list);
                IntToBytes(len, buf, 0);
                file.Write(buf, 0, 4);

                foreach (MidiEvent mevent in list) {
                    int varlen = VarlenToBytes(mevent.DeltaTime, buf, 0);
                    file.Write(buf, 0, varlen);

                    if (mevent.EventFlag == SysexEvent1 ||
                        mevent.EventFlag == SysexEvent2 ||
                        mevent.EventFlag == MetaEvent) {
                        buf[0] = mevent.EventFlag;
                    }
                    else {
                        buf[0] = (byte)(mevent.EventFlag + mevent.Channel);
                    }
                    file.Write(buf, 0, 1);

                    if (mevent.EventFlag == EventNoteOn) {
                        buf[0] = mevent.Notenumber;
                        buf[1] = mevent.Velocity;
                        file.Write(buf, 0, 2);
                    }
                    else if (mevent.EventFlag == EventNoteOff) {
                        buf[0] = mevent.Notenumber;
                        buf[1] = mevent.Velocity;
                        file.Write(buf, 0, 2);
                    }
                    else if (mevent.EventFlag == EventKeyPressure) {
                        buf[0] = mevent.Notenumber;
                        buf[1] = mevent.KeyPressure;
                        file.Write(buf, 0, 2);
                    }
                    else if (mevent.EventFlag == EventControlChange) {
                        buf[0] = mevent.ControlNum;
                        buf[1] = mevent.ControlValue;
                        file.Write(buf, 0, 2);
                    }
                    else if (mevent.EventFlag == EventProgramChange) {
                        buf[0] = mevent.Instrument;
                        file.Write(buf, 0, 1);
                    }
                    else if (mevent.EventFlag == EventChannelPressure) {
                        buf[0] = mevent.ChanPressure;
                        file.Write(buf, 0, 1);