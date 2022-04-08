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
        c