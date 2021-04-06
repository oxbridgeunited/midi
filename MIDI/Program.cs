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
        private static readonly string ScoreAttackASCII = "  ____                                      _      _     _                    _    \n / ___|    ___    ___    _ __    ___       / \\    | |_  | |_    __ _    ___  | | __\n \\___ \\   / __|  / _ \\  | '__|  / _ \\     / _ \\   | __| | __|  / _` |  / __| | |/ /\n  ___) | | (__  | (_) | | |    |  __/    / ___ \\  | |_  | |_  | (_| | | (__  |   < \n |____/   \\___|  \\___/  |_|     \\___|   /_/   \\_\\  \\__|  \\__|  \\__,_|  \\___| |_|\\_\\\n";

        private static void Main()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("WELCOME TO MIDI PRACTICE");
            Console.WriteLine(ScoreAttackASCII);
            Console.WriteLine("Select mode by typing number (1 to 5) and hitting ENTER.\n\n");
            Console.ResetColor();
            Console.WriteLine("1: Numpad Mode\n    Numbers 0 to 9, no MIDI keyboard required!\n");
            Console.WriteLine("2: Basic Mode\n    White keys only, notes are always natural.\n");
            Console.WriteLine("3: Regular Mode\n    White keys are always natural, and black keys are always sharp.\n");
            Console.WriteLine("4: Semi-Advanced Mode\n    White keys are always natural, and black keys can be either sharp or flat.\n");
            Console.WriteLine("5: Advanced Mode\n    White keys are NOT always natural, and black keys can be either sharp or flat.\n");
            Console.WriteLine("x: Exit\n");
            bool game = int.TryParse(Console.ReadLine(), out int input); // get the game mode the user selected
            switch (input)
            {
                case 1:
                    Console.WriteLine("Numpad mode selected. \nScore Attack - 30 rounds.");
                    break;
                case 2:
                    Console.WriteLine("Basic mode selected. \nScore Attack - 20 rounds.");
                    break;
                case 3:
                    Console.WriteLine("Regular mode selected. \nScore Attack - 20 rounds.");
                    break;
                case 4:
                    Console.WriteLine("Semi-Advanced mode selected. \nScore Attack - 20 rounds.");
                    break;
                case 5:
                    Console.WriteLine("Advanced mode selected. \nScore Attack - 20 rounds.");
                    break;
                default:
                    Console.WriteLine("Press ENTER to exit.");
                    break;
            }
            while (game) // we're playing
            {
                GameCode(input); // the game is in here
                Console.WriteLine("Want to play again? Type Y to replay the game.");
                if (Console.ReadLine().ToLower() != "y") // if the user doesn't want to play
                {
                    game = false; // change the game bool to false
                    Console.WriteLine("\nPress ENTER to exit.");
                }
            }
            Console.ReadLine();
        }
        private static void GameCode(int input)
        {
            OutputDevice outputDevice = SelectMidiDevice(OutputDevice.GetAll().ToList()); // get the output device
            outputDevice.EventSent += EmptyEvent;
            int rounds = input == 1 ? 30 : 20; // 20 rounds standard, but 30 for Numpad mode
            InputDevice inputDevice = null;
            if (input > 1) // MIDI mode selected
            {
                inputDevice = SelectMidiDevice(InputDevice.GetAll().ToList()); // get input device for MIDI mode
                if (inputDevice == default) // if no input devices, default to Numpad mode
                {
                    input = 1;
                    rounds = 30;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("A MIDI keyboard is required for this mode! Starting Numpad Mode instead.\nPress ENTER to continue.");
                    Console.ResetColor();
                    Console.ReadLine();
                }
            }
            PreGameCountDown(); // get ready...
            int points = 0;
            for (int i = 0; i < rounds; i++) // and play the game!
            {
                points += input > 1 // pick the correct game
                    ? MIDIMode(outputDevice, inputDevice, input) : NumpadMode(); // and add points if they get the note correct
            }
            if (inputDevice != null) // always dispose your IDisposables when you're done with them
            {
                inputDevice.Dispose();
            }
            Console.WriteLine();
            if (points == 5 * rounds) // Perfect Score Announcement
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Results: You achieved a perfect score with {points} points! Congratulations!");
                Console.ResetColor();
                tts.SpeakAsyncCancelAll();
                tts.SpeakAsync($"Perfect Score! Results: You achieved a perfect score with {points} points. Congratulations!");
                outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)60, (SevenBitNumber)127)); // C
                outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)64, (SevenBitNumber)127)); // E
                outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)67, (SevenBitNumber)127)); // G
                Pause(100); // C Major for a short period
                outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)60, (SevenBitNumber)127));
                outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)64, (SevenBitNumber)127));
                outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)67, (SevenBitNumber)127));
                Pause(50); // short pause
                outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)60, (SevenBitNumber)127)); // C
                outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)64, (SevenBitNumber)127)); // E
                outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)67, (SevenBitNumber)127)); // G
                Pause(1000); // C Major for a long period
                outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)60, (SevenBitNumber)127));
                outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)64, (SevenBitNumber)127));
                outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)67, (SevenBitNumber)127));
            }
            else // Regular Score Announcement
            {
                Console.WriteLine($"Results: You scored {points}/{5 * rounds} points.");
                tts.SpeakAsyncCancelAll();
                tts.SpeakAsync($"Results: You scored {points} out of a maximum of {5 * rounds} points.");
            }
            outputDevice.Dispose(); // always dispose your IDisposables when you're done with them
        }
        private static void PreGameCountDown()
        {
            int preGameCountDown = 3; // 3 second countdown
            while (preGameCountDown > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"Starting: in {preGameCountDown}");
                Console.ResetColor();
                preGameCountDown -= 1;
                Pause(1000); // 1 second each
            }
        }
        private static int NumpadMode()
        {
            string numToPlay = rng.Next(10).ToString(); // get number to input
            tts.SpeakAsyncCancelAll();
            tts.SpeakAsync($"{numToPlay}. . . . . . . . . . . .{numToPlay}."); // Using lots of fullstops to generate gap between repeat
            Pause(500);
            Console.WriteLine(numToPlay + "\n");
            Timer antiCheatTimer = new Timer
            { 
                Interval = 100,
                Enabled = true
            };
            bool cheatMode = true; // no cheaters here
            antiCheatTimer.Elapsed += (antiCheatSender, antiCheatEvent) => ChangeBoolToFalse(antiCheatSender, antiCheatEvent, ref cheatMode);
            string numPlayed = null;
            while (cheatMode)
            {
                if (numPlayed != null) // if you get caught cheating
                {
                    Console.WriteLine("CHEAT! Wait for the note to come up before pressing anything!");
                    antiCheatTimer.Dispose(); // always dispose your IDisposables when you're done with them
                    Pause(1000);
                    return 0; // no points for cheaters
                }
            }
            antiCheatTimer.Dispose(); // always dispose your IDisposables when you're done with them
            Timer timer = new Timer
            {
                Interval = 500,
                Enabled = true
            };
            int pointsToAdd = 5; // perfect score
            timer.Elapsed += (timersender, timerevent) => ReducePointsGiven(timersender, timerevent, ref pointsToAdd);
            numPlayed = Console.ReadKey().KeyChar.ToString(); // get the input
            Console.WriteLine();
            timer.Dispose(); // always dispose your IDisposables when you're done with them
            return NotePlayed(numPlayed, numToPlay, pointsToAdd); // check if they match
        }
        private static int MIDIMode(OutputDevice outputDevice, InputDevice inputDevice, int mode)
        {
            int midiNotePlayed = -1;
            inputDevice.EventReceived += (midisender, midievent) => MidiEventReceived(midisender, midievent, ref midiNotePlayed);
            inputDevice.StartEventsListening(); // turn on the MIDI device
            int noteNumber = rng.Next(12); // get note to play
            double sharpflag = rng.NextDouble(); // used in some modes to modify the note and make the game harder
            string noteToPlay = GetNoteToPlay(mode, ref noteNumber, sharpflag); // turns the note number and uses the sharpflag to get the note to play
            outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)(noteNumber + 60), (SevenBitNumber)127)); // plays the note to play
            tts.SpeakAsyncCancelAll();
            tts.SpeakAsync(noteToPlay.Replace("#", " Sharp").Replace("b", " Flat"));
            Console.WriteLine(noteToPlay + "\n");
            Timer antiCheatTimer = new Timer
            {
                Interval = 100,
                Enabled = true
            };
            bool cheatMode = true; // no cheaters here
            antiCheatTimer.Elapsed += (antiCheatSender, antiCheatEvent) => ChangeBoolToFalse(antiCheatSender, antiCheatEvent, ref cheatMode);
            while (cheatMode)
            {
                if (midiNotePlayed != -1) // if you get caught cheating
                {
                    Console.WriteLine("CHEAT! Wait for the note to come up before pressing anything!");
                    antiCheatTimer.Dispose(); // always dispose your IDisposables when you're done with them
                    outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)(noteNumber + 60), (SevenBitNumber)127));
                    Pause(1000);
                    return 0; // no points for cheaters
                }
            }
            antiCheatTimer.Dispose(); // always dispose your IDisposables when you're done with them
            outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)(noteNumber + 60), (SevenBitNumber)127));
            Timer timer = new Timer
            {
                Interval = 900,
                Enabled = true
            };
            int pointsToAdd = 5; // perfect score
            timer.Elapsed += (timersender, timerevent) => ReducePointsGiven(timersender, timerevent, ref pointsToAdd);
            while (midiNotePlayed == -1)
            {
                // waiting for MIDI input
            }
            Console.WriteLine(); // once the input is received
            timer.Dispose(); // always dispose your IDisposables when you're done with them
            return NotePlayed(GetNotePlayed(mode, midiNotePlayed, sharpflag), noteToPlay, pointsToAdd); // check if they match
        }
        private static string GetNoteToPlay(int mode, ref int noteNumber, double sharpflag)
        {
            string noteToPlay = mode == 2 || mode == 5 // if basic or advanced mode
                ? naturalnotes[noteNumber] // get a natural note
                : mode == 4 && sharpflag >= 0.5 // otherwise 1/2 of the time in semi advanced mode
                ? flatnotes[noteNumber] : sharpnotes[noteNumber]; // get a flat note, otherwise get a sharp note
            while (noteToPlay == null) // if the natural note is not natural
            {
                noteNumber = rng.Next(12); // reroll
                noteToPlay = naturalnotes[noteNumber]; // and hope this one is natural
            }
            if (mode == 5 && sharpflag < 0.5) // for advanced mode 1/2 of the time do 1 of 2 things
            {
                if (sharpflag < 0.25) // 1/2 of the time
                {
                    noteNumber = (noteNumber + 1) % 12; // add a semitone
                    return noteToPlay + "#"; // and the sharp symbol
                }
                else // the other half of the time
                {
                    noteNumber = (noteNumber + 11) % 12; // remove a semitone
                    return noteToPlay + "b"; // and the flat symbol
                }
            }
            return noteToPlay;
        }
        private static string GetNotePlayed(int mode, int noteNumber, double sharpflag)
        {
            string notePlayed = mode != 4 || sharpflag < 0.5 // check if GetNoteToPlay used flatnotes
                ? sharpnotes[noteNumber] : flatnotes[noteNumber]; // if it did, use flatnotes, otherwise use sharpnotes (natural is a subset of this)
            if (mode == 5 && sharpflag < 0.5) // reverse engineering the advanced mode process from GetNoteToPlay
            {
                notePlayed = sharpflag < 0.25 // we added/removed a semitone so we need to un-add/un-remove it
                    ? sharpnotes[(noteNumber + 11) % 12] + "#" : flatnotes[(noteNumber + 1) % 12] + "b"; // and add the sharp/flat symbol
                if (notePlayed.Contains("##")) // if we added a sharp symbol to a sharp then we went too far
                {
                    return sharpnotes[noteNumber];
                }
                if (notePlayed.Contains("bb")) // if we added a flat symbol to a flat then we went too far
                {
                    return flatnotes[noteNumber];
                }
            }
            return notePlayed;
        }
        private static int NotePlayed(string notePlayed, string noteToPlay, int pointsToAdd)
        {
            int points = 0;
            Console.WriteLine($"You hit: {notePlayed}\n");
            if (notePlayed == noteToPlay) // check if the note played matches the expected note
            {
                Console.ForegroundColor = pointsToAdd == 5 ? ConsoleColor.Yellow : ConsoleColor.Green; // yellow if perfect, otherwise green
                Console.WriteLine($"Correct! You scored {pointsToAdd} points.\n");
                Console.ResetColor();
                tts.SpeakAsyncCancelAll();
                points += pointsToAdd;
            }
            else // if it is wrong
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Incorrect! You were asked to hit {noteToPlay}, no points for you this time.\n");
                Console.ResetColor();
                tts.SpeakAsyncCancelAll();
                tts.SpeakAsync("Incorrect.");
                Pause(800); // give them some breathing time
            }
            Pause(400);
            return points;
        }
        private static void Pause(int interval) // yeah we do a lot of pausing
        {
            bool paused = true;
            Timer timer = new Timer
            {
                Interval = interval, // how long to pause for
                Enabled = true
            };
            timer.Elapsed += (timersender, timerevent) => ChangeBoolToFalse(timersender, timerevent, ref paused);
            while (paused)
            {
                // do nothing while paused
            }
            timer.Dispose(); // always dispose your IDisposables when you're done with them
        }
        private static void EmptyEvent(object sender, MidiEventSentEventArgs e)
        {
            // do nothing
        }
        private static T SelectMidiDevice<T>(IList<T> list) // used for input and output devices
        {
            if (list.Count() == 1)
            {
                return list[0]; // there's only 1 so just use that one
            }
            else if (list.Count > 1)
            {
                string[] typeOfDevice = Regex.Split(typeof(T).Name, @"(?<!^)(?=[A-Z])"); // regex necessary because duplicated code is bad
                Console.WriteLine($"Select your MIDI {typeOfDevice[0]} {typeOfDevice[1]}:");
                for (int i = 0; i < list.Count(); i++) // list out all the devices, if we didn't need to use the index this could be a foreach
                {
                    Console.WriteLine($"{i}: {list[i]}");
                }
                int input = int.Parse(Console.ReadLine()); // get the index selected
                return input < list.Count() && input >= 0 // return the selected device
                    ? list[input] : SelectMidiDevice(list); // if they fuck up then they can go in to recursion hell
            }
            else // if there's no MIDI devices
            {
                return default; // can't return null in a generic method
            }
        }
        private static void MidiEventReceived(object sender, MidiEventReceivedEventArgs e, ref int output)
        {
            if (e.Event.EventType == MidiEventType.NoteOn) // if the input is the user pressing a note
            { // get the note number out of the wall of text
                output = int.Parse(new string(e.Event.ToString().Split('(', ')')[1].TakeWhile(char.IsDigit).ToArray())) % 12;
            }
        }
        private static void ReducePointsGiven(object sender, ElapsedEventArgs e, ref int points)
        {
            if (points > 0) // don't go negative
            {
                points -= 1;
            }
        }
        private static void ChangeBoolToFalse(object sender, ElapsedEventArgs e, ref bool boolToChange) // self-explanatory
        {
            boolToChange = false;
        }
    }
}
