using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yomiage.SDK.Config;
using Yomiage.SDK.FileConverter;
using Yomiage.SDK.Talk;

namespace NeutrinoTalk.FileLoader
{
    internal class FileConverter : FileConverterBase
    {

        public EngineConfig Config { get; set; }

        public override string[] ImportFilterList { get; } = new string[]
        {
            "ustファイル(NeutrinoTalk)|*.ust",
            "vsqxファイル(NeutrinoTalk)|*.vsqx",
        };

        public override (string, TalkScript[]) Open(string filepath, string filter)
        {
            if (filter.Contains("ust"))
            {
                var notes = UstUtil.ReadUST(filepath);
                return MakeScripts(notes, UstUtil.tempo);
            }
            else if (filter.Contains("vsqx"))
            {
                var notes = VsqxUtil.ReadVSQX(filepath);
                return MakeScripts(notes, VsqxUtil.tempo);
            }

            return (null, null);
        }


        private (string, TalkScript[]) MakeScripts(List<Note> notes, double tempo)
        {
            if (notes == null || notes.Count == 0)
            {
                return (null, null);
            }

            notes = PreProcessing(notes);

            var groups = SplitNotes(notes);

            var dict = new List<TalkScript>();

            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];

                var pauseScript = group.GetPauseScript(i, Config?.Key);
                if (pauseScript.Sections.First().Pause.Span_ms > 0)
                {
                    dict.Add(pauseScript);
                }

                var mainScript = group.GetMainScript(i, Config?.Key);

                if (mainScript.MoraCount > 0)
                {
                    dict.Add(mainScript);
                }
            }

            var text = "";
            foreach (var d in dict)
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    text += Environment.NewLine;
                }
                text += d.OriginalText;
            }

            return (text, dict.ToArray());
        }

        private List<Note> PreProcessing(List<Note> notes)
        {
            List<Note> newNotes = new List<Note>();

            // 無音区間は "R" に統一
            notes.ForEach(n =>
            {
                if (n.OutputMora == "")
                {
                    n.OutputMora = "R";
                }
            });

            // Rはマージ
            for (int i = 0; i < notes.Count; i++)
            {
                if (i == notes.Count - 1)
                {
                    newNotes.Add(notes[i]);
                    break;
                }

                var note1 = notes[i];
                var note2 = notes[i + 1];

                // R はマージ
                if (note1.OutputMora == "R" && note2.OutputMora == "R")
                {
                    newNotes.Add(new Note()
                    {
                        Time_sec = note1.Time_sec,
                        Duration = note1.Duration + note2.Duration,
                        OutputMora = "R",
                        Key = note1.Key,
                        Pitch = note1.Pitch,
                    });
                    i += 1;
                    continue;
                }

                newNotes.Add(note1);
            }

            return newNotes;
        }

        private List<NoteGroup> SplitNotes(List<Note> notes)
        {
            var noteGroups = new List<NoteGroup>();

            List<Note> group = new List<Note>();
            int pause_duration = 0;

            for (int i = 0; i < notes.Count; i++)
            {
                var note = notes[i];
                if (note.OutputMora == "R" && note.Duration > 4)
                {
                    if (group.Count > 0)
                    {
                        noteGroups.Add(new NoteGroup(pause_duration, group.ToArray()));
                        group.Clear();
                    }
                    pause_duration = note.Duration;
                    continue;
                }
                group.Add(note);
            }

            if (group.Count > 0)
            {
                noteGroups.Add(new NoteGroup(pause_duration, group.ToArray()));
                group.Clear();
            }

            return noteGroups;
        }

    }

    internal class NoteGroup
    {
        public int Pause_duration { get; set; }
        public Note[] Notes { get; set; }

        public NoteGroup(int pause_sec, Note[] notes)
        {
            Pause_duration = pause_sec;
            Notes = notes;
        }

        public TalkScript GetPauseScript(int i, string engineKey)
        {
            return new TalkScript()
            {
                OriginalText = "ポーズ＿" + i.ToString("0000") + "。",
                EngineName = engineKey,
                Sections = new List<Section>()
                    {
                        new Section()
                        {
                            Pause = new Pause()
                            {
                                Type = PauseType.Manual,
                                Span_ms = Pause_duration - 1,
                            },
                            Moras = new List<Mora>()
                            {
                                new Mora()
                                {
                                    Character = "ッ",
                                }
                            }
                        }
                    }
            };
        }

        public TalkScript GetMainScript(int i, string engineKey)
        {
            var text = "";
            foreach (var note in Notes)
            {
                if (note.OutputMora == "R")
                {
                    // 短い無音は ッ となる
                    note.OutputMora = "ッ";
                }
                text += note.OutputMora;
            }
            var mainScript = new TalkScript()
            {
                OriginalText = "フレーズ" + i.ToString("0000") + "_" + text + "。",
                EngineName = engineKey,
            };

            int pause = 1;

            Section section = new Section()
            {
                Pause = new Pause
                {
                    Type = PauseType.Manual,
                    Span_ms = pause,
                }
            };

            foreach (var note in Notes)
            {
                section.Moras.Add(new Mora()
                {
                    Character = note.OutputMora,
                    Speed = note.Duration,
                    Pitch = note.Pitch,
                }); ;
            }

            mainScript.Sections.Add(section);

            return mainScript;
        }
    }

}
