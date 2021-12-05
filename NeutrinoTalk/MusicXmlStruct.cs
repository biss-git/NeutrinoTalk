using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace NeutrinoTalk
{
    public static class Const
    {
        public static Dictionary<int, string> StepDict = new Dictionary<int, string>()
        {
            {0, "C" },
            {1, "C#" },
            {2, "D" },
            {3, "D#" },
            {4, "E" },
            {5, "F" },
            {6, "F#" },
            {7, "G" },
            {8, "G#" },
            {9, "A" },
            {10, "A#" },
            {11, "B" },
        };
    }

    internal class MusicXmlStruct
    {
    }

    [XmlRoot(ElementName = "score-partwise")]
    public class ScorePartwise
    {
        [XmlArray(ElementName = "part")]
        [XmlArrayItem(ElementName = "measure")]
        public List<Measure> Measures { get; set; } = new List<Measure>();
    }

    public class Measure
    {
        [XmlElement(ElementName = "attributes")]
        public Attributes Attributes { get; set; }

        [XmlElement(ElementName = "direction")]
        public Direction Direction { get; set; }

        [XmlElement(ElementName = "note")]
        public List<Note> Notes { get; set; } = new List<Note>();

        public int TotalDuration
        {
            get
            {
                if(Notes.Count == 0)
                {
                    return 0;
                }
                return Notes.Sum(x => x.Duration);
            }
        }
    }

    public class Attributes
    {
        [XmlElement(ElementName = "divisions")]
        public int Divisions { get; set; } = 2;
    }

    public class Direction
    {
        [XmlElement(ElementName = "sound")]
        public Sound Sound { get; set; } = new Sound();
    }

    public class Sound
    {
        [XmlAttribute(AttributeName = "tempo")]
        public int Tempo { get; set; } = 100;
    }

    public class Note
    {
        public Note()
        {

        }

        public Note(int duration)
        {
            Duration = duration;
            Rest = new Rest();
        }

        public Note(string lyric, int duration, string step, int octave, bool breath = false)
        {
            Duration = duration;
            Lyric = new Lyric() { Text = lyric };
            Pitch = new Pitch { Step = step, Octave = octave };
            if (breath)
            {
                Notations = new Notations();
            }
        }

        public Note(string lyric, int duration, int key, bool breath = false)
        {
            Duration = duration;
            Lyric = new Lyric() { Text = lyric };
            var stepValue = key % 12;
            var octaveValue = key / 12;
            var stepChar = Const.StepDict[stepValue];
            Pitch = new Pitch { Step = stepChar.Substring(0, 1), Octave = octaveValue };
            if (stepChar.Length > 1)
            {
                // # の処理
                Pitch.Alter = 1;
            }
            if (breath)
            {
                Notations = new Notations();
            }
        }

        [XmlElement(ElementName = "rest")]
        public Rest Rest { get; set; }

        [XmlElement(ElementName = "pitch")]
        public Pitch Pitch { get; set; }

        [XmlElement(ElementName = "duration")]
        public int Duration { get; set; } = 2;

        [XmlElement(ElementName = "notations")]
        public Notations Notations { get; set; }

        [XmlElement(ElementName = "lyric")]
        public Lyric Lyric { get; set; }
    }

    public class Rest
    {

    }

    public class Pitch
    {
        [XmlElement(ElementName = "step")]
        public string Step { get; set; } = "G";
        [XmlElement(ElementName = "octave")]
        public int Octave { get; set; } = 4;
        [XmlElement(ElementName = "alter")]
        public int? Alter { get; set; }
    }

    public class Notations
    {
        [XmlElement(ElementName = "articulations")]
        public Articlations Articlations { get; set; } = new Articlations();
    }

    public class Articlations
    {
        [XmlElement(ElementName = "breath-mark")]
        public BreathMark BreathMark { get; set; } = new BreathMark();
    }

    public class BreathMark
    {

    }

    public class Lyric
    {
        [XmlElement(ElementName = "text")]
        public string Text { get; set; } = "ア";
    }

}
