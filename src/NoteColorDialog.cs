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
using System.Windows.Forms;
using System.Collections.Generic;

namespace MidiSheetMusic {

/** @class NoteColorDialog 
 * The NoteColorDialog is used to choose what color to display for each of
 * the 12 notes in a scale, as well as the shade color.
 */
public class NoteColorDialog {

    private Color[] colors;      /** The 12 colors used for each note in the note scale */
    private Button[] buttons;    /** The 12 buttons used to select the colors. */
    private Color shadeColor;    /** The color used for shading notes during playback */
    private Color shade2Color;   /** The color used for shading the left hand piano. */
    private Button shadeButton;  /** The button used to select the shade color */
    private Button shade2Button; /** The button used to select the shade2 color */
    private Form dialog;         /** The dialog box */


    /** Create a new NoteColorDialog.  Call the ShowDialog() method
     * to display the dialog.
     */
    public NoteColorDialog() {
        /* Create the dialog box */
        dialog = new Form();
        Font font = dialog.Font;
        dialog.Font = new Font(font.FontFamily, font.Size * 1.4f);
        int unit = dialog.Font.Height * 4/3;
        int xstart = unit * 2;
        int ystart = unit * 2;
        int labelheight = unit * 3/2;
        int maxwidth = 0;

        dialog.Text = "Choose Note Colors";
        dialog.MaximizeBox = false;
        dialog.MinimizeBox = false;
        dialog.ShowInTaskbar = false;
        dialog.Icon = new Icon(GetType(), "NotePair.ico");

        /* Initialize the colors */
        shadeColor = Color.FromArgb(210, 205, 220);
        shade2Color = Color.FromArgb(150, 200, 220);
        colors = new Color[12];
        colors[0] = Color.FromArgb(180, 0, 0);
        colors[1] = Color.FromArgb(230, 0, 0);
        colors[2] = Color.FromArgb(220, 128, 0);
        colors[3] = Color.FromArgb(130, 130, 0);
        colors[4] = Color.FromArgb(187, 187, 0);
        colors[5] = Color.FromArgb(0, 100, 0);
        colors[6] = Color.FromArgb(0, 140, 0);
        colors[7] = Color.FromArgb(0, 180, 180);
        colors[8] = Color.FromArgb(0, 0, 120);
        colors[9] = Color.FromArgb(0, 0, 180);
        colors[10] = Color.FromArgb(88, 0, 147);
        colors[11] = Color.FromArgb(129, 0, 215);

        buttons = new Button[12];

        string[] names = { "A", "A#", "B", "C", "C#", "D", 
                           "D#", "E", "F", "F#", "G", "G#" };

        /* Create the first column, note labels A thru D */
        for (int i = 0; i < 6; i++) {
            Label note = new Label();
            note.Parent = dialog;
            note.Text = names[i];
            note.Location = new Point(xstart, ystart + i * labelheight);
            note.AutoSize = true;
            maxwidth = Math.Max(maxwidth, note.Width);
        }

        /* Create the second column, the colors */
        xstart += maxwidth * 4/3;
        for (int i = 0; i < 6; i++) {
            buttons[i] = new Button();
            buttons[i].Parent = dialog;
            buttons[i].Text = "";
            buttons[i].Tag = i;
            buttons[i].Location = new Point(xstart, ystart + i * labelheight);
            buttons[i].Size = new Size(maxwidth*3/2, labelheight-4);
            buttons[i].ForeColor = buttons[i].BackColor = colors[i];
            buttons[i].Click += new EventHandler(SetNoteColor);
        }

        /* Create the third column, note labels D# thru G# */
        xstart += maxwidth * 2;
        for (int i = 6; i < 12; i++) {
            Label note = new Label();
            note.Parent = dialog;
            note.Text = names[i];
            note.Location = new Point(xstart, (i - 6) * labelheight + ystart);
            note.AutoSize = true;
            maxwidth = Math.Max(maxwidth, note.Width);
        }

        /* Create the fourth column, the colors */
        xstart += maxwidth * 4/3 ;
        for (int i = 6; i < 12; i++) {
            buttons[i] = new Button();
            buttons[i].Parent = dialog;
            buttons[i].Text = "";
            buttons[i].Tag = i;
            buttons[i].Location = new Point(xstart, (i - 6) * labelheight + ystart);
            buttons[i].Size = new Size(maxwidth*3/2, labelheight-4);
      