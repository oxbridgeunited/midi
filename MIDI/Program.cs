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

        private static void Main(string[] args)
        {
            Console.WriteLine("Note Practice");
            Console.WriteLine("1: Super Basic");
            Console.WriteLine("2: Basic");
            Console.WriteLine("3: Advanced");
            Console.WriteLine("x: exit");
            string input1 = Console.ReadLine();
            Console.WriteLine();
            InputDevice inputDevice = SelectMidiDevice(InputDevice.GetAll().ToList());
            OutputDevice outputDevice = SelectMidiDevice(OutputDevice.GetAll().ToList());
            if (input1 == "1")
            {
                Console.WriteLine("Super Basic mode: White keys only");
            }
            else if (input1 == "2" || input1 == "3")
            {
                Console.WriteLine("Midi mode: White and black keys");
            }
            int points = 0;
            if (int.TryParse(input1, out _))
            {
                Console.WriteLine("Starting:");
                for (int i = 0; i < 20; i++)
                {
                    points += PlayTheGame(outputDevice, inputDevice, input1);
                }
            }
            inputDevice.Dispose();
            outputDevice.Dispose();
            Console.WriteLine();
            Console.WriteLine($"Results: You scored {points} points. Congratulations!");
            tts.SpeakAsyncCancelAll();
            tts.SpeakAsync($"Results: You scored {points} points. Congratulations!");
            Console.ReadLine();
        }

        private static int PlayTheGame(OutputDevice outputDevice, InputDevice inputDevice, string mode)
        {
            int midiNotePlayed = -1;
            inputDevice.EventReceived += (midisender, midievent) => MidiEventReceived(midisender, midievent, ref midiNotePlayed);
            inputDevice.StartEventsListening();
            outputDevice.EventSent += OutputDevice_EventSent;
            int noteNumber = rng.Next(12);
            double sharpflag = rng.NextDouble();
            string noteToPlay = GetNoteToPlay(mode, ref noteNumber, sharpflag);
            outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)(noteNumber + 60), (SevenBitNumber)127));
            Console.WriteLine(noteToPlay);
            tts.SpeakAsyncCancelAll();
            tts.SpeakAsync(noteToPlay.Replace("#", " Sharp").Replace("b", " Flat"));
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
                    OneSecondBreak();
                    return 0;
                }
            }
            antiCheatTimer.Dispose();
            outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)(noteNumber + 60), (SevenBitNumber)127));
            Timer timer = new Timer
            {
                Interval = 1000,
                Enabled = true
            };
            int pointsToAdd = 5;
            timer.Elapsed += (timersender, timerevent) => ReducePointsGiven(timersender, timerevent, ref pointsToAdd);
            while (true)
            {
                // waiting for MIDI input
                if (midiNotePlayed != -1)
                {
                    int pointsScored = NotePlayed(GetNotePlayed(mode, midiNotePlayed, sharpflag), noteToPlay, pointsToAdd);
                    timer.Dispose();
                    return pointsScored;
                }
            }
        }

        private static string GetNoteToPlay(string mode, ref int noteNumber, double sharpflag)
        {
            string noteToPlay = mode != "2" ? naturalnotes[noteNumber] : sharpflag < 0.5 ? sharpnotes[noteNumber] : flatnotes[noteNumber];
            while (noteToPlay == null)
            {
                noteNumber = rng.Next(12);
                noteToPlay = naturalnotes[noteNumber];
            }
            if (mode == "3" && sharpflag < 0.5)
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
        private static string GetNotePlayed(string mode, int noteNumber, double sharpflag)
        {
            string notePlayed = mode != "2" || sharpflag < 0.5 ? sharpnotes[noteNumber] : flatnotes[noteNumber];
            if (mode == "3" && sharpflag < 0.5)
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
            Console.WriteLine($"You played: {midiNotePlayed}");
            Console.WriteLine();
            if (midiNotePlayed == noteToPlay)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Correct! You scored {pointsToAdd} points.");
                Console.ResetColor();
                tts.SpeakAsyncCancelAll();
                tts.SpeakAsync("Correct.");
                points += pointsToAdd;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Incorrect! You were asked to play {noteToPlay}, no points for you this time.");
                Console.ResetColor();
                tts.SpeakAsyncCancelAll();
                tts.SpeakAsync("Incorrect.");
            }
            Console.WriteLine();
            OneSecondBreak();
            return points;
        }

        private static void OneSecondBreak()
        {
            bool oneSecondBreak = true;
            Timer oneSecond = new Timer
            {
                Interval = 1000,
                Enabled = true
            };
            oneSecond.Elapsed += (timersender, timerevent) => ChangeBoolToFalse(timersender, timerevent, ref oneSecondBreak);
            while (oneSecondBreak)
            {

            }
            oneSecond.Dispose();
        }

        private static void OutputDevice_EventSent(object sender, MidiEventSentEventArgs e)
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
                throw new ArgumentNullException("No MIDI device found");
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
