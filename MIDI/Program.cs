using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;

namespace MIDI
{
    class Program
    {
        private static Random rng = new Random();
        private static readonly string[] basicnotes = { "C", null , "D", null, "E", "F", null, "G", null, "A", null, "B" };
        private static readonly string[] sharpnotes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        private static readonly string[] flatnotes = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };

        private static void Main(string[] args)
        {
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
                InputDevice inputDevice = SelectMidiDevice(InputDevice.GetAll().ToList());
                string midiNotePlayed = null;
                inputDevice.EventReceived += (midisender, midievent) => MidiEventReceived(midisender, midievent, ref midiNotePlayed);
                inputDevice.StartEventsListening();
                while (counter < 20)
                {
                    bool cheatMode = true;
                    Timer antiCheatTimer = new Timer
                    {
                        Interval = 100,
                        Enabled = true
                    };
                    antiCheatTimer.Elapsed += (antiCheatSender, antiCheatEvent) => ChangeBoolToFalse(antiCheatSender, antiCheatEvent, ref cheatMode);
                    string note = null;
                    while (note == null)
                    {
                        int noteNumber = rng.Next(12);
                        note = basicnotes[noteNumber];
                    }
                    Console.WriteLine(note);
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
                            string displayNote = sharpnotes[int.Parse(midiNotePlayed)];
                            Console.WriteLine($"You played: {displayNote}");
                            Console.WriteLine();
                            if (displayNote == note)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Correct! You scored {pointsToAdd} points.");
                                Console.ResetColor();
                                Console.WriteLine();
                                points += pointsToAdd;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Incorrect! You were asked to play {note}, no points for you this time.");
                                Console.ResetColor();
                                Console.WriteLine();
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
                Console.WriteLine();
                Console.WriteLine($"Results: You scored {points} points. Congratulations!");
                Console.ReadLine();
            }
            else if (input1 == '2' || input1 == '3')
            {
                InputDevice inputDevice = SelectMidiDevice(InputDevice.GetAll().ToList());
                string midiNotePlayed = null;
                inputDevice.EventReceived += (midisender, midievent) => MidiEventReceived(midisender, midievent, ref midiNotePlayed);
                inputDevice.StartEventsListening();
                Console.WriteLine("Midi mode: White and black keys");
                Console.WriteLine("Starting:");
                while (counter < 20)
                {
                    bool cheatMode = true;
                    Timer antiCheatTimer = new Timer
                    {
                        Interval = 100,
                        Enabled = true
                    };
                    antiCheatTimer.Elapsed += (antiCheatSender, antiCheatEvent) => ChangeBoolToFalse(antiCheatSender, antiCheatEvent, ref cheatMode);
                    int noteNumber = rng.Next(12);
                    bool sharpFlag = rng.NextDouble() < 0.5;
                    string note = sharpFlag ? sharpnotes[noteNumber] : flatnotes[noteNumber];
                    Console.WriteLine(note);
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
                                points += pointsToAdd;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Incorrect! You were asked to play {note}, no points for you this time.");
                                Console.ResetColor();
                                Console.WriteLine();
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
                Console.ReadLine();
            }
        }

        private static InputDevice SelectMidiDevice(List<InputDevice> list)
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
