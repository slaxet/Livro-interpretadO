
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


/** @class MidiFileReader
 * The MidiFileReader is used to read low-level binary data from a file.
 * This class can do the following:
 *
 * - Peek at the next byte in the file.
 * - Read a byte
 * - Read a 16-bit big endian short
 * - Read a 32-bit big endian int
 * - Read a fixed length ascii string (not null terminated)
 * - Read a "variable length" integer.  The format of the variable length
 *   int is described at the top of this file.
 * - Skip ahead a given number of bytes
 * - Return the current offset.
 */

public class MidiFileReader {
    private byte[] data;       /** The entire midi file data */
    private int parse_offset;  /** The current offset while parsing */

    /** Create a new MidiFileReader for the given filename */
    public MidiFileReader(string filename) {
        FileInfo info = new FileInfo(filename);
        if (!info.Exists) {
            throw new MidiFileException("File " + filename + " does not exist", 0);
        }
        if (info.Length == 0) {
            throw new MidiFileException("File " + filename + " is empty (0 bytes)", 0);
        }
        FileStream file = File.Open(filename, FileMode.Open, 
                                    FileAccess.Read, FileShare.Read);

        /* Read the entire file into memory */
        data = new byte[ info.Length ];
        int offset = 0;
        int len = (int)info.Length;
        while (true) {
            if (offset == info.Length)
                break;
            int n = file.Read(data, offset, (int)(info.Length - offset));
            if (n <= 0)
                break;
            offset += n;
        }
        parse_offset = 0;
        file.Close();
    }

    /** Create a new MidiFileReader from the given data */
    public MidiFileReader(byte[] bytes) {
        data = bytes;
        parse_offset = 0;
    }

    /** Check that the given number of bytes doesn't exceed the file size */
    private void checkRead(int amount) {
        if (parse_offset + amount > data.Length) {
            throw new MidiFileException("File is truncated", parse_offset);
        }
    }

    /** Read the next byte in the file, but don't increment the parse offset */
    public byte Peek() {
        checkRead(1);
        return data[parse_offset];
    }

    /** Read a byte from the file */
    public byte ReadByte() { 
        checkRead(1);
        byte x = data[parse_offset];
        parse_offset++;
        return x;
    }

    /** Read the given number of bytes from the file */
    public byte[] ReadBytes(int amount) {
        checkRead(amount);
        byte[] result = new byte[amount];
        for (int i = 0; i < amount; i++) {
            result[i] = data[i + parse_offset];
        }
        parse_offset += amount;
        return result;
    }

    /** Read a 16-bit short from the file */
    public ushort ReadShort() {
        checkRead(2);
        ushort x = (ushort) ( (data[parse_offset] << 8) | data[parse_offset+1] );
        parse_offset += 2;
        return x;
    }

    /** Read a 32-bit int from the file */
    public int ReadInt() {