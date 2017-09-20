﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace GalEngine
{
    public class ResListAnalyser : ResourceAnalyser
    {
        private enum ResourceType
        {
            Unknown,
            Image,
            Audio,
            TextFormat,
            Brush
        }

        private class Sentence
        {
            public ResourceType Type = ResourceType.Unknown;
            public string FilePath = null;
            public string Tag = null;
            public string Fontface = null;
            public float Size = 0;
            public int Weight = 0;
            public Vector4 Color = Vector4.Zero;

            public override string ToString()
            {
                string result = "";

                result += "Type = " + Type;
                result += ", Tag = " + Include(Tag);

                switch (Type)
                {
                    case ResourceType.Unknown:
                        DebugLayer.ReportError(ErrorType.UnknownResourceType);
                        break;

                    case ResourceType.Image:
                    case ResourceType.Audio:
                        result += ", FilePath = " + Include(FilePath);
                        break;

                    case ResourceType.TextFormat:
                        result += ", Fontface = " + Include(Fontface);
                        result += ", Size = " + Size;
                        result += ", Weight = " + Weight;
                        break;

                    case ResourceType.Brush:
                        result += ", Red = " + Color.X;
                        result += ", Green = " + Color.Y;
                        result += ", Blue = " + Color.Z;
                        result += ", Alpha = " + Color.W;
                        break;

                    default:
                        break;
                }
                return "[" + result + "]";
            }

            public bool IsError()
            {
                if (Tag is null) return true;

                switch (Type)
                {
                    case ResourceType.Unknown:
                        return true;
                       
                    case ResourceType.Image:
                        return FilePath is null;

                    case ResourceType.Audio:
                        return FilePath is null;

                    case ResourceType.TextFormat:
                        return Fontface is null | Size is 0 | Weight is 0; 

                    case ResourceType.Brush:
                        return Color == -Vector4.One;

                    default:
                        return true;
                }
            }

            public static string Include(string input) => '"' + input + '"';

            public static string UnInclude(string input) => input.Substring(1, input.Length - 2);

            public static ResourceType GetType(string TypeName)
            {
                switch (TypeName)
                {
                    case "Image":
                        return ResourceType.Image;

                    case "Audio":
                        return ResourceType.Audio;

                    case "TextFormat":
                        return ResourceType.TextFormat;

                    case "Brush":
                        return ResourceType.Brush;

                    default:
                        DebugLayer.ReportError(ErrorType.UnknownResourceType);
                        return ResourceType.Unknown;
                }
            }
        }

        private Dictionary<string, ResourceTag> resourceList;

        private static void ProcessSentenceValue(ref Sentence sentence, ref string value)
        {
            var result = value.Split(new char[] { '=' }, 2);

            var left = result[0]; var right = result[1];

            switch (left)
            {
                case "Type":
                    sentence.Type = Sentence.GetType(right);
                    break;

                case "Tag":
                    sentence.Tag = Sentence.UnInclude(right);
                    break;

                case "FilePath":
                    sentence.FilePath = Sentence.UnInclude(right);
                    break;

                case "Fontface":
                    sentence.Fontface = Sentence.UnInclude(right);
                    break;

                case "Size":
                    sentence.Size = (float)Convert.ToDouble(right);
                    break;

                case "Weight":
                    sentence.Weight = Convert.ToInt32(right);
                    break;

                case "Red":
                    sentence.Color.X = (float)Convert.ToDouble(right);
                    break;

                case "Green":
                    sentence.Color.Y = (float)Convert.ToDouble(right);
                    break;

                case "Blue":
                    sentence.Color.Z = (float)Convert.ToDouble(right);
                    break;

                case "Alpha":
                    sentence.Color.W = (float)Convert.ToDouble(right);
                    break;


                default:
                    DebugLayer.ReportError(ErrorType.InconsistentResourceParameters, value);
                    break;
            }

            value = "";
        }

        private static void BuildSentenceFromFile(string[] contents, string filePath, out List<Sentence> sentences)
        {
            sentences = new List<Sentence>();

            Sentence currentSentence = null;

            string currentString = "";
            bool inString = false;
            bool inSentence = false;

            int line = 0;

            foreach (var item in contents)
            {
                line++;

                for (int i = 0; i < item.Length; i++)
                {
                    //The Sentence's Beginning
                    if (item[i] is '[')
                    {
#if DEBUG
                        DebugLayer.Assert(inSentence is true, ErrorType.InvalidResourceFormat, line, filePath);
#endif
                        inSentence = true;
                        currentSentence = new Sentence(); continue;
                    }


                    //The Sentence's Ending
                    if (item[i] is ']')
                    {
#if DEBUG
                        DebugLayer.Assert(inSentence is false, ErrorType.InvalidResourceFormat, line, filePath);
#endif
                        inSentence = false;

                        ProcessSentenceValue(ref currentSentence, ref currentString);

#if DEBUG
                        DebugLayer.Assert(currentSentence.IsError(), ErrorType.InconsistentResourceParameters,
                            currentSentence.ToString());
#endif
                        sentences.Add(currentSentence); continue;
                    }

                    //Find a value
                    if (item[i] is ',' && inString is false)
                    {
                        ProcessSentenceValue(ref currentSentence, ref currentString);

                        continue;
                    }

                    if (item[i] != ' ' || inString is true)
                        currentString += item[i];

                    if (item[i] is '"') { inString ^= true; continue; }
                }
            }

#if DEBUG
            DebugLayer.Assert(inSentence is true | inString is true, ErrorType.InvalidResourceFormat,
                contents.Length, filePath);
#endif

        }

        private static void BuildSentenceFromList(Dictionary<string, ResourceTag> resourceList, out List<Sentence> sentences)
        {
            sentences = new List<Sentence>();

            foreach (var item in resourceList)
            {
                Sentence sentence = new Sentence
                {
                    Tag = item.Key
                };

                switch (item.Value)
                {
                    case ImageTag imageTag:
                        sentence.Type = ResourceType.Image;
                        sentence.FilePath = imageTag.FilePath;
                        break;

                    case AudioTag audioTag:
                        sentence.Type = ResourceType.Audio;
                        sentence.FilePath = audioTag.FilePath;
                        break;

                    case TextFormatTag textFormatTag:
                        sentence.Type = ResourceType.TextFormat;
                        sentence.Fontface = textFormatTag.Fontface;
                        sentence.Size = textFormatTag.Size;
                        sentence.Weight = textFormatTag.Weight;
                        break;

                    case BrushTag brushTag:
                        sentence.Type = ResourceType.Brush;
                        sentence.Color = brushTag.Color;
                        break;

                    default:
                        DebugLayer.ReportError(ErrorType.UnknownResourceType);
                        break;
                }

                sentences.Add(sentence);
            }
        }

        internal ResListAnalyser(string Tag, string FilePath) : base(Tag, FilePath)
        {
            resourceList = new Dictionary<string, ResourceTag>();
        }

        protected override void ProcessReadFile(ref string[] contents)
        {
            BuildSentenceFromFile(contents, FilePath, out List<Sentence> sentences);

            foreach (var item in sentences)
            {
                switch (item.Type)
                {
                    case ResourceType.Unknown:
                        DebugLayer.ReportError(ErrorType.UnknownResourceType);
                        break;

                    case ResourceType.Image:
                        resourceList.Add(item.Tag, new ImageTag(item.Tag, item.FilePath));
                        break;

                    case ResourceType.Audio:
                        resourceList.Add(item.Tag, new AudioTag(item.Tag, item.FilePath));
                        break;

                    case ResourceType.TextFormat:
                        resourceList.Add(item.Tag, new TextFormatTag(item.Tag, item.Fontface, item.Size, item.Weight));
                        break;

                    case ResourceType.Brush:
                        resourceList.Add(item.Tag, new BrushTag(item.Tag, item.Color));
                        break;

                    default:
                        break;
                }
            }

        }

        protected override void ProcessWriteFile(out string[] contents)
        {
            BuildSentenceFromList(resourceList, out List<Sentence> sentences);

            contents = new string[sentences.Count * 2];

            for (int i = 0; i < sentences.Count; i++)
            {
                contents[i * 2] = sentences[i].ToString();
            }
        }
    }
}