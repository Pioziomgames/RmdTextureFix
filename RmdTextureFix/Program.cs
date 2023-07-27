using TxnLib;
using static GJ.IO.IOFunctions;
using static TxnLib.RmdEnums;

namespace RmdTextureFix
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
                PrintAndExit();

            if (File.Exists(args[0]))
            {
                if (Path.GetExtension(args[0]).ToLower() == ".pac")
                {
                    Console.WriteLine($"Reading pac file: {Path.GetFileName(args[0])}");
                    FixPac(args[0]);
                }
                else if (Path.GetExtension(args[0]).ToLower() == ".rmd")
                {
                    Console.WriteLine($"Reading rmd file: {Path.GetFileName(args[0])}");
                    FixRmd(args[0]);
                }
            }
            else if (Directory.Exists(args[0]))
            {
                string[] RmdFiles = Directory.GetFiles(args[0], "*.rmd*");
                string[] PacFiles = Directory.GetFiles(args[0], "*.pac*");

                if (RmdFiles.Length > 0)
                {
                    Console.WriteLine($"Reading {RmdFiles.Length} rmd files...");
                    for (int i = 0; i < RmdFiles.Length; i++)
                    {
                        Console.WriteLine($"Reading rmd {i + 1}/{RmdFiles.Length}: {Path.GetFileName(RmdFiles[i])}");
                        FixRmd(RmdFiles[i]);
                    }
                }
                if (PacFiles.Length > 0)
                {
                    Console.WriteLine($"Reading {PacFiles.Length} pac files...");
                    for (int i = 0; i < PacFiles.Length; i++)
                    {
                        Console.WriteLine($"Reading pac {i + 1}/{PacFiles.Length}: {Path.GetFileName(PacFiles[i])}");
                        FixPac(PacFiles[i]);
                    }
                }
                if (RmdFiles.Length == 0 || PacFiles.Length == 0)
                {
                    Console.WriteLine($"No pac or rmd files found in {args[0]}");
                    Console.WriteLine("\nPress any button to exit");
                    Console.ReadKey();
                }
            }
            else
            {
                PrintAndExit();
            }
        }
        static void FixPac(string path)
        {
            PacFile pac = new(path);
            for (int i = 0; i < pac.Entries.Count; i++)
            {
                if (Path.GetExtension(pac.Entries[i].Name).ToLower() == ".rmd")
                {
                    RmdFile rmd = new(pac.Entries[i].File);
                    FixRmd(ref rmd);
                    pac.Entries[i] = new PacFile.PacEntry(pac.Entries[i].Name, rmd.Save());
                }
            }
            pac.Save(path);
        }
        static void FixRmd(string path)
        {
            RmdFile rmd = new(path);
            FixRmd(ref rmd);
            rmd.Save(path);
        }
        static void FixRmd(ref RmdFile rmd)
        {
            for (int j = 0; j < rmd.Chunks.Count; j++)
            {
                if (rmd.Chunks[j].Type == RmdChunkType.TextureDictionary)
                {
                    RwTextureDictionary td = (RwTextureDictionary)rmd.Chunks[j];
                    for (int k = 0; k < td.Textures.Count; k++)
                    {
                        if (!td.Textures[k].RasterInfo.Format.HasFlag(RwRasterFormat.HasHeaders))
                        {
                            td.Textures[k].RasterData.GenerateHeaders(td.Textures[k].RasterInfo);
                            td.Textures[k].RasterInfo.Format |= RwRasterFormat.HasHeaders;
                            td.Textures[k].RasterInfo.Format &= ~RwRasterFormat.Swizzled;
                            td.Textures[k].RasterInfo.CalculateSizes();
                        }
                    }
                    rmd.Chunks[j] = td;
                }
            }
        }
        static void PrintAndExit()
        {
            Console.WriteLine("RmdTextureFix 1.0 by Pioziomgames");
            Console.WriteLine("Usage:");
            Console.WriteLine("\tRmdTextureFix.exe {Rmd/Pac/Directory Path}");
            Console.WriteLine("\nPress any key to exit");
            Console.ReadKey();
            System.Environment.Exit(0);
        }
    }
    class PacFile
    {
        public struct PacEntry
        {
            public byte[] File;
            public string Name;
            public PacEntry(string name, byte[] file)
            {
                Name = name;
                File = file;
            }
        }
        public List<PacEntry> Entries;
        public PacFile(string path)
        {
            Entries = new List<PacEntry>();
            using (BinaryReader reader = new(File.OpenRead(path)))
            {
                while (true)
                {
                    if (reader.BaseStream.Position + 256 >= reader.BaseStream.Length)
                        break;
                    Align(reader, 64);
                    string FileName = new(reader.ReadChars(252));
                    FileName = FileName.Replace("\0", "");
                    if (FileName.Length == 0)
                        break;
                    int Size = reader.ReadInt32();
                    byte[] file = reader.ReadBytes(Size);
                    Entries.Add(new PacEntry(FileName, file));
                }
            }
        }
        public void Save(string path)
        {
            using (BinaryWriter writer = new(File.OpenWrite(path)))
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    writer.Write(Entries[i].Name.PadRight(252, '\0').ToArray());
                    writer.Write(Entries[i].File.Length);
                    writer.Write(Entries[i].File);
                    Align(writer, 64);
                }
                writer.Flush();
                writer.Close();
            }
        }
    }
}