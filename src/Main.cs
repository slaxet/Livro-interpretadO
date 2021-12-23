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
using System.Drawing;
using MidiSheetMusic;

public class MainClass {

    [STAThread]
    public static void Main(string[] argv) {
        if (argv.Length < 2) {
            Console.WriteLine("Usage: sheet input.mid output_prefix(_[page_number].png)");
            return;
        }
        string filename = argv[0];
        SheetMusic sheet = new SheetMusic(filename, null);
		