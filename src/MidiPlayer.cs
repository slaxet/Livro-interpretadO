
/*
 * Copyright (c) 2011-2012 Madhav Vaidyanathan
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
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;

namespace MidiSheetMusic {

/** @class MidiPlayer
 *
 * The MidiPlayer is the panel at the top used to play the sound
 * of the midi file.  It consists of:
 *
 * - The Rewind button
 * - The Play/Pause button
 * - The Stop button
 * - The Fast Forward button
 * - The Playback speed bar
 * - The Volume bar
 *
 * The sound of the midi file depends on
 * - The MidiOptions (taken from the menus)
 *   Which tracks are selected
 *   How much to transpose the keys by
 *   What instruments to use per track
 * - The tempo (from the Speed bar)
 * - The volume
 *
 * The MidiFile.ChangeSound() method is used to create a new midi file
 * with these options.  The mciSendString() function is used for 
 * playing, pausing, and stopping the sound.
 *
 * For shading the notes during playback, the method
 * SheetMusic.ShadeNotes() is used.  It takes the current 'pulse time',
 * and determines which notes to shade.
 */
public class MidiPlayer : Panel  {
    private Image rewindImage;   /** The rewind image */
    private Image playImage;     /** The play image */
    private Image pauseImage;    /** The pause image */
    private Image stopImage;     /** The stop image */
    private Image fastFwdImage;  /** The fast forward image */
    private Image volumeImage;   /** The volume image */

    private Button rewindButton; /** The rewind button */
    private Button playButton;   /** The play/pause button */
    private Button stopButton;   /** The stop button */
    private Button fastFwdButton;/** The fast forward button */
    private TrackBar speedBar;   /** The trackbar for controlling the playback speed */
    private TrackBar volumeBar;  /** The trackbar for controlling the volume */
    private ToolTip playTip;     /** The tooltip for the play button */

    int playstate;               /** The playing state of the Midi Player */
    const int stopped   = 1;     /** Currently stopped */
    const int playing   = 2;     /** Currently playing music */
    const int paused    = 3;     /** Currently paused */
    const int initStop  = 4;     /** Transitioning from playing to stop */
    const int initPause = 5;     /** Transitioning from playing to pause */

    MidiFile midifile;          /** The midi file to play */
    MidiOptions options;   /** The sound options for playing the midi file */
    string tempSoundFile;       /** The temporary midi filename currently being played */
    double pulsesPerMsec;       /** The number of pulses per millisec */
    SheetMusic sheet;           /** The sheet music to shade while playing */
    Piano piano;                /** The piano to shade while playing */
    Timer timer;                /** Timer used to update the sheet music while playing */
    TimeSpan startTime;         /** Absolute time when music started playing */
    double startPulseTime;      /** Time (in pulses) when music started playing */
    double currentPulseTime;    /** Time (in pulses) music is currently at */
    double prevPulseTime;       /** Time (in pulses) music was last at */
    StringBuilder errormsg;     /** Error messages from midi player */
    Process timidity;           /** The Linux timidity process */

    [DllImport("winmm.dll")]
    public static extern int mciSendString(string lpstrCommand,
                                           string lpstrReturnString,
                                           int uReturnLength,
                                           int dwCallback);

    [DllImport("winmm.dll")]
    public static extern int mciGetErrorString(int errcode, 
                                               StringBuilder msg, uint buflen);


    /** Load the play/pause/stop button images */
    private void loadButtonImages() {
        int buttonheight = this.Font.Height * 2;
        Size imagesize = new Size(buttonheight, buttonheight);
        rewindImage = new Bitmap(typeof(MidiPlayer), "rewind.png");
        rewindImage = new Bitmap(rewindImage, imagesize);
        playImage = new Bitmap(typeof(MidiPlayer), "play.png");
        playImage = new Bitmap(playImage, imagesize);
        pauseImage = new Bitmap(typeof(MidiPlayer), "pause.png");
        pauseImage = new Bitmap(pauseImage, imagesize);
        stopImage = new Bitmap(typeof(MidiPlayer), "stop.png");
        stopImage = new Bitmap(stopImage, imagesize);
        fastFwdImage = new Bitmap(typeof(MidiPlayer), "fastforward.png");
        fastFwdImage = new Bitmap(fastFwdImage, imagesize);
        volumeImage = new Bitmap(typeof(MidiPlayer), "volume.png");
        volumeImage = new Bitmap(volumeImage, imagesize);
    }

    /** Create a new MidiPlayer, displaying the play/stop buttons, the
     *  speed bar, and volume bar.  The midifile and sheetmusic are initially null.
     */
    public MidiPlayer() {
        this.Font = new Font("Arial", 10, FontStyle.Bold);
        loadButtonImages();
        int buttonheight = this.Font.Height * 2;

        this.midifile = null;
        this.options = null;
        this.sheet = null;
        playstate = stopped;
        startTime = DateTime.Now.TimeOfDay;
        startPulseTime = 0;
        currentPulseTime = 0;
        prevPulseTime = -10;
        errormsg = new StringBuilder(256);
        ToolTip tip;

        /* Create the rewind button */
        rewindButton = new Button();
        rewindButton.Parent = this;
        rewindButton.Image = rewindImage;
        rewindButton.ImageAlign = ContentAlignment.MiddleCenter;
        rewindButton.Size = new Size(buttonheight, buttonheight);
        rewindButton.Location = new Point(buttonheight/2, buttonheight/2);
        rewindButton.Click += new EventHandler(Rewind);
        tip = new ToolTip();
        tip.SetToolTip(rewindButton, "Rewind");

        /* Create the play button */
        playButton = new Button();
        playButton.Parent = this;
        playButton.Image = playImage;
        playButton.ImageAlign = ContentAlignment.MiddleCenter;
        playButton.Size = new Size(buttonheight, buttonheight);
        playButton.Location = new Point(buttonheight/2, buttonheight/2);
        playButton.Location = new Point(rewindButton.Location.X + rewindButton.Width + buttonheight/2,
                                        rewindButton.Location.Y);
        playButton.Click += new EventHandler(PlayPause);
        playTip = new ToolTip();
        playTip.SetToolTip(playButton, "Play");

        /* Create the stop button */
        stopButton = new Button();
        stopButton.Parent = this;
        stopButton.Image = stopImage;
        stopButton.ImageAlign = ContentAlignment.MiddleCenter;
        stopButton.Size = new Size(buttonheight, buttonheight);
        stopButton.Location = new Point(playButton.Location.X + playButton.Width + buttonheight/2,