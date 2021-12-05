// For Directory.GetFiles and Directory.GetDirectories
// For File.Exists, Directory.Exists
using System;
using System.IO;
using System.Runtime.InteropServices;

public class RecursiveFileProcessor
{

    [DllImport("kernel32.dll")]
    static extern uint GetCompressedFileSizeW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
    [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]

    static extern bool GetDiskFreeSpace(string lpDirectoryName,
    out ulong lpSectorsPerCluster,
    out ulong lpBytesPerSector,
    out ulong lpNumberOfFreeClusters,
    out ulong lpTotalNumberOfClusters);

    public static void Main()
    {

        ulong SectorsPerCluster = 0;
        ulong BytesPerSector = 0;
        ulong NumberOfFreeClusters = 0;
        ulong TotalNumberOfClusters = 0;

        long TotalSizeOfFiles = 0;
        long TotalFileSlack = 0;

        ulong TotalNumberOfFiles = 0;
        ulong TotalNTFSCompressedFiles = 0;

        DateTime BeginningTime = DateTime.Now;
        DateTime EndingTime;
        TimeSpan TimeTaken;

        bool GetDiskFreeSpaceSuccess = GetDiskFreeSpace("C:\\", out SectorsPerCluster, out BytesPerSector, out NumberOfFreeClusters, out TotalNumberOfClusters);

        if (!GetDiskFreeSpaceSuccess)
            throw new System.ComponentModel.Win32Exception();

        Console.WriteLine("Enter folder path: ");
        string FolderPath = Console.ReadLine();
        Console.WriteLine(Environment.NewLine);

        Console.WriteLine("Beginning at: " + BeginningTime);
        Console.WriteLine(Environment.NewLine);

        if (Directory.Exists(FolderPath))
        {
            // This path is a directory
            ProcessDirectory(FolderPath, BytesPerSector, SectorsPerCluster, ref TotalSizeOfFiles, ref TotalFileSlack, ref TotalNumberOfFiles, ref TotalNTFSCompressedFiles);
        }
        else
        {
            Console.WriteLine("{0} is not a valid file or directory.", FolderPath);
        }

        EndingTime = DateTime.Now;
        TimeTaken = EndingTime.Subtract(BeginningTime);

        Console.WriteLine(Environment.NewLine, Environment.NewLine);

        Console.WriteLine("Time began: " + BeginningTime);
        Console.WriteLine("Time ended: " + EndingTime);
        Console.WriteLine("Time taken: " + TimeTaken);

        Console.WriteLine(Environment.NewLine);

        Console.WriteLine("Total Size of files in drive is: " + TotalSizeOfFiles);
        Console.WriteLine("Total file slack of files in drive is: " + TotalFileSlack);
        Console.WriteLine("Total NTFS Compressed Files: " + TotalNTFSCompressedFiles);
        Console.WriteLine("Total number of files scanned: " + TotalNumberOfFiles);

    }

    // Process all files in the directory passed in, recurse on any directories 
    // that are found, and process the files they contain.
    public static void ProcessDirectory(string targetDirectory, ulong BytesPerSector, ulong SectorsPerCluster, ref long TotalSizeOfFiles, ref long TotalFileSlack, ref ulong TotalNumberOfFiles, ref ulong TotalNTFSCompressedFiles)
    {

        // Process the list of files found in the directory.

        try
        {

            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                ProcessFile(fileName, BytesPerSector, SectorsPerCluster, ref TotalSizeOfFiles, ref TotalFileSlack, ref TotalNumberOfFiles, ref TotalNTFSCompressedFiles);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory, BytesPerSector, SectorsPerCluster, ref TotalSizeOfFiles, ref TotalFileSlack, ref TotalNumberOfFiles, ref TotalNTFSCompressedFiles);

        }

        catch (System.UnauthorizedAccessException)
        {

            // Do Nothing

        }

    }

    // Insert logic for processing found files here.
    public static void ProcessFile(string path, ulong BytesPerSector, ulong SectorsPerCluster, ref long TotalSizeOfFiles, ref long TotalFileSlack, ref ulong TotalNumberOfFiles, ref ulong TotalNTFSCompressedFiles)
    {
        TotalNumberOfFiles++;

        Console.Write("\rProcessing file: " + path);

        long FileSize = 0;
        long FileSlack = 0;
        long FileSizeOnDisk = 0;
        long size = 0;
        uint hosize = 0;
        FileInfo FileNameInfo;

        uint ClusterSize = 0;
        uint losize = 0;

        FileNameInfo = new FileInfo(path);
        FileSize = new FileInfo(path).Length;

        ClusterSize = (uint)(SectorsPerCluster * BytesPerSector);
        losize = GetCompressedFileSizeW(path, out hosize);
        size = (long)hosize << 32 | losize;
        FileSizeOnDisk = ((size + ClusterSize - 1) / ClusterSize) * ClusterSize;

        if (FileSizeOnDisk > FileSize)
            FileSlack = FileSizeOnDisk - FileSize;

        if (FileSizeOnDisk < FileSize)
        {

            TotalNTFSCompressedFiles++;

        }


       // Console.WriteLine("File slack is for file " + path + " with a size of " + FileSize + " is : " + FileSlack);

        TotalSizeOfFiles += Convert.ToInt64(FileSizeOnDisk);
        TotalFileSlack += Convert.ToInt64(FileSlack);

        ClearCurrentConsoleLine();

    }

    public static void ClearCurrentConsoleLine()
    {

        int currentLineCursor = Console.CursorTop;

        Console.SetCursorPosition(0, Console.CursorTop);

        for (int i = 0; i < Console.WindowWidth; i++)
            Console.Write(" ");
        Console.SetCursorPosition(0, currentLineCursor);

    }

}