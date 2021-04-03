using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Speech.Synthesis;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Common;

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
            string input1 = Console.ReadKey().KeyChar.ToString();
            Console.WriteLine();
            InputDevice inputDevice = SelectMidiDevice(InputDevice.GetAll().ToList());
            OutputDevice outputDevice = SelectMidiDevice(OutputDevice.GetAll().ToList());
            int points = 0;
            if (input1 == "1")
            {
                Console.WriteLine("Super Basic mode: White keys only");
            }
            else if (input1 == "2" || input1 == "3")
            {
                Console.WriteLine("Midi mode: White and black keys");
            }
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
            int noteNumber = -1;
            string noteToPlay = null;
            bool sharpflag = rng.NextDouble() < 0.5;
            if (mode == "1")
            {
                while (noteToPlay == null)
                {
                    noteNumber = rng.Next(12);
                    noteToPlay = naturalnotes[noteNumber];
                }
            }
            else
            {
                noteNumber = rng.Next(12);
                noteToPlay = sharpflag ? sharpnotes[noteNumber] : flatnotes[noteNumber];
            }
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
                    return 0;
                }
            }
            outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)(noteNumber + 60), (SevenBitNumber)127));
            antiCheatTimer.Dispose();
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
                    int pointsScored = NotePlayed(mode == "1" ? naturalnotes[midiNotePlayed] : sharpflag ? sharpnotes[midiNotePlayed] : flatnotes[midiNotePlayed], noteToPlay, pointsToAdd);
                    timer.Dispose();
                    return pointsScored;
                }
            }
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
            return points;
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
                Console.WriteLine("Select your MIDI device:");
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
