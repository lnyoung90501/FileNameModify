using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace FileNameModify
{
    /***********************************************************************************************************************************
    Program:    FileNameModify
    Version:    1.01    3-Jan-2016  Initial version, with minor modifications
    Purpose:    Change file names by modifying the file number, that is part of the file name.  Uses a PhotoPath.dat configuration file 
                that provides directions on how to accomplish this.
    Modifications:
                1.  Subdirectory option is implmented.
                2.  Enhancement added.  Can only add sigle digit additions to the file name.  Need to be able to add a string of information to the file name.
   
    Known Problems and Enhancements:
    ***********************************************************************************************************************************/
    class Program
    {
        public struct StructData
        {
            public string sKey;
            public string sValue;
            public string sComment;
        }
        public struct Mask
        {
            public string sRegExp;
            public int iPos;
            public string sAdd;
        }
        public struct FileReplace
        {
            public List<Mask> aMask;
            public List<string> saPath;
            public string sReplace;
            public string sInsert;
            public bool bSubDir;
        }
        static void Main(string[] args)
        {
            //Declarations

            string sGNU = "\n\nModify Photo Number\nVersion 1.01, 3-Jan-2016\n\nFor assistance contact Larry Young at larry.young@youngzones.org.\n\nThis software is copyrighted by Larry N. Young, 2016.  \nThere is no warranty for this software.  A copy of the full \nlicence is included in the\nsource code and at http://www.gnu.org/licenses/gpl.txt\n\n";
            Console.WriteLine("{0}", sGNU);
            string sFilePath = "PhotoPath.dat";   //Add the file name to the path
            FileInfo existingFile = new FileInfo(sFilePath);

            //Code

            if (existingFile.Exists == false)
            {
                Console.WriteLine("\n\nFATAL ERROR: the PhotoPath.dat file in the program directory does not exsist.\nFind a copy of the file and put in the proper directory.  Exiting...");
                Console.Write("\n\nPress any key");
                Console.ReadLine();
                Environment.Exit(2);
            }
            StreamReader srFile = null;
            try
            {
                //Open the file, throws an exception when file not found
                srFile = new StreamReader(sFilePath);
            }
            catch
            {
                Console.WriteLine("\nThe {0} file did not open properly, exiting....\n", sFilePath);
                Console.Write("\n\nPress any key");
                Console.ReadLine();
                return;
            }

            ArrayList sConfigData = new ArrayList();
            int iCount = 0;
            sConfigData.Add(srFile.ReadLine());
            while (!srFile.EndOfStream)
            {
                Console.WriteLine("Line {0}: {1}", iCount, sConfigData[iCount]);
                sConfigData.Add(srFile.ReadLine());
                iCount++;
            }
            //Read last line
            Console.WriteLine("Line {0}: {1}", iCount, sConfigData[iCount]);
            sConfigData.Add(srFile.ReadLine());
            iCount++;
            Console.WriteLine("\nThere were {0} lines of text read ", iCount);
            srFile.Close();
            StructData[] sd = new StructData[iCount];
            for (int i = 0; i < iCount; i++)
                sd[i] = ParseConfigData((string)sConfigData[i]);
            Console.WriteLine("\nThe Photo Data path is: {0}\n", sd[0].sValue);
            try
            {
                FileReplace fr = ProcessConfigData(sd);
                foreach (StructData s in sd)
                {
                    if (s.sKey == "Path")
                    {
                        if (Directory.Exists(s.sValue))
                            ProcessDirectory(s.sValue, fr);
                        else
                            Console.WriteLine("\nThe directory {0} does not exsist and was not processed.\n", s.sValue);
                    }
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine("ERROR: exception thrown, value ={0}", ex);
                Environment.Exit(2);
            }
        }
        // Process all files in the directory passed in, recurse on any directories 
        // that are found, and process the files they contain.
        public static FileReplace ProcessConfigData(StructData[] sd)
        {
            FileReplace fr = new FileReplace();
            fr.aMask = new List<Mask>();
            fr.saPath = new List<string>();
            fr.sReplace = "";
            foreach (StructData s in sd)
            {
                switch (s.sKey)
                {
                    case "Mask":
                        Console.WriteLine("Mask: {0}", s.sValue);
                        fr.aMask.Add(BuildRegExp(s.sValue));
                        break;
                    case "Path":
                        Console.WriteLine("Path: {0}", s.sValue);
                        fr.saPath.Add(s.sValue);
                        break;
                    case "Replace":
                        Console.WriteLine("Insert: {0}", s.sValue);
                        fr.sReplace = s.sValue;
                        break;
                    case "Insert":
                        Console.WriteLine("Replace: {0}", s.sValue);
                        fr.sInsert = s.sValue;
                        break;
                    case "SubDir":
                        Console.WriteLine("SubDir: {0}", s.sValue);
                        if (s.sValue == "Include")
                            fr.bSubDir = true;
                        else
                            fr.bSubDir = false;
                        break;
                    default:
                        Console.WriteLine("No key match, sValue = {0}", s.sValue);
                        break;
                }
            }
            if (fr.aMask[0].sRegExp.Length < 1)
                throw new ArgumentOutOfRangeException("Mask");
            /*           foreach (Mask m in fr.aMask)
                       {
                           if (m.iPos != -1 && m.cAddChar == '\0')
                               throw new ArgumentOutOfRangeException("MissingInsertValue");
                       }
                       */
            return fr;
        }
        public static Mask BuildRegExp(string s)
        {
            char c = '\0';
            Mask m = new Mask();
            int iWildPos = -1;
            m.iPos = -1;
            m.sAdd = "";
            for (int i = 0; i < s.Length; i++)
            {
                c = s[i];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                    m.sRegExp += c;
                else if (c == '#')
                    m.sRegExp += "[0-9]";
                else if (c == '-')
                    m.iPos = i;
                else if (c == '*')
                {
                    m.sRegExp += ".*";
                    iWildPos = i;           //Record position of wildcard character.  Add must be before this point
                }
                else if (c == '.')
                    m.sRegExp += @"\.";
                else if (c == '_')
                    m.sRegExp += @"_";
                else if (c == ';')
                {
                    if (m.iPos == -1)
                    {  //This is a replace
                        m.iPos = int.Parse(s[i + 1].ToString());
                    }
                    else
                    { //This is an add charachter value
                        m.sAdd = s.Substring(i + 1, s.Length - i - 1);
                    }
                    break;      //No more to add
                }
                else
                    throw new ArgumentOutOfRangeException("Bad Reg Exp");
            }
            if (iWildPos != -1 && m.iPos > iWildPos)
                throw new ArgumentOutOfRangeException("Bad Reg Exp, wild before add point");
            Console.WriteLine("The mask is {0}", m.sRegExp);
            return m;
        }
        public static void ProcessDirectory(string targetDirectory, FileReplace fr)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                ProcessFile(fileName, fr);
            if (fr.bSubDir == true)
            {
                // Recurse into subdirectories of this directory.
                string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
                foreach (string subdirectory in subdirectoryEntries)
                    ProcessDirectory(subdirectory, fr);
            }
        }

        // Insert logic for processing found files here.
        public static void ProcessFile(string path, FileReplace fr)
        {
            Console.WriteLine("Processed file '{0}'. ", path);
            string sOrigFileName = path;
            bool bMatch = false;
            foreach (Mask m in fr.aMask)
            {
                Match match = Regex.Match(path, m.sRegExp, RegexOptions.IgnoreCase);
                Console.WriteLine("File Name: {0} {1} match with RegExp: {2}, Index = {3}", path, (match.Success ? "" : "Does Not"), m.sRegExp, match.Index);
                if (match.Success == true)
                {
                    bMatch = true;
                    if (m.sAdd.Length > 0)
                    {
                        path = path.Insert(m.iPos + match.Index, m.sAdd);
                        Console.WriteLine("   Add char, the new filename is {0}", path);
                    }
                    else
                    {
                        if (fr.sReplace.Length > 0)
                        {
                            //                     Console.WriteLine("The character at position {0} is a \'{1}\'", m.iPos + match.Index - 1, path[m.iPos + match.Index - 1]);
                            path = path.Remove(m.iPos + match.Index, 1);
                            path = path.Insert(m.iPos + match.Index, fr.sReplace);
                            Console.WriteLine("   Replace char, the new filename is {0}", path);
                        }
                        else
                        {
                            //                     Console.WriteLine("The character at position {0} is a \'{1}\'", m.iPos + match.Index - 1, path[m.iPos + match.Index - 1]);
                            //                            path = path.Remove(m.iPos + match.Index, 1);
                            path = path.Insert(m.iPos + match.Index, fr.sInsert);
                            Console.WriteLine("   Insert string, the new filename is {0}", path);
                        }

                    }
                }
            }
            if (bMatch == true)
            {
                if (File.Exists(path))
                {
                    //           throw new ArgumentOutOfRangeException("File Exsists, cannot change name"); ;
                    Console.WriteLine("    The file {0} exsists, skipping replace.", path);
                }
                else
                    File.Move(sOrigFileName, path);
            }
        }
        public static StructData ParseConfigData(string sConfigData)
        {
            int iPos = 0, iPosComment = 0;
            StructData sd = new StructData();
            iPos = sConfigData.IndexOf(';');
            if (iPos == -1)
            {
                sd.sKey = "";
                sd.sValue = sConfigData;
                sd.sComment = "";
                return sd;
            }
            else
            {
                iPosComment = sConfigData.IndexOf('|', iPos + 1);
                sd.sKey = sConfigData.Substring(0, iPos);
                if (iPosComment == -1)
                {
                    sd.sValue = sConfigData.Substring(iPos + 1, sConfigData.Length - iPos - 1);
                    sd.sComment = "";
                }
                else
                {
                    sd.sValue = sConfigData.Substring(iPos + 1, iPosComment - iPos - 1);
                    sd.sComment = sConfigData.Substring(iPosComment + 1, sConfigData.Length - iPosComment - 1);
                }
                return sd;
            }
        }
    }
}
