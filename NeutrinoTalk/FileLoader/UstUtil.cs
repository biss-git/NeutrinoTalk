using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace NeutrinoTalk.FileLoader
{
    internal static class UstUtil
    {
        public static double tempo = 120;
        public static double key = 0;
        public static int[] lengthArray;
        public static int[] noteNumArray;
        public static string fileName = string.Empty;
        public static List<Note> notes;

        static readonly string[] onkai = new string[]
        {
          "C1", "C#1", "D1", "D#1", "E1", "F1", "F#1", "G1", "G#1", "A2", "A#2", "B2",
          "C2", "C#2", "D2", "D#2", "E2", "F2", "F#2", "G2", "G#2", "A3", "A#3", "B3",
          "C3", "C#3", "D3", "D#3", "E3", "F3", "F#3", "G3", "G#3", "A4", "A#4", "B4",
          "C4", "C#4", "D4", "D#4", "E4", "F4", "F#4", "G4", "G#4", "A5", "A#5", "B5",
          "C5", "C#5", "D5", "D#5", "E5", "F5", "F#5", "G5", "G#5", "A6", "A#6", "B6",
          "C6", "C#6", "D6", "D#6", "E6", "F6", "F#6", "G6", "G#6", "A7", "A#7", "B7",
          "C7", "C#7", "D7", "D#7", "E7", "F7", "F#7", "G7", "G#7", "A8", "A#8", "B8",
          "C8", "C#8", "D8", "D#8", "E8", "F8", "F#8", "G8", "G#8", "A9", "A#9", "B9"
        };

        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileString(
        string lpApplicationName,
        string lpKeyName,
        string lpDefault,
        StringBuilder lpReturnedstring,
        int nSize,
        string lpFileName);

        public static string GetIniValue(string path, string section, string key)
        {
            StringBuilder sb = new StringBuilder(256);
            GetPrivateProfileString(section, key, string.Empty, sb, sb.Capacity, path);
            return sb.ToString();
        }


        public static List<Note> ReadUST(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }
            fileName = Path.GetFileNameWithoutExtension(filePath);
            tempo = double.Parse(GetIniValue(filePath, "#SETTING", "tempo"));

            double length_1 = 60.0 / tempo;
            string temp;

            List<int> lengthList = new List<int>();
            List<int> noteNumList = new List<int>();
            int length;
            double time = 0;
            double length_sec;
            string imora;
            int noteNum;

            notes = new List<Note>();
            for (int i = 0; i < 9999; i++)
            {
                temp = GetIniValue(filePath, "#" + i.ToString("D04"), "Length");
                if (string.IsNullOrEmpty(temp)) { break; }
                length = int.Parse(temp);
                lengthList.Add(length);
                length_sec = length_1 * length / 480;
                imora = GetIniValue(filePath, "#" + i.ToString("D04"), "Lyric");
                noteNum = int.Parse(GetIniValue(filePath, "#" + i.ToString("D04"), "NoteNum"));
                noteNumList.Add(noteNum);

                notes.Add(new Note()
                {
                    Time_sec = time,
                    Duration = Math.Max(length / 60, 1),
                    InputMora = imora,
                    OutputMora = Character.ChangeMora(imora, ""),
                    Key = onkai[noteNum - 24],
                    Pitch = noteNum - 59
                });

                if (!Character.CheckMora(notes.Last().OutputMora))
                {
                    notes.Last().OutputMora = "ー";
                }
                time += length_sec;
            }

            lengthArray = lengthList.ToArray();
            noteNumArray = noteNumList.ToArray();

            return notes;
        }
    }

    internal class Note
    {
        public double Time_sec;
        public int Duration;
        public string InputMora;
        public string OutputMora;
        public string Key;
        //public double Pitch_Hz;
        public int Pitch;
    }

    /// <summary>
    /// ボイスロイドで扱える文字に関するクラス
    /// </summary>
    public static class Character
    {

        public static double pitchReference = 282;
        public static double pitchReferenceBottom = 141;
        public static double pitchReferenceTop = 564;

        /// <summary>
        /// １文字の文字一覧
        /// </summary>
        public static readonly string[] mora1 = new string[]
        {
            "ア","イ","ウ","エ","オ",
            "カ","キ","ク","ケ","コ",
            "サ","シ","ス","セ","ソ",
            "タ","チ","ツ","テ","ト",
            "ナ","ニ","ヌ","ネ","ノ",
            "ハ","ヒ","フ","ヘ","ホ",
            "マ","ミ","ム","メ","モ",
            "ヤ","ユ","ヨ",
            "ラ","リ","ル","レ","ロ",
            "ワ","ヲ","ン",
            "ヰ","ヴ","ヱ",
            "ガ","ギ","グ","ゲ","ゴ",
            "ザ","ジ","ズ","ゼ","ゾ",
            "ダ","ヂ","ヅ","デ","ド",
            "バ","ビ","ブ","ベ","ボ",
            "パ","ピ","プ","ペ","ポ",
            //"ァ","ィ","ゥ","ェ","ォ",
            //"ャ","ュ","ョ",
            "ー", "ッ"
        };
        /// <summary>
        /// ２文字の文字一覧
        /// </summary>
        public static readonly string[] mora2 = new string[]
        {
                                    "イェ",
                    "ウィ",         "ウェ", "ウォ",
                                    "キェ",         "キャ", "キュ", "キョ",
            "クァ", "クィ",         "クェ", "クォ",
                                    "シェ",         "シャ", "シュ", "ショ",
            "スァ", "スィ",         "スェ", "スォ",
                                    "チェ",         "チャ", "チュ", "チョ",
            "ツァ", "ツィ",         "ツェ", "ツォ",
                    "ティ",                         "テャ", "テュ", "テョ",
                            "トゥ",
                                    "ニェ",         "ニャ", "ニュ", "ニョ",
            "ヌァ", "ヌィ",         "ヌェ", "ヌォ",
                                    "ヒェ",         "ヒャ", "ヒュ", "ヒョ",
            "ファ", "フィ",         "フェ", "フォ", "フャ", "フュ", "フョ",
                                    "ミェ",         "ミャ", "ミュ", "ミョ",
            "ムァ", "ムィ",         "ムェ", "ムォ",
                                    "リェ",         "リャ", "リュ", "リョ",
            "ルァ", "ルィ",         "ルェ", "ルォ",
            "ヴァ", "ヴィ",         "ヴェ", "ヴォ", "ヴャ", "ヴュ", "ヴョ",
                                    "ギェ",         "ギャ", "ギュ", "ギョ",
            "グァ", "グィ",         "グェ", "グォ",
                                    "ジェ",         "ジャ", "ジュ", "ジョ",
            "ズァ", "ズィ",         "ズェ", "ズォ",
                    "ディ",                         "デャ", "デュ", "デョ",
                            "ドゥ",
                                    "ビェ",         "ビャ", "ビュ", "ビョ",
            "ブァ", "ブィ",         "ブェ", "ブォ", "ブャ", "ブュ", "ブョ",
                                    "ピェ",         "ピャ", "ピュ", "ピョ",
            "プァ", "プィ",         "プェ", "プォ", "プャ", "プュ", "プョ",
        };

        public static readonly string[] subStringD = new string[]
        {
            "D", "Ｄ", "d", "ｄ"
        };
        public static readonly string[] subStringV = new string[]
        {
            "V", "Ｖ", "v", "ｖ"
        };

        /// <summary>
        /// 話速1の時の長さ
        /// </summary>
        /// <param name="n">モーラ数</param>
        public static double length(int n)
        {
            if (n < 1)
            {
                return 0.16;
            }
            else if (n > 30)
            {
                return 0.13;
            }
            return 0.16 - 0.03 * n / 30;
        }

        /// <summary>
        /// 長さの基準値
        /// </summary>
        public static double lengthReference = 0.16;

        public static string CorrectMora(string inputMora)
        {
            if (inputMora.Length == 0)
            {
                return string.Empty;
            }
            else if (inputMora.Length == 1)
            {
                if (mora1.Contains(inputMora))
                {
                    return inputMora;
                }
            }
            else if (inputMora.Length == 2)
            {
                if (mora2.Contains(inputMora))
                {
                    return inputMora;
                }
                if (mora1.Contains(inputMora.Substring(0, 1)) &&
                    subStringD.Contains(inputMora.Substring(1, 1)))
                {
                    return inputMora.Substring(0, 1) + "D";
                }
                if (mora1.Contains(inputMora.Substring(0, 1)) &&
                    subStringV.Contains(inputMora.Substring(1, 1)))
                {
                    return inputMora.Substring(0, 1) + "V";
                }
            }
            else if (inputMora.Length == 3)
            {
                if (mora2.Contains(inputMora.Substring(0, 2)) &&
                    subStringD.Contains(inputMora.Substring(2, 1)))
                {
                    return inputMora.Substring(0, 2) + "D";
                }
                if (mora2.Contains(inputMora.Substring(0, 2)) &&
                    subStringV.Contains(inputMora.Substring(2, 1)))
                {
                    return inputMora.Substring(0, 2) + "V";
                }
            }
            return string.Empty;
        }
        public static bool CheckMora(string inputMora)
        {
            if (inputMora.Length == 0)
            {
                return true;
            }
            else if (inputMora.Length == 1)
            {
                if (mora1.Contains(inputMora))
                {
                    return true;
                }
            }
            else if (inputMora.Length == 2)
            {
                if (mora2.Contains(inputMora))
                {
                    return true;
                }
                if (mora1.Contains(inputMora.Substring(0, 1)) &&
                    subStringD.Contains(inputMora.Substring(1, 1)))
                {
                    return true;
                }
                if (mora1.Contains(inputMora.Substring(0, 1)) &&
                    subStringV.Contains(inputMora.Substring(1, 1)))
                {
                    return true;
                }
            }
            else if (inputMora.Length == 3)
            {
                if (mora2.Contains(inputMora.Substring(0, 2)) &&
                    subStringD.Contains(inputMora.Substring(2, 1)))
                {
                    return true;
                }
                if (mora2.Contains(inputMora.Substring(0, 2)) &&
                    subStringV.Contains(inputMora.Substring(2, 1)))
                {
                    return true;
                }
            }
            return false;
        }

        public static string ChangeMora(string mora, string v = "V")
        {
            if (mora == "R")
            {
                return string.Empty;
            }
            // "a R"みたいな末端の音
            if (mora.Contains(" R"))
            {
                return "ー";
            }
            string tempMora = ToKatakana(mora);
            foreach (var m in mora2)
            {
                if (tempMora.Contains(m))
                {
                    return m + v;
                }
            }
            foreach (var m in mora1)
            {
                if (tempMora.Contains(m))
                {
                    return m + v;
                }
            }
            return mora + v;
        }

        /// <summary> カタカナに変換する、D,Vは全角になってしまうので注意 ゔだけは特別扱いしておく </summary>
        public static string ToKatakana(string input)
        {
            return new string(input.Select(c =>
            {
                return (c == 'ゔ') ? 'ヴ' :
                    (c >= 'ぁ' && c <= 'ゖ') ? (char)(c + 'ァ' - 'ぁ') : c;
            }).ToArray());
        }
        /// <summary> ひらがなに変換する 「ヴ」だけはそのままにしておく　「ゔ」が環境依存文字のため </summary>
        public static string ToHiragana(string input)
        {
            return new string(input.Select(
                c =>
                {
                    return c == 'ゔ' ? 'ヴ' :
                    (c >= 'ァ' && c <= 'ヶ' && c != 'ヴ') ? (char)(c + 'ぁ' - 'ァ') : c;
                }).ToArray());
        }
    }
}
