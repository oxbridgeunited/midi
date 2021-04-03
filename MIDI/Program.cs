using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using System.Speech.Synthesis;
using Melanchall.DryWetMidi.Common;

namespace MIDI
{
    class Program
    {
        private static readonly Random rng = new Random();
        private static readonly string[] basicnotes = { "C", null , "D", null, "E", "F", null, "G", null, "A", null, "B" };
        private static readonly string[] sharpnotes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        private static readonly string[] flatnotes = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };

        private static void Main(string[] args)
        {
            SpeechSynthesizer tts = new SpeechSynthesizer();
            int counter = 0;
            int points = 0;
            Console.WriteLine("Note Practice");
            Console.WriteLine("1: Super Basic");
            Console.WriteLine("2: Basic");
            Console.WriteLine("3: Advanced");
            Console.WriteLine("x: exit");
            char input1 = Console.ReadKey().KeyChar;
            Console.WriteLine();
            if (input1 == '1')
            {
                Console.WriteLine("Super Basic Mode");
                Console.WriteLine("White keys only");
                Console.WriteLine("Starting:");
                InputDevice inputDevice = SelectMidiInput(InputDevice.GetAll().ToList());
                OutputDevice outputDevice = SelectMidiOutput(OutputDevice.GetAll().ToList());
                string midiNotePlayed = null;
                inputDevice.EventReceived += (midisender, midievent) => MidiEventReceived(midisender, midievent, ref midiNotePlayed);
                outputDevice.EventSent += OutputDevice_EventSent;
                inputDevice.StartEventsListening();
                while (counter < 20)
                {
                    int noteNumber = 0;
                    bool cheatMode = true;
                    string note = null;
                    while (note == null)
                    {
                        noteNumber = rng.Next(12);
                        note = basicnotes[noteNumber];
                    }
                    Timer antiCheatTimer = new Timer
                    {
                        Interval = 100,
                        Enabled = true
                    };
                    outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)(noteNumber + 60), (SevenBitNumber)127));
                    antiCheatTimer.Elapsed += (antiCheatSender, antiCheatEvent) => ChangeBoolToFalse(antiCheatSender, antiCheatEvent, ref cheatMode);
                    Console.WriteLine(note);
                    tts.SpeakAsyncCancelAll();
                    tts.SpeakAsync(note);
                    while (cheatMode)
                    {
                        if (midiNotePlayed != null)
                        {
                            Console.WriteLine("CHEAT! Wait for the note to come up before pressing anything!");
                            antiCheatTimer.Dispose();
                            goto End;
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
                        if (midiNotePlayed != null)
                        {
                            string displayNote = sharpnotes[int.Parse(midiNotePlayed)];
                            Console.WriteLine($"You played: {displayNote}");
                            Console.WriteLine();
                            if (displayNote == note)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Correct! You scored {pointsToAdd} points.");
                                Console.ResetColor();
                                Console.WriteLine();
                                tts.SpeakAsyncCancelAll();
                                tts.SpeakAsync("Correct.");
                                points += pointsToAdd;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Incorrect! You were asked to play {note}, no points for you this time.");
                                Console.ResetColor();
                                Console.WriteLine();
                                tts.SpeakAsyncCancelAll();
                                tts.SpeakAsync("Incorrect.");
                            }
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
                            break;
                        }
                    }
                    timer.Dispose();
                End:
                    counter += 1;
                    midiNotePlayed = null;
                    note = null;
                }
                inputDevice.Dispose();
                outputDevice.Dispose();
                Console.WriteLine();
                Console.WriteLine($"Results: You scored {points} points. Congratulations!");
                tts.SpeakAsyncCancelAll();
                tts.SpeakAsync($"Results: You scored {points} points. Congratulations!");
                Console.ReadLine();
            }
            else if (input1 == '2' || input1 == '3')
            {
                InputDevice inputDevice = SelectMidiInput(InputDevice.GetAll().ToList());
                string midiNotePlayed = null;
                inputDevice.EventReceived += (midisender, midievent) => MidiEventReceived(midisender, midievent, ref midiNotePlayed);
                inputDevice.StartEventsListening();
                Console.WriteLine("Midi mode: White and black keys");
                Console.WriteLine("Starting:");
                while (counter < 20)
                {
                    bool cheatMode = true;
                    int noteNumber = rng.Next(12);
                    bool sharpFlag = rng.NextDouble() < 0.5;
                    string note = sharpFlag ? sharpnotes[noteNumber] : flatnotes[noteNumber];
                    Console.WriteLine(note);
                    tts.SpeakAsyncCancelAll();
                    tts.SpeakAsync(note.Replace("#", " sharp").Replace("b", " flat"));
                    Timer antiCheatTimer = new Timer
                    {
                        Interval = 100,
                        Enabled = true
                    };
                    antiCheatTimer.Elapsed += (antiCheatSender, antiCheatEvent) => ChangeBoolToFalse(antiCheatSender, antiCheatEvent, ref cheatMode);
                    while (cheatMode)
                    {
                        if (midiNotePlayed != null)
                        {
                            Console.WriteLine("CHEAT! Wait for the note to come up before pressing anything!");
                            antiCheatTimer.Dispose();
                            goto End;
                        }
                    }
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
                        if (midiNotePlayed != null)
                        {
                            int notePlayedInt = int.Parse(midiNotePlayed);
                            string displayNote = sharpFlag ? sharpnotes[notePlayedInt] : flatnotes[notePlayedInt];
                            Console.WriteLine($"You played: {displayNote}");
                            Console.WriteLine();
                            if (displayNote == note)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Correct! You scored {pointsToAdd} points.");
                                Console.ResetColor();
                                Console.WriteLine();
                                tts.SpeakAsyncCancelAll();
                                tts.SpeakAsync("Correct.");
                                points += pointsToAdd;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Incorrect! You were asked to play {note}, no points for you this time.");
                                Console.ResetColor();
                                Console.WriteLine();
                                tts.SpeakAsyncCancelAll();
                                tts.SpeakAsync("Incorrect.");
                            }
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
                            break;
                        }
                    }
                    timer.Dispose();
                    End:
                    counter += 1;
                    midiNotePlayed = null;
                }
                inputDevice.Dispose();
                Console.WriteLine();
                Console.WriteLine($"Results: You scored {points} points. Congratulations!");
                tts.SpeakAsyncCancelAll();
                tts.SpeakAsync($"Results: You scored {points} points. Congratulations!");
                Console.ReadLine();
            }
        }

        private static void OutputDevice_EventSent(object sender, MidiEventSentEventArgs e)
        {
        }

        private static OutputDevice SelectMidiOutput(List<OutputDevice> list)
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
                return input < list.Count() && input >= 0 ? list[input] : null;
            }
            else
            {
                throw new ArgumentNullException("No MIDI device found");
            }
        }

        private static InputDevice SelectMidiInput(List<InputDevice> list)
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
                return input < list.Count() && input >= 0 ? list[input] : null;
            }
            else
            {
                throw new ArgumentNullException("No MIDI device found");
            }
        }

        private static void MidiEventReceived(object sender, MidiEventReceivedEventArgs e, ref string output)
        {
            if (e.Event.EventType == MidiEventType.NoteOn)
            {
                output = (int.Parse(new string(e.Event.ToString().Split('(', ')')[1].TakeWhile(char.IsDigit).ToArray())) % 12).ToString();
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
