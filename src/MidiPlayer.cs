
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
                                        playButton.Location.Y);
        stopButton.Click += new EventHandler(Stop);
        tip = new ToolTip();
        tip.SetToolTip(stopButton, "Stop");

        /* Create the fastFwd button */        
        fastFwdButton = new Button();
        fastFwdButton.Parent = this;
        fastFwdButton.Image = fastFwdImage;
        fastFwdButton.ImageAlign = ContentAlignment.MiddleCenter;
        fastFwdButton.Size = new Size(buttonheight, buttonheight);
        fastFwdButton.Location = new Point(stopButton.Location.X + stopButton.Width + buttonheight/2,                      
                                          stopButton.Location.Y);
        fastFwdButton.Click += new EventHandler(FastForward);
        tip = new ToolTip();
        tip.SetToolTip(fastFwdButton, "Fast Forward");



        /* Create the Speed bar */
        Label speedLabel = new Label();
        speedLabel.Parent = this;
        speedLabel.Text = "Speed: ";
        speedLabel.TextAlign = ContentAlignment.MiddleRight;
        speedLabel.Height = buttonheight;
        speedLabel.Width = buttonheight*2;
        speedLabel.Location = new Point(fastFwdButton.Location.X + fastFwdButton.Width + buttonheight/2,
                                        fastFwdButton.Location.Y);

        speedBar = new TrackBar();
        speedBar.Parent = this;
        speedBar.Minimum = 1;
        speedBar.Maximum = 100;
        speedBar.TickFrequency = 10;
        speedBar.TickStyle = TickStyle.BottomRight;
        speedBar.LargeChange = 10;
        speedBar.Value = 100;
        speedBar.Width = buttonheight * 5;
        speedBar.Location = new Point(speedLabel.Location.X + speedLabel.Width + 2,
                                      speedLabel.Location.Y);
        tip = new ToolTip();
        tip.SetToolTip(speedBar, "Adjust the speed");

        /* Create the Volume bar */
        Label volumeLabel = new Label();
        volumeLabel.Parent = this;
        volumeLabel.Image = volumeImage;
        volumeLabel.ImageAlign = ContentAlignment.MiddleRight;
        volumeLabel.Height = buttonheight;
        volumeLabel.Width = buttonheight*2;
        volumeLabel.Location = new Point(speedBar.Location.X + speedBar.Width + buttonheight/2,
                                         speedBar.Location.Y);

        volumeBar = new TrackBar();
        volumeBar.Parent = this;
        volumeBar.Minimum = 1;
        volumeBar.Maximum = 100;
        volumeBar.TickFrequency = 10;
        volumeBar.TickStyle = TickStyle.BottomRight;
        volumeBar.LargeChange = 10;
        volumeBar.Value = 100;
        volumeBar.Width = buttonheight * 5;
        volumeBar.Location = new Point(volumeLabel.Location.X + volumeLabel.Width + 2,
                                       volumeLabel.Location.Y);
        volumeBar.Scroll += new EventHandler(ChangeVolume);
        tip = new ToolTip();
        tip.SetToolTip(volumeBar, "Adjust the volume");

        Height = buttonheight*2;

        /* Initialize the timer used for playback, but don't start
         * the timer yet (enabled = false).
         */
        timer = new Timer();
        timer.Enabled = false;
        timer.Interval = 100;  /* 100 millisec */
        timer.Tick += new EventHandler(TimerCallback);

        tempSoundFile = "";
    }

    public void SetPiano(Piano p) {
        piano = p;
    }

    /** The MidiFile and/or SheetMusic has changed. Stop any playback sound,
     *  and store the current midifile and sheet music.
     */
    public void SetMidiFile(MidiFile file, MidiOptions opt, SheetMusic s) {

        /* If we're paused, and using the same midi file, redraw the
         * highlighted notes.
         */
        if ((file == midifile && midifile != null && playstate == paused)) {
            options = opt;
            sheet = s;
            sheet.ShadeNotes((int)currentPulseTime, (int)-10, false);

            /* We have to wait some time (200 msec) for the sheet music
             * to scroll and redraw, before we can re-shade.
             */
            Timer redrawTimer = new Timer();
            redrawTimer.Interval = 200;
            redrawTimer.Tick += new EventHandler(ReShade);
            redrawTimer.Enabled = true;
            redrawTimer.Start();
        }
        else {
            this.Stop(null, null);
            midifile = file;
            options = opt;
            sheet = s;
        }
        this.DeleteSoundFile();
    }

    /** If we're paused, reshade the sheet music and piano. */
    private void ReShade(object sender, EventArgs args) {
        if (playstate == paused) {
            sheet.ShadeNotes((int)currentPulseTime, (int)-10, false);
            piano.ShadeNotes((int)currentPulseTime, (int)prevPulseTime);
        }
        Timer redrawTimer = (Timer) sender;
        redrawTimer.Stop();
        redrawTimer.Dispose();
    }


    /** Delete the temporary midi sound file */
    public void DeleteSoundFile() {
        if (tempSoundFile == "") {
            return;
        }
        try {
            FileInfo soundfile = new FileInfo(tempSoundFile);
            soundfile.Delete();
        }
        catch (IOException e) {
        }
        tempSoundFile = ""; 
    }

    /** Return the number of tracks selected in the MidiOptions.
     *  If the number of tracks is 0, there is no sound to play.
     */
    private int numberTracks() {
        int count = 0;
        for (int i = 0; i < options.tracks.Length; i++) {
            if (options.tracks[i] && !options.mute[i]) {
                count += 1;
            }
        }
        return count;
    }

    /** Create a new midi file with all the MidiOptions incorporated.
     *  Save the new file to TEMP/<originalfile>.MSM.mid, and store
     *  this temporary filename in tempSoundFile.
     */ 
    private void CreateMidiFile() {
        double inverse_tempo = 1.0 / midifile.Time.Tempo;
        double inverse_tempo_scaled = inverse_tempo * speedBar.Value / 100.0;
        options.tempo = (int)(1.0 / inverse_tempo_scaled);
        pulsesPerMsec = midifile.Time.Quarter * (1000.0 / options.tempo);

        string filename = Path.GetFileName(midifile.FileName).Replace(".mid", "") + ".MSM.mid";
        tempSoundFile = System.IO.Path.GetTempPath() + "/" + filename;

        /* If the filename is > 127 chars, the sound won't play */
        if (tempSoundFile.Length > 127) {
            tempSoundFile = System.IO.Path.GetTempPath() + "/MSM.mid";
        }

        if (midifile.ChangeSound(tempSoundFile, options) == false) {
            /* Failed to write to tempSoundFile */
            tempSoundFile = ""; 
        }
    }


    /** Play the sound for the given MIDI file */
    private void PlaySound(string filename) {
        if (Type.GetType("Mono.Runtime") != null)
            PlaySoundMono(filename);
        else 
            PlaySoundWindows(filename);
    }

    /** On Linux Mono, we spawn the timidity command to play the
     *  midi file for us.
     */
    private void PlaySoundMono(string filename) {
        try {
            ProcessStartInfo info = new ProcessStartInfo();
            info.CreateNoWindow = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardInput = true;
            info.UseShellExecute = false;
            info.FileName = "/usr/bin/timidity";
            info.Arguments = "\"" + filename + "\"";
            timidity = new Process();
            timidity.StartInfo = info;
            timidity.Start();