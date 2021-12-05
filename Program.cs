// For Directory.GetFiles and Directory.GetDirectories
// For File.Exists, Directory.Exists
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

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

    static object ConsoleLock = new object();

    public static void Main()
    {

        Console.WriteLine("Enter folder path: ");
        string FolderPath = Console.ReadLine();
        Console.WriteLine(Environment.NewLine);

        if (Directory.Exists(FolderPath))
        {

            TestDirectory(FolderPath);

        }
        else
        {

            Console.WriteLine("{0} is not a valid file or directory.", FolderPath);

        }

    }

    private static void TestDirectory(string TargetDirectory)
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(TargetDirectory);
        DirectoryInfo[] directoryInfoArray = directoryInfo.GetDirectories();

        foreach (DirectoryInfo directories in directoryInfoArray)
        {
            string ThreadDirectory = directories.FullName;
            Thread WorkerThreadSubDirectories = new Thread(BeginThread);
            WorkerThreadSubDirectories.Start(ThreadDirectory);

        }

        Thread WorkerThreadMainDirectory = new Thread(BeginThread);
        WorkerThreadMainDirectory.Start(TargetDirectory);

    }

    public static void BeginThread(object ThreadDirectory)
    {

        //Console.WriteLine("Thrad for subdirectories and files beginning in directory: " + ThreadDirectory);

        DateTime EndingTime;
        TimeSpan TimeTaken;

        ulong SectorsPerCluster = 0;
        ulong BytesPerSector = 0;

        long TotalSizeOfFiles = 0;
        long TotalFileSlack = 0;

        ulong TotalNumberOfFiles = 0;
        ulong TotalNTFSCompressedFiles = 0;

        string targetDirectory = (string)ThreadDirectory;
        string BasePath = targetDirectory.Substring(0, 3);

        _ = GetDiskFreeSpace(BasePath, out SectorsPerCluster, out BytesPerSector, out _, out _);

        DateTime BeginningTime = DateTime.Now;

        ProcessDirectory(targetDirectory, BytesPerSector, SectorsPerCluster, ref TotalSizeOfFiles, ref TotalFileSlack, ref TotalNumberOfFiles, ref TotalNTFSCompressedFiles);

        EndingTime = DateTime.Now;
        TimeTaken = EndingTime.Subtract(BeginningTime);

        lock(ConsoleLock)
        {

            Console.WriteLine(Environment.NewLine, "-----", Environment.NewLine);

            Console.WriteLine("Time began: " + BeginningTime);
            Console.WriteLine("Time ended: " + EndingTime);
            Console.WriteLine("Time taken: " + TimeTaken);

            Console.WriteLine("Total Size of files in directory {0} is: {1}", targetDirectory, TotalSizeOfFiles);
            Console.WriteLine("Total file slack of files in directory {0} is: {1}", targetDirectory, TotalFileSlack);
            Console.WriteLine("Total NTFS Compressed Files in directory {0} is: {1}", targetDirectory, TotalNTFSCompressedFiles);
            Console.WriteLine("Total number of files scanned in directory {0} is: {1}", targetDirectory, TotalNumberOfFiles);

        }

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

            if (targetDirectory.Length > 4)
            {

                // Recurse into subdirectories of this directory.
                string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
                foreach (string subdirectory in subdirectoryEntries)
                    ProcessDirectory(subdirectory, BytesPerSector, SectorsPerCluster, ref TotalSizeOfFiles, ref TotalFileSlack, ref TotalNumberOfFiles, ref TotalNTFSCompressedFiles);

            }

        }

        catch (System.UnauthorizedAccessException)
        {

            Console.WriteLine("{0} is unauthorized", targetDirectory);

        }

    }

    public static void ProcessFile(string path, ulong BytesPerSector, ulong SectorsPerCluster, ref long TotalSizeOfFiles, ref long TotalFileSlack, ref ulong TotalNumberOfFiles, ref ulong TotalNTFSCompressedFiles)
    {
        TotalNumberOfFiles++;

        //Console.Write("\rProcessing file: " + path);

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


        //Console.WriteLine("File slack is for file " + path + " with a size of " + FileSize + " is : " + FileSlack);

        TotalSizeOfFiles += Convert.ToInt64(FileSizeOnDisk);
        TotalFileSlack += Convert.ToInt64(FileSlack);

        //ClearCurrentConsoleLine();

    }

}