using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace NeutrinoTalk.FileLoader
{
    internal static class VsqxUtil
    {

        public static double tempo = 120;
        public static int resolution = 480;
        public static double key = 0;
        public static int trackNumber = 0;
        public static int[] timeArray;
        public static int[] lengthArray;
        public static int[] noteNumArray;
        public static string fileName = string.Empty;
        public static string filePath = string.Empty;
        public static List<Note> notes;

        static readonly string[] onkai = new string[]
        { "C-2", "C#-2", "D-2", "D#-2", "E-2", "F-2", "F#-2", "G-2", "G#-2", "A-1", "A#-1", "B-1",
          "C-1", "C#-1", "D-1", "D#-1", "E-1", "F-1", "F#-1", "G-1", "G#-1", "A0", "A#0", "B0",
          "C0", "C#0", "D0", "D#0", "E0", "F0", "F#0", "G0", "G#0", "A1", "A#1", "B1",
          "C1", "C#1", "D1", "D#1", "E1", "F1", "F#1", "G1", "G#1", "A2", "A#2", "B2",
          "C2", "C#2", "D2", "D#2", "E2", "F2", "F#2", "G2", "G#2", "A3", "A#3", "B3",
          "C3", "C#3", "D3", "D#3", "E3", "F3", "F#3", "G3", "G#3", "A4", "A#4", "B4",
          "C4", "C#4", "D4", "D#4", "E4", "F4", "F#4", "G4", "G#4", "A5", "A#5", "B5",
          "C5", "C#5", "D5", "D#5", "E5", "F5", "F#5", "G5", "G#5", "A6", "A#6", "B6",
          "C6", "C#6", "D6", "D#6", "E6", "F6", "F#6", "G6", "G#6", "A7", "A#7", "B7",
          "C7", "C#7", "D7", "D#7", "E7", "F7", "F#7", "G7", "G#7", "A8", "A#8", "B8",
          "C8", "C#8", "D8", "D#8", "E8", "F8", "F#8", "G8", "G#8", "A9", "A#9", "B9"
        };

        public static List<Note> ReadVSQX(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }
            VsqxUtil.filePath = filePath;
            fileName = Path.GetFileNameWithoutExtension(filePath);
            {
                XDocument xdoc = XDocument.Load(filePath, new LoadOptions() { });
                XElement masterTrack = GetElement(xdoc.Root, "masterTrack");
                resolution = (int)GetElement(masterTrack, "resolution");
                tempo = (int)GetElement(masterTrack, "tempo") / 100.0;
            }

            return SelectTrack(0);
        }

        /// <summary> MakeList </summary>
        public static List<Note> SelectTrack(int number)
        {
            key = 0;

            double length_1 = 60.0 / tempo / resolution;

            List<int> timeList = new List<int>();
            List<int> lengthList = new List<int>();
            List<int> noteNumList = new List<int>();
            int time = 0;
            int time_last = 0;
            int time_offset = 0;
            int length;
            int noteNum;
            double time_sec;
            double length_sec;
            string imora;

            notes = new List<Note>();

            var xdoc = XDocument.Load(filePath);
            var vsTracks = GetElements(xdoc.Root, "vsTrack");
            foreach (var vsTrack in vsTracks)
            {
                if ((int)GetElement(vsTrack, "tNo") == number)
                {
                    var vsParts = GetElements(vsTrack, "vsPart");
                    foreach (var vsPart in vsParts)
                    {
                        time_offset = (int)GetElement(vsPart, "t");
                        var notes = GetElements(GetElement(vsTrack, "vsPart"), "note");
                        foreach (var note in notes)
                        {
                            time = time_offset + (int)GetElement(note, "t");

                            if (time != time_last)
                            {
                                length = time - time_last;

                                timeList.Add(time_last);
                                lengthList.Add(length);
                                noteNumList.Add(60);

                                time_sec = time_last * length_1;
                                length_sec = length * length_1;

                                VsqxUtil.notes.Add(new Note()
                                {
                                    Time_sec = time_sec,
                                    Duration = 2 * length / resolution,
                                    InputMora = "",
                                    OutputMora = "",
                                    Key = "",
                                    Pitch = 0
                                });

                                //dataGridView1.Rows.Add(time_sec.ToString("F2"), length_sec.ToString("F2"), "", "", "", "");
                            }

                            length = (int)GetElement(note, "dur");
                            noteNum = (int)GetElement(note, 2);
                            imora = (string)GetElement(note, 4);

                            timeList.Add(time);
                            lengthList.Add(length);
                            noteNumList.Add(noteNum);

                            time_sec = time * length_1;
                            length_sec = length * length_1;
                            var omora = Character.ChangeMora(imora, "");

                            //int i = dataGridView1.Rows.Add(time_sec.ToString("F2"), length_sec.ToString("F2"), imora, Character.ChangeMora(imora),
                            //    onkai[noteNum], (8.1758 * Math.Pow(2, noteNum / 12.0)).ToString("F1"));
                            if (!Character.CheckMora(omora))
                            {
                                omora = "ー";
                            }

                            VsqxUtil.notes.Add(new Note()
                            {
                                Time_sec = time_sec,
                                Duration = 2 * length / resolution,
                                InputMora = imora,
                                OutputMora = omora,
                                Key = onkai[noteNum],
                                Pitch = noteNum - 59,
                            });

                            time_last = time + length;
                        }
                    }
                    break;
                }
            }

            timeArray = timeList.ToArray();
            lengthArray = lengthList.ToArray();
            noteNumArray = noteNumList.ToArray();

            return notes;
        }

        private static XElement[] GetElements(XElement e, string name)
        {
            var elements = e.Elements();
            return elements.Where(x => x.Name.ToString().Contains(name)).ToArray();
        }
        private static XElement GetElement(XElement e, string name)
        {
            var elements = e.Elements();
            foreach (var element in elements)
            {
                if (element.Name.ToString().Contains(name))
                {
                    return element;
                }
            }
            return null;
        }
        private static XElement GetElement(XElement e, int number)
        {
            return e.Elements().ToArray()[number];
        }

        private static string VSQXmora(string input)
        {
            switch (input)
            {
                case "a": return "ア";
                case "i": return "イ";
                case "j e": return "イェ";
                case "M": return "ウ";
                case "w i": return "ウィ";
                case "w e": return "ウェ";
                case "w o": return "ウォ";
                case "e": return "エ";
                case "o": return "オ";
                case "k a": return "カ";
                case "g a": return "ガ";
                case "k' i": return "キ";
                case "k' e": return "キェ";
                case "k' a": return "キャ";
                case "k' M": return "キュ";
                case "k' o": return "キョ";
                case "g' i": return "ギ";
                case "g' e": return "ギェ";
                case "g' a": return "ギャ";
                case "g' M": return "ギュ";
                case "g' o": return "ギョ";
                case "k M": return "ク";
                case "g M": return "グ";
                case "k e": return "ケ";
                case "g e": return "ゲ";
                case "k o": return "コ";
                case "g o": return "ゴ";
                case "s a": return "サ";
                case "dz a": return "ザ";
                case "S i": return "シ";
                case "S e": return "シェ";
                case "S a": return "シャ";
                case "S M": return "シュ";
                case "S o": return "ショ";
                case "dZ i": return "ジ";
                case "dZ e": return "ジェ";
                case "dZ a": return "ジャ";
                case "dZ M": return "ジュ";
                case "dZ o": return "ジョ";
                case "s M": return "ス";
                case "s i": return "スィ";
                case "dz M": return "ズ";
                case "dz i": return "ズィ";
                case "s e": return "セ";
                case "dz e": return "ゼ";
                case "s o": return "ソ";
                case "dz o": return "ゾ";
                case "t a": return "タ";
                case "d a": return "ダ";
                case "tS i": return "チ";
                case "tS e": return "チェ";
                case "tS a": return "チャ";
                case "tS M": return "チュ";
                case "tS o": return "チョ";
                case "ts M": return "ツ";
                case "ts a": return "ツァ";
                case "ts i": return "ツィ";
                case "ts e": return "ツェ";
                case "ts o": return "ツォ";
                case "t e": return "テ";
                case "t' i": return "ティ";
                case "t' e": return "テェ";
                case "t' a": return "テャ";
                case "t' M": return "テュ";
                case "t' o": return "テョ";
                case "d e": return "デ";
                case "d' i": return "ディ";
                case "d' e": return "デェ";
                case "d' a": return "デャ";
                case "d' M": return "デュ";
                case "d' o": return "デョ";
                case "t o": return "ト";
                case "t M": return "トゥ";
                case "d o": return "ド";
                case "d M": return "ドゥ";
                case "n a": return "ナ";
                case "J i": return "ニ";
                case "J e": return "ニェ";
                case "J a": return "ニャ";
                case "J M": return "ニュ";
                case "J o": return "ニョ";
                case "n M": return "ヌ";
                case "n e": return "ネ";
                case "n o": return "ノ";
                case "h a": return "ハ";
                case "b a": return "バ";
                case "p a": return "パ";
                case "C i": return "ヒ";
                case "C e": return "ヒェ";
                case "C a": return "ヒャ";
                case "C M": return "ヒュ";
                case "C o": return "ヒョ";
                case "b' i": return "ビ";
                case "b' e": return "ビェ";
                case "b' a": return "ビャ";
                case "b' M": return "ビュ";
                case "b' o": return "ビョ";
                case "p' i": return "ピ";
                case "p' e": return "ピェ";
                case "p' a": return "ピャ";
                case "p' M": return "ピュ";
                case "p' o": return "ピョ";
                case @"p\ M": return "フ";
                case @"p\ a": return "ファ";
                case @"p\' i": return "フィ";
                case @"p\ e": return "フェ";
                case @"p\ o": return "フォ";
                case @"p\' a": return "フャ";
                case @"p\' M": return "フュ";
                case "b M": return "ブ";
                case "p M": return "プ";
                case "h e": return "ヘ";
                case "b e": return "ベ";
                case "p e": return "ペ";
                case "h o": return "ホ";
                case "b o": return "ボ";
                case "p o": return "ポ";
                case "m a": return "マ";
                case "m' i": return "ミ";
                case "m' e": return "ミェ";
                case "m' a": return "ミャ";
                case "m' M": return "ミュ";
                case "m' o": return "ミョ";
                case "m M": return "ム";
                case "m e": return "メ";
                case "m o": return "モ";
                case "j a": return "ヤ";
                case "j M": return "ユ";
                case "j o": return "ヨ";
                case "4 a": return "ラ";
                case "4' i": return "リ";
                case "4' e": return "リェ";
                case "4' a": return "リャ";
                case "4' M": return "リュ";
                case "4' o": return "リョ";
                case "4 M": return "ル";
                case "4 e": return "レ";
                case "4 o": return "ロ";
                case "w a": return "ワ";
                case "J": return "ン";
                case "N": return "ン";
                case "N'": return "ン";
                case @"N\": return "ン";
                case "m": return "ン";
                case "m'": return "ン";
                case "n": return "ン";
                case "-": return "ー";
                case "4'": return "リャSS";
                case "4": return "ラSS";
                case "C": return "ヒSS";
                case "S": return "シSS";
                case "b'": return "ビSS";
                case "b": return "バSS";
                case "d'": return "ディSS";
                case "d": return "ダSS";
                case "dZ": return "ジャSS";
                case "dz": return "ザSS";
                case "g'": return "ギャSS";
                case "g": return "ガSS";
                case "h": return "ハSS";
                case "j": return "ヤSS";
                case "k'": return "キャSS";
                case "k": return "カSS";
                case "p'": return "ピャSS";
                case "p": return "パSS";
                case @"p\": return "フSS";
                case "s": return "サSS";
                case "t'": return "ティSS";
                case "t": return "タSS";
                case "tS": return "チSS";
                case "ts": return "ツSS";
                case "w": return "ワSS";
            }
            return input;
        }

    }
}
