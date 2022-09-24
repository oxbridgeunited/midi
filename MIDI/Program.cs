using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

Random rng = new();
SpeechSynthesizer tts = new();
ManualResetEvent resetEvent = new(false);
string[] naturalnotes = { "C", null, "D", null, "E", "F", null, "G", null, "A", null, "B" };
string[] sharpnotes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
string[] flatnotes = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };
string ScoreAttackASCII = "  ____                                      _      _     _                    _    \n / ___|    ___    ___    _ __    ___       / \\    | |_  | |_    __ _    ___  | | __\n \\___ \\   / __|  / _ \\  | '__|  / _ \\     / _ \\   | __| | __|  / _` |  / __| | |/ /\n  ___) | | (__  | (_) | | |    |  __/    / ___ \\  | |_  | |_  | (_| | | (__  |   < \n |____/   \\___|  \\___/  |_|     \\___|   /_/   \\_\\  \\__|  \\__|  \\__,_|  \\___| |_|\\_\\\n";
string EndlessModeASCII = "   _____               _   _                        __  __               _        \n  | ____|  _ __     __| | | |   ___   ___   ___    |  \\/  |   ___     __| |   ___ \n  |  _|   | '_ \\   / _` | | |  / _ \\ / __| / __|   | |\\/| |  / _ \\   / _` |  / _ \\\n  | |___  | | | | | (_| | | | |  __/ \\__ \\ \\__ \\   | |  | | | (_) | | (_| | |  __/\n  |_____| |_| |_|  \\__,_| |_|  \\___| |___/ |___/   |_|  |_|  \\___/   \\__,_|  \\___|\n";

// ASCII no 0 - 9
string ASCII0 = "\n\n    █████   \n   ██   ██  \n  ██     ██ \n  ██     ██ \n  ██     ██ \n   ██   ██  \n    █████   \n";
string ASCII1 = "\n\n      ██   \n    ████   \n      ██   \n      ██   \n      ██   \n      ██   \n    ██████ \n";
string ASCII2 = "\n\n   ███████  \n  ██     ██ \n         ██ \n   ███████  \n  ██        \n  ██        \n  █████████ \n";
string ASCII3 = "\n\n   ███████  \n  ██     ██ \n         ██ \n   ███████  \n         ██ \n  ██     ██ \n   ███████  \n";
string ASCII4 = "\n\n  ██        \n  ██    ██  \n  ██    ██  \n  ██    ██  \n  █████████ \n        ██  \n        ██  \n";
string ASCII5 = "\n\n  ████████ \n  ██       \n  ██       \n  ███████  \n        ██ \n  ██    ██ \n   ██████  \n";
string ASCII6 = "\n\n   ███████  \n  ██     ██ \n  ██        \n  ████████  \n  ██     ██ \n  ██     ██ \n   ███████  \n";
string ASCII7 = "\n\n  ████████ \n  ██    ██ \n      ██   \n     ██    \n    ██     \n    ██     \n    ██     \n";
string ASCII8 = "\n\n   ███████  \n  ██     ██ \n  ██     ██ \n   ███████  \n  ██     ██ \n  ██     ██ \n   ███████  \n";
string ASCII9 = "\n\n   ███████  \n  ██     ██ \n  ██     ██ \n   ████████ \n         ██ \n  ██     ██ \n   ███████  \n";

// ASCII CDEFGAB
// If Natural, 's'+'#'+'f'+'b' are removed.
// If Sharp,   's' becomes ' ', '#' becomes '█'. 'f'+'b' are removed.
// If Flat,    'f' becomes ' ', 'b' becomes '█'. 's'+'#' are removed.
string ASCIIC = "\n\n   ███████ sss##s##ssfbbfffff\n  ██     ██sss##s##ssfbbfffff\n  ██       s#########fbbfffff\n  ██       sss##s##ssfbbbbbbf\n  ██       s#########fbbfffbb\n  ██     ██sss##s##ssfbbfffbb\n   ███████ sss##s##ssfbbbbbbf\n\n";
string ASCIID = "\n\n  ████████ sss##s##ssfbbfffff\n  ██     ██sss##s##ssfbbfffff\n  ██     ██s#########fbbfffff\n  ██     ██sss##s##ssfbbbbbbf\n  ██     ██s#########fbbfffbb\n  ██     ██sss##s##ssfbbfffbb\n  ████████ sss##s##ssfbbbbbbf\n\n";
string ASCIIE = "\n\n  █████████sss##s##ssfbbfffff\n  ██       sss##s##ssfbbfffff\n  ██       s#########fbbfffff\n  ███████  sss##s##ssfbbbbbbf\n  ██       s#########fbbfffbb\n  ██       sss##s##ssfbbfffbb\n  █████████sss##s##ssfbbbbbbf\n\n";
string ASCIIF = "\n\n  █████████sss##s##ssfbbfffff\n  ██       sss##s##ssfbbfffff\n  ██       s#########fbbfffff\n  ███████  sss##s##ssfbbbbbbf\n  ██       s#########fbbfffbb\n  ██       sss##s##ssfbbfffbb\n  ██       sss##s##ssfbbbbbbf\n\n";
string ASCIIG = "\n\n   ███████ sss##s##ssfbbfffff\n  ██     ██sss##s##ssfbbfffff\n  ██       s#########fbbfffff\n  ██  █████sss##s##ssfbbbbbbf\n  ██     ██s#########fbbfffbb\n  ██     ██sss##s##ssfbbfffbb\n   ███████ sss##s##ssfbbbbbbf\n\n";
string ASCIIA = "\n\n     ███   sss##s##ssfbbfffff\n    ██ ██  sss##s##ssfbbfffff\n   ██   ██ s#########fbbfffff\n  ██     ██sss##s##ssfbbbbbbf\n  █████████s#########fbbfffbb\n  ██     ██sss##s##ssfbbfffbb\n  ██     ██sss##s##ssfbbbbbbf\n\n";
string ASCIIB = "\n\n  ████████ sss##s##ssfbbfffff\n  ██     ██sss##s##ssfbbfffff\n  ██     ██s#########fbbfffff\n  ████████ sss##s##ssfbbbbbbf\n  ██     ██s#########fbbfffbb\n  ██     ██sss##s##ssfbbfffbb\n  ████████ sss##s##ssfbbbbbbf\n\n";

// ASCII Correct and Incorrect
string ASCIIyes = "           ██\n          ██ \n         ██  \n        ██   \n  ██   ██    \n   ██ ██     \n    ███      \n";
string ASCIIno = "   ██     ██ \n    ██   ██  \n     ██ ██   \n      ███    \n     ██ ██   \n    ██   ██  \n   ██     ██ \n";

Console.WriteLine("WELCOME TO MIDI PRACTICE\n");
Console.WriteLine("Select the game type by typing a number (1 to 4) and hitting ENTER.\n\n");
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("1: Score Attack");
Console.WriteLine("2: Score Attack - Text To Speech DISABLED");
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("3: Endless Mode");
Console.WriteLine("4: Endless Mode - Text To Speech DISABLED");
Console.ResetColor();
bool endlessMode = EndlessOrScore();
Console.ForegroundColor = endlessMode ? ConsoleColor.Cyan : ConsoleColor.Yellow;
Console.WriteLine(endlessMode ? EndlessModeASCII : ScoreAttackASCII);
Console.WriteLine("Select the difficulty by typing a number (1 to 5) and hitting ENTER.\n\n");
Console.ResetColor();
Console.WriteLine("1: Numpad\n    Numbers 0 to 9, no MIDI keyboard required!\n");
Console.WriteLine("2: Basic\n    White keys only, notes are always natural.\n");
Console.WriteLine("3: Regular\n    White keys are always natural, and black keys are always sharp.\n");
Console.WriteLine("4: Semi-Advanced\n    White keys are always natural, and black keys can be either sharp or flat.\n");
Console.WriteLine("5: Advanced\n    White keys are NOT always natural, and black keys can be either sharp or flat.\n");
Console.WriteLine("x: Exit\n");
bool game = int.TryParse(Console.ReadLine(), out int gameDifficulty); // get the game difficulty the user selected
string modeString = endlessMode ? "Endless Mode." : "Score Attack - 20 rounds.";
switch (gameDifficulty)
{
    case 1:
        Console.WriteLine("Numpad selected. \n" + (endlessMode ? modeString : "Score Attack - 30 rounds."));
        break;
    case 2:
        Console.WriteLine("Basic selected. \n" + modeString);
        break;
    case 3:
        Console.WriteLine("Regular selected. \n" + modeString);
        break;
    case 4:
        Console.WriteLine("Semi-Advanced selected. \n" + modeString);
        break;
    case 5:
        Console.WriteLine("Advanced selected. \n" + modeString);
        break;
    default:
        Console.WriteLine("Press ENTER to exit.");
        break;
}
OutputDevice outputDevice = null;
InputDevice inputDevice = null;
if (game)
{
    outputDevice = SelectMidiDevice(OutputDevice.GetAll().ToList()); // get output device
    outputDevice.EventSent += EmptyEvent;
    if (gameDifficulty > 1)
    {
        inputDevice = SelectMidiDevice(InputDevice.GetAll().ToList()); // get input device for MIDI mode
        if (inputDevice == default) // if no input devices, default to Numpad
        {
            gameDifficulty = 1;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("A MIDI keyboard is required for this difficulty! Starting Numpad instead.\nPress ENTER to continue.");
            Console.ResetColor();
            Console.ReadLine();
        }
    }
}
while (game) // we're playing
{
    await GameCode(endlessMode, gameDifficulty, outputDevice, inputDevice); // the game is in here
    Console.WriteLine("Want to play again? Type Y to replay the game.");
    if (!Console.ReadLine().Equals("y", StringComparison.OrdinalIgnoreCase)) // if the user doesn't want to play
    {
        game = false; // change the game bool to false
        Console.WriteLine("\nPress ENTER to exit.");
    }
    tts.SpeakAsyncCancelAll();
}
if (inputDevice != null)
{
    inputDevice.Dispose(); // always dispose your IDisposables when you're done with them
}
if (outputDevice != null)
{
    outputDevice.Dispose(); // always dispose your IDisposables when you're done with them
}
Console.ReadLine();
bool EndlessOrScore()
{
    string gameType = Console.ReadLine();
    if (gameType is "2" or "4") // Disable Text-To-Speech
    {
        tts.Pause();
    }
    if (gameType is "1" or "2") // Score Attack
    {
        return false;
    }
    else if (gameType is "3" or "4") // Endless Mode
    {
        return true;
    }
    Console.WriteLine("Type a number from 1 to 4");
    return EndlessOrScore();
}
async Task GameCode(bool endlessMode, int input, OutputDevice outputDevice, InputDevice inputDevice = null)
{
    PreGameCountDown(3); // get ready...
    int points = 0;
    int plays = 0;
    int rounds = input == 1 ? 30 : 20; // 20 rounds standard, but 30 for Numpad
    while (plays < rounds)
    {
        tts.SpeakAsyncCancelAll();
        points += input > 1 // pick the correct game
            ? await MIDIMode(outputDevice, inputDevice, input, endlessMode) : NumpadMode(endlessMode); // and add points if they get the note correct
        plays += 1;
        if (endlessMode)
        {
            Console.WriteLine($"Accuracy: {(int)Math.Round((double)points / plays * 100)}% ({points}/{plays})");
            rounds += 1; // In Endless Mode, keep incrementing the round count to make it impossible to reach
        }
        Pause(400);
    }
    Console.WriteLine();
    int perfectScore = endlessMode ? plays : plays * 5;
    if (points == perfectScore) // Perfect Score Announcement
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Results: You achieved a perfect score with {points} points! Congratulations!");
        Console.ResetColor();
        tts.SpeakAsyncCancelAll();
        tts.SpeakAsync($"Perfect Score! Results: You achieved a perfect score with {points} points. Congratulations!");
        await PlayMIDI(outputDevice, 0, 100); // C Major for a short period
        await PlayMIDI(outputDevice, 4, 100); // E
        await PlayMIDI(outputDevice, 7, 100); // G
        Pause(50); // short pause
        await PlayMIDI(outputDevice, 0, 1000); // C Major for a long period
        await PlayMIDI(outputDevice, 4, 1000); // E
        await PlayMIDI(outputDevice, 7, 1000); // G
    }
    else // Regular Score Announcement
    {
        Console.WriteLine($"Results: You scored {points}/{perfectScore} points.");
        tts.SpeakAsyncCancelAll();
        tts.SpeakAsync($"Results: You scored {points} out of a maximum of {perfectScore} points.");
    }
}
void PreGameCountDown(int seconds)
{
    for (int i = seconds; i > 0; i--)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Starting: in {i}");
        Console.ResetColor();
        Pause(1000); // 1 second each
    }
}
int NumpadMode(bool endlessMode)
{
    Stopwatch antiCheatTimer = new();
    antiCheatTimer.Start();
    string numToPlay = rng.Next(10).ToString(); // get number to input
    tts.SpeakAsync($"{numToPlay}. . . . . . . . . . . .{numToPlay}."); // Using lots of fullstops to generate gap between repeat
    string[] NumberToASCII = { ASCII0, ASCII1, ASCII2, ASCII3, ASCII4, ASCII5, ASCII6, ASCII7, ASCII8, ASCII9 };
    Console.WriteLine(NumberToASCII[int.Parse(numToPlay)] + "\n"); // get the number as ASCII and display it
    string numPlayed = Console.ReadKey().KeyChar.ToString(); // get the input
    antiCheatTimer.Stop();
    long timeElapsed = antiCheatTimer.ElapsedMilliseconds;
    if (timeElapsed < 100) // if you get caught cheating
    {
        Console.WriteLine("CHEAT! Wait for the note to come up before pressing anything!");
        Pause(2000);
        return 0; // no points for cheaters
    }
    Console.WriteLine();
    int pointsToAdd = endlessMode ? 1 : (int)Math.Max(0, 5 - ((timeElapsed - 100) / 600));
    return NotePlayed(numPlayed, numToPlay, pointsToAdd, endlessMode); // check if they match
}
async Task<int> MIDIMode(OutputDevice outputDevice, InputDevice inputDevice, int version, bool endlessMode)
{
    resetEvent.Reset();
    int midiNotePlayed = -1;
    inputDevice.EventReceived += (midisender, midievent) => MidiEventReceived(midisender, midievent, ref midiNotePlayed);
    inputDevice.StartEventsListening(); // turn on the MIDI device
    Stopwatch antiCheatTimer = new();
    antiCheatTimer.Start();
    int noteNumber = rng.Next(12); // get note to play
    await PlayMIDI(outputDevice, noteNumber, 1000);
    double sharpflag = rng.NextDouble(); // used in some modes to modify the note and make the game harder
    string noteToPlay = GetNoteToPlay(version, ref noteNumber, sharpflag); // turns the note number and uses the sharpflag to get the note to play
    tts.SpeakAsync(noteToPlay.Replace("#", " Sharp").Replace("b", " Flat"));
    string[] noteToASCII = { ASCIIC, null, ASCIID, null, ASCIIE, ASCIIF, null, ASCIIG, null, ASCIIA, null, ASCIIB };
    string noteASCII = noteToASCII[noteNumber];
    if (noteToPlay.Contains('#')) // display the note using ASCII art
    {
        // Sharp
        noteASCII = noteToASCII[(noteNumber + 11) % 12];
        Console.WriteLine(noteASCII.Replace("s", " ").Replace("f", "").Replace("b", "").Replace("#", "█"));
    }
    else if (noteToPlay.Contains('b'))
    {
        // Flat
        noteASCII = noteToASCII[(noteNumber + 1) % 12];
        Console.WriteLine(noteASCII.Replace("s", "").Replace("#", "").Replace("f", " ").Replace("b", "█"));
    }
    else
    {
        // Natural
        Console.WriteLine(noteASCII.Replace("s", "").Replace("#", "").Replace("f", "").Replace("b", ""));
    }
    resetEvent.WaitOne();
    antiCheatTimer.Stop();
    outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)(noteNumber + 60), (SevenBitNumber)127)); // turn off note if still playing
    long timeElapsed = antiCheatTimer.ElapsedMilliseconds;
    if (timeElapsed < 100) // if you get caught cheating
    {
        Console.WriteLine("CHEAT! Wait for the note to come up before pressing anything!");
        Pause(2000);
        return 0; // no points for cheaters
    }
    Console.WriteLine(); // once the input is received
    int pointsToAdd = endlessMode ? 1 : (int)Math.Max(0, 5 - ((timeElapsed - 100) / 1000));
    return NotePlayed(GetNotePlayed(version, midiNotePlayed, sharpflag), noteToPlay, pointsToAdd, endlessMode); // check if they match
}
string GetNoteToPlay(int mode, ref int noteNumber, double sharpflag)
{
    string noteToPlay = mode is 2 or 5 // if basic or advanced mode
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
string GetNotePlayed(int mode, int noteNumber, double sharpflag)
{
    string notePlayed = mode == 4 && sharpflag >= 0.5 // check if GetNoteToPlay used flatnotes
        ? flatnotes[noteNumber] : sharpnotes[noteNumber]; // if it did, use flatnotes, otherwise use sharpnotes (natural is a subset of this)
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
int NotePlayed(string notePlayed, string noteToPlay, int pointsToAdd, bool endlessMode)
{
    tts.SpeakAsyncCancelAll(); // stop TTS once a note is played
    Console.WriteLine($"You hit: {notePlayed}\n");
    int points = 0;
    if (notePlayed == noteToPlay) // check if the note played matches the expected note
    {
        Console.ForegroundColor = pointsToAdd == 5 ? ConsoleColor.Yellow : ConsoleColor.Green;
        Console.WriteLine(ASCIIyes);
        Console.WriteLine($"Correct!" + (endlessMode ? "" : $" You scored {pointsToAdd} points."));
        if (!endlessMode)
        {
            switch (pointsToAdd)
            {
                case 1:
                    Console.WriteLine("██  \n");
                    break;
                case 2:
                    Console.WriteLine("██  ██  \n");
                    break;
                case 3:
                    Console.WriteLine("██  ██  ██  \n");
                    break;
                case 4:
                    Console.WriteLine("██  ██  ██  ██  \n");
                    break;
                case 5:
                    Console.WriteLine("██  ██  ██  ██  ██  \n");
                    break;
                default:
                    Console.WriteLine("\n");
                    break;
            }
        }
        Console.ResetColor();
        points += pointsToAdd;
    }
    else // if it is wrong
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(ASCIIno);
        Console.WriteLine($"Incorrect! You were asked to hit {noteToPlay}" + (endlessMode ? "" : ", no points for you this time") + ".\n");
        Console.ResetColor();
        tts.SpeakAsync("Incorrect.");
        Pause(800); // give them some breathing time
    }
    return points;
}
void Pause(int ms)
{
    Thread.Sleep(ms);
}
void EmptyEvent(object sender, MidiEventSentEventArgs e)
{
    // do nothing
}
T SelectMidiDevice<T>(IList<T> list) // used for input and output devices
{
    switch (list.Count)
    {
        case 1:
            return list[0]; // there's only 1 so just use that one
        case > 1:
            {
                PropertyInfo deviceName = typeof(T).GetProperty("Name");
                string[] typeOfDevice = Regex.Split(typeof(T).Name, @"(?<!^)(?=[A-Z])"); // regex necessary because duplicated code is bad
                Console.WriteLine($"Select your MIDI {typeOfDevice[0]} {typeOfDevice[1]}:");
                for (int i = 0; i < list.Count; i++) // list out all the devices, if we didn't need to use the index this could be a foreach
                {
                    Console.WriteLine($"{i}: {deviceName.GetValue(list[i])}");
                }
                int input = int.Parse(Console.ReadLine()); // get the index selected
                return input < list.Count && input >= 0 // return the selected device
                    ? list[input] : SelectMidiDevice(list); // if they fuck up then they can go in to recursion hell
            }
        default: // if there's no MIDI devices
            return default; // can't return null in a generic method
    }
}
void MidiEventReceived(object sender, MidiEventReceivedEventArgs e, ref int output)
{
    if (e.Event.EventType == MidiEventType.NoteOn) // if the input is the user pressing a note
    { // get the note number out of the wall of text
        output = int.Parse(new string(e.Event.ToString().Split('(', ')')[1].TakeWhile(char.IsDigit).ToArray())) % 12;
        resetEvent.Set();
    }
}
async Task PlayMIDI(OutputDevice outputDevice, int noteNumber, int ms)
{
    outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)(noteNumber + 60), (SevenBitNumber)127)); // plays the note to play
    await Task.Delay(ms);
    outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)(noteNumber + 60), (SevenBitNumber)127));
}
