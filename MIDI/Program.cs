using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Speech.Synthesis;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Common;
using System.Text.RegularExpressions;

namespace MIDI
{
    class Program
    {
        private static readonly Random rng = new Random();
        private static readonly SpeechSynthesizer tts = new SpeechSynthesizer();
        private static readonly string[] naturalnotes = { "C", null, "D", null, "E", "F", null, "G", null, "A", null, "B" };
        private static readonly string[] sharpnotes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        private static readonly string[] flatnotes = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };

        private static void Main()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("WELCOME TO MIDI PRACTICE!\nSelect mode by typing number (1 to 4) and hitting ENTER.\n\n");
            Console.ResetColor();
            Console.WriteLine("1: Numpad Mode\n    Numbers 0 to 9 (no MIDI keyboard required!)\n");
            Console.WriteLine("2: Basic Mode\n    White keys only\n");
            Console.WriteLine("3: Regular Mode\n    White keys and black keys\n");
            Console.WriteLine("4: Advanced Mode\n    All naturals, sharps and flats.\n");
            Console.WriteLine("x: Exit");
            Console.WriteLine();
            InputDevice inputDevice = null;
            bool game = int.TryParse(Console.ReadLine(), out int input);
            if (input > 1)
            {
                inputDevice = SelectMidiDevice(InputDevice.GetAll().ToList());
                if (inputDevice == default)
                {
                    input = 1;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("A MIDI keyboard is required for this mode! Starting Numpad Mode instead.\nPress ENTER to continue.");
                    Console.ResetColor();
                    Console.ReadLine();
                }
            }
            switch (input)
            {
                case 1:
                    Console.WriteLine("Numpad mode selected.");
                    break;
                case 2:
                    Console.WriteLine("Basic mode selected.");
                    break;
                case 3:
                    Console.WriteLine("Regular mode selected.");
                    break;
                case 4:
                    Console.WriteLine("Advanced mode selected.");
                    break;
                default:
                    Console.WriteLine("Exit.");
                    break;
            }
            if (game)
            {
                OutputDevice outputDevice = SelectMidiDevice(OutputDevice.GetAll().ToList());
                outputDevice.EventSent += EmptyEvent;
                int preGameCountdown = 3;
                while (preGameCountdown > 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"Starting: in {preGameCountdown}"); //BUG accepts inputs during countdown
                    Console.ResetColor();
                    preGameCountdown -= 1;
                    Pause(1000);
                }
                int rounds = input == 1 ? 30 : 20; //more rounds for Numpad mode!!
                int points = 0;
                if (input > 1)
                {
                    for (int i = 0; i < rounds; i++)
                    {
                        points += MIDIMode(outputDevice, inputDevice, input);
                    }
                }
                else if (input == 1) //Numpad Mode
                {
                    for (int i = 0; i < rounds; i++)
                    {
                        points += NumpadMode();
                    }
                }
                if (inputDevice != null)
                {
                    inputDevice.Dispose();
                }
                Console.WriteLine();
                if (points == 5 * rounds) //Perfect Score Announcement!
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Results: You achieved a perfect score with {points} points! Congratulations!");
                    Console.ResetColor();
                    tts.SpeakAsyncCancelAll();
                    tts.SpeakAsync($"Perfect Score! Results: You achieved a perfect score with {points} points. Congratulations!");
                    outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)(60), (SevenBitNumber)127));
                    outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)(64), (SevenBitNumber)127));
                    outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)(67), (SevenBitNumber)127));
                    Pause(100);
                    outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)(60), (SevenBitNumber)127));
                    outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)(64), (SevenBitNumber)127));
                    outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)(67), (SevenBitNumber)127));
                    Pause(50);
                    outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)(60), (SevenBitNumber)127));
                    outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)(64), (SevenBitNumber)127));
                    outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)(67), (SevenBitNumber)127));
                    Pause(1000);
                    outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)(60), (SevenBitNumber)127));
                    outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)(64), (SevenBitNumber)127));
                    outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)(67), (SevenBitNumber)127));
                }
                else //Regular Score Announcement.
                {
                    Console.WriteLine($"Results: You scored {points}/{5 * rounds} points.");
                    tts.SpeakAsyncCancelAll();
                    tts.SpeakAsync($"Results: You scored {points} out of a maximum of {5 * rounds} points.");
                }
                outputDevice.Dispose();
            }
            Console.WriteLine("\nPress ENTER to exit.");
            Console.ReadLine();
        }
        private static int NumpadMode()
        {
            string numToPlay = rng.Next(10).ToString();
            tts.SpeakAsyncCancelAll();
            tts.SpeakAsync($"{numToPlay}. . . . . . . . . . . .{numToPlay}."); //Using lots of fullstops to generate gap between repeat.
            Pause(500);
            Console.WriteLine(numToPlay);
            Timer antiCheatTimer = new Timer
            {
                Interval = 100,
                Enabled = true
            };
            bool cheatMode = true;
            antiCheatTimer.Elapsed += (antiCheatSender, antiCheatEvent) => ChangeBoolToFalse(antiCheatSender, antiCheatEvent, ref cheatMode);
            string numPlayed = null;
            while (cheatMode)
            {
                if (numPlayed != null)
                {
                    Console.WriteLine("CHEAT! Wait for the note to come up before pressing anything!");
                    antiCheatTimer.Dispose();
                    Pause(1000);
                    return 0;
                }
            }
            Timer timer = new Timer
            {
                Interval = 500,
                Enabled = true
            };
            int pointsToAdd = 5;
            timer.Elapsed += (timersender, timerevent) => ReducePointsGiven(timersender, timerevent, ref pointsToAdd);
            numPlayed = Console.ReadKey().KeyChar.ToString();
            Console.WriteLine();
            timer.Dispose();
            return NotePlayed(numPlayed, numToPlay, pointsToAdd);
        }

        private static int MIDIMode(OutputDevice outputDevice, InputDevice inputDevice, int mode)
        {
            int midiNotePlayed = -1;
            inputDevice.EventReceived += (midisender, midievent) => MidiEventReceived(midisender, midievent, ref midiNotePlayed);
            inputDevice.StartEventsListening();
            int noteNumber = rng.Next(12);
            double sharpflag = rng.NextDouble();
            string noteToPlay = GetNoteToPlay(mode, ref noteNumber, sharpflag);
            outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)(noteNumber + 60), (SevenBitNumber)127));
            tts.SpeakAsyncCancelAll();
            tts.SpeakAsync(noteToPlay.Replace("#", " Sharp").Replace("b", " Flat"));
            Console.WriteLine(noteToPlay);
            Timer antiCheatTimer = new Timer
            {
                Interval = 100,
                Enabled = true
            };
            bool cheatMode = true;
            antiCheatTimer.Elapsed += (antiCheatSender, antiCheatEvent) => ChangeBoolToFalse(antiCheatSender, antiCheatEvent, ref cheatMode);
            while (cheatMode)
            {
                if (midiNotePlayed != -1)
                {
                    Console.WriteLine("CHEAT! Wait for the note to come up before pressing anything!");
                    antiCheatTimer.Dispose();
                    outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)(noteNumber + 60), (SevenBitNumber)127));
                    Pause(1000);
                    return 0;
                }
            }
            antiCheatTimer.Dispose();
            outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)(noteNumber + 60), (SevenBitNumber)127));
            Timer timer = new Timer
            {
                Interval = 900,
                Enabled = true
            };
            int pointsToAdd = 5;
            timer.Elapsed += (timersender, timerevent) => ReducePointsGiven(timersender, timerevent, ref pointsToAdd);
            while (midiNotePlayed == -1)
            {
                // waiting for MIDI input
            }
            timer.Dispose();
            return NotePlayed(GetNotePlayed(mode, midiNotePlayed, sharpflag), noteToPlay, pointsToAdd);
        }
        private static string GetNoteToPlay(int mode, ref int noteNumber, double sharpflag)
        {
            string noteToPlay = mode != 3 ? naturalnotes[noteNumber] : sharpflag < 0.5 ? sharpnotes[noteNumber] : flatnotes[noteNumber];
            while (noteToPlay == null)
            {
                noteNumber = rng.Next(12);
                noteToPlay = naturalnotes[noteNumber];
            }
            if (mode == 4 && sharpflag < 0.5)
            {
                if (sharpflag < 0.25)
                {
                    noteNumber = (noteNumber + 1) % 12;
                    return noteToPlay + "#";
                }
                else
                {
                    noteNumber = (noteNumber + 11) % 12;
                    return noteToPlay + "b";
                }
            }
            return noteToPlay;
        }
        private static string GetNotePlayed(int mode, int noteNumber, double sharpflag)
        {
            string notePlayed = mode != 3 || sharpflag < 0.5 ? sharpnotes[noteNumber] : flatnotes[noteNumber];
            if (mode == 4 && sharpflag < 0.5)
            {
                notePlayed = sharpflag < 0.25 ? sharpnotes[(noteNumber + 11) % 12] + "#" : flatnotes[(noteNumber + 1) % 12] + "b";
                if (notePlayed.Contains("##"))
                {
                    return sharpnotes[noteNumber];
                }
                if (notePlayed.Contains("bb"))
                {
                    return flatnotes[noteNumber];
                }
            }
            return notePlayed;
        }
        private static int NotePlayed(string midiNotePlayed, string noteToPlay, int pointsToAdd)
        {
            int points = 0;
            Console.WriteLine($"You hit: {midiNotePlayed}");
            Console.WriteLine();
            if (midiNotePlayed == noteToPlay)
            {
                Console.ForegroundColor = pointsToAdd == 5 ? ConsoleColor.Yellow : ConsoleColor.Green;
                Console.WriteLine($"Correct! You scored {pointsToAdd} points.");
                Console.ResetColor();
                tts.SpeakAsyncCancelAll();
                points += pointsToAdd;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Incorrect! You were asked to hit {noteToPlay}, no points for you this time.");
                Console.ResetColor();
                tts.SpeakAsyncCancelAll();
                tts.SpeakAsync("Incorrect.");
                Pause(800);
            }
            Console.WriteLine();
            Pause(400);
            return points;
        }
        private static void Pause(int interval)
        {
            bool paused = true;
            Timer timer = new Timer
            {
                Interval = interval,
                Enabled = true
            };
            timer.Elapsed += (timersender, timerevent) => ChangeBoolToFalse(timersender, timerevent, ref paused);
            while (paused)
            {

            }
            timer.Dispose();
        }
        private static void EmptyEvent(object sender, MidiEventSentEventArgs e)
        {
        }
        private static T SelectMidiDevice<T>(IList<T> list)
        {
            if (list.Count() == 1)
            {
                return list[0];
            }
            else if (list.Count > 1)
            {
                string[] typeOfDevice = Regex.Split(typeof(T).Name, @"(?<!^)(?=[A-Z])");
                Console.WriteLine($"Select your MIDI {typeOfDevice[0]} {typeOfDevice[1]}:");
                for (int i = 0; i < list.Count(); i++)
                {
                    Console.WriteLine($"{i}: {list[i]}");
                }
                int input = int.Parse(Console.ReadLine());
                return input < list.Count() && input >= 0 ? list[input] : throw new ArgumentOutOfRangeException("Pick a MIDI device listed");
            }
            else 
            {
                return default; 
            }
        }
        private static void MidiEventReceived(object sender, MidiEventReceivedEventArgs e, ref int output)
        {
            if (e.Event.EventType == MidiEventType.NoteOn)
            {
                output = int.Parse(new string(e.Event.ToString().Split('(', ')')[1].TakeWhile(char.IsDigit).ToArray())) % 12;
            }
        }
        private static void ReducePointsGiven(object sender, ElapsedEventArgs e, ref int points)
        {
            if (points > 0)
            {
                points -= 1;
            }
        }
        private static void ChangeBoolToFalse(object sender, ElapsedEventArgs e, ref bool boolToChange)
        {
            boolToChange = false;
        }
    }
}
