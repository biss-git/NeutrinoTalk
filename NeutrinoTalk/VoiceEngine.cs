using NAudio.Wave;
using NeutrinoTalk.FileLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Yomiage.SDK;
using Yomiage.SDK.Config;
using Yomiage.SDK.FileConverter;
using Yomiage.SDK.Talk;
using Yomiage.SDK.VoiceEffects;

namespace NeutrinoTalk
{
    public class VoiceEngine : VoiceEngineBase
    {
        private FileConverter fileConverter = new FileConverter();
        public override IFileConverter FileConverter => fileConverter;

        public override void Initialize(string configDirectory, string dllDirectory, EngineConfig config)
        {
            base.Initialize(configDirectory, dllDirectory, config);
            fileConverter.Config = config;
        }

        public override async Task<double[]> Play(VoiceConfig mainVoice, VoiceConfig subVoice, TalkScript talkScript, MasterEffectValue masterEffect, Action<int> setSamplingRate_Hz, Action<double[]> submitWavePart)
        {
            int tempo = (int)(mainVoice.VoiceEffect.GetTempo() * masterEffect.GetSpeed());
            double volume = mainVoice.VoiceEffect.GetVolume() * masterEffect.GetVolume();
            int keyShift = mainVoice.VoiceEffect.GetKeyShift();
            string syntheType = mainVoice.Library.Settings.GetSyntheType();

            if (talkScript.MoraCount == 1 && talkScript.Sections[0].Moras[0].Character == "ッ")
            {
                setSamplingRate_Hz(44100);
                return new double[(int)(44100 * (60.0 / tempo / 2) * talkScript.Sections.First().Pause.Span_ms)];
            }

            var neutrinoFolder = Settings.GetBaseFolder();

            if (string.IsNullOrWhiteSpace(neutrinoFolder) ||
                !Directory.Exists(neutrinoFolder))
            {
                StateText = "neutrino のフォルダが見つかりません。" + neutrinoFolder;
                return null;
            }


            var notes = MakeNotes(talkScript, keyShift);

            var totalDuration = notes.Sum(x => x.Duration); // ここで totalDuration を計算しておかないと WriteMusicXml で変更されてしまう。

            WriteMusicXml(notes, tempo, neutrinoFolder);


            (var fs, var wave) = await GenerateWave(neutrinoFolder, syntheType);

            setSamplingRate_Hz(fs);

            var removeLength = (int)(fs * (60.0 / tempo / 2) * 8);
            var waveLength = (int)(fs * (60.0 / tempo / 2) * totalDuration);
            var headLength = 0;
            if (talkScript.Sections.First().Pause.Span_ms <= 0)
            {
                headLength = (int)(fs * (60.0 / tempo / 2) * 1);
                waveLength += headLength;
            }

            return wave.Skip(removeLength - headLength).Take(waveLength).Select(x => x * volume).ToArray();
        }

        private List<Note> MakeNotes(TalkScript talkScript, int keyShift)
        {
            var notes = new List<Note>();
            foreach(var section in talkScript.Sections)
            {
                if (section.Pause.Span_ms > 0)
                {
                    var note = new Note(section.Pause.Span_ms);
                    notes.Add(note);
                }
                foreach (var mora in section.Moras)
                {
                    if(mora.Character == "ッ")
                    {
                        var note = new Note(mora.GetDuration());
                        notes.Add(note);
                    }
                    else
                    {
                        var note = new Note(mora.Character, mora.GetDuration(), 4 * 12 - 1 + mora.GetPitch() + keyShift, mora.GetBreath());
                        notes.Add(note);
                    }
                }
            }

            if(talkScript.EndSection.Pause.Span_ms > 0)
            {
                var note = new Note(talkScript.EndSection.Pause.Span_ms);
                notes.Add(note);
            }

            return notes;
        }

        private void WriteMusicXml(List<Note> notes, int tempo, string neutrinoFolder)
        {
            ScorePartwise scorePartwise = new ScorePartwise();

            {
                var measure = new Measure();
                measure.Attributes = new Attributes() { };
                measure.Direction = new Direction() { Sound = { Tempo = tempo } };
                measure.Notes.Add(new Note(8));
                scorePartwise.Measures.Add(measure);
            }
            {
                var measure = new Measure();
                foreach(var note in notes)
                {
                    measure.Notes.Add(note);
                    while(measure.TotalDuration >= 8)
                    {
                        if (measure.TotalDuration > 8)
                        {
                            var temp = measure.TotalDuration - 8;
                            var lastNote = measure.Notes.Last();
                            lastNote.Duration -= temp;
                            scorePartwise.Measures.Add(measure);
                            measure = new Measure();
                            if (lastNote.Pitch != null)
                            {
                                measure.Notes.Add(new Note("ー", temp, lastNote.Pitch.Step, lastNote.Pitch.Octave, lastNote.Notations != null));
                            }
                            else
                            {
                                measure.Notes.Add(new Note(temp));
                            }
                            lastNote.Notations = null;
                        }
                        else
                        {
                            scorePartwise.Measures.Add(measure);
                            measure = new Measure();
                        }
                    }
                }
                if (measure.TotalDuration > 0)
                {
                    if (measure.TotalDuration < 8)
                    {
                        measure.Notes.Add(new Note(8 - measure.TotalDuration));
                    }
                    scorePartwise.Measures.Add(measure);
                }
            }
            {
                var measure = new Measure();
                measure.Notes.Add(new Note(8));
                scorePartwise.Measures.Add(measure);
            }

            var xmlPath = Path.Combine(neutrinoFolder, "score", "musicxml", "unicoe.musicxml");

            {
                XmlSerializer ser = new XmlSerializer(typeof(ScorePartwise));
                XmlWriterSettings settings = new XmlWriterSettings()
                {
                    Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    Indent = true,
                    IndentChars = "  ",
                };
                using var textWriter = XmlWriter.Create(xmlPath, settings);
                ser.Serialize(textWriter, scorePartwise);
            }
        }

        private async Task<(int, List<double>)> GenerateWave(string neutrinoFolder, string syntheType)
        {
            var format = "RunBatFormat_WORLD";
            switch (syntheType)
            {
                case "WORLD":
                    format = "RunBatFormat_WORLD";
                    break;
                case "NSF":
                    format = "RunBatFormat_NSF";
                    break;
            }

            var batText = File.ReadAllText(Path.Combine(DllDirectory, format));
            batText = batText.Replace("{ModelDir}", "MERROW");

            var batPath = Path.Combine(neutrinoFolder, "unicoe.bat");
            File.WriteAllText(batPath, batText);

            var processStartInfo = new ProcessStartInfo()
            {
                FileName = batPath,
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            var process = Process.Start(processStartInfo);
            process.WaitForExit();

            var wavPath = Path.Combine(neutrinoFolder, "output", "unicoe.wav");

            if (!File.Exists(wavPath))
            {
                return (44100, new List<double>());
            }

            using var reader = new WaveFileReader(wavPath);
            var wave = new List<double>();
            int fs = reader.WaveFormat.SampleRate;
            while (reader.Position < reader.Length)
            {
                var samples = reader.ReadNextSampleFrame();
                wave.Add(samples.First());
            }

            return (fs, wave);
        }
    }
}
