using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace Updater;

public class Program {
	public static void Main(string[] args) {
		int processId = int.Parse(args[1]);

		try {
			Process process = Process.GetProcessById(processId);
			process.WaitForExit();
		} catch (ArgumentException) { }

		string fileExtension;
		using (FileStream fileStream = new (args[0], FileMode.Open)) {
			ZipArchive zip = new (fileStream);

			fileExtension = Environment.ProcessPath!.EndsWith(".exe") ? ".exe" : "";

			foreach (ZipArchiveEntry entry in zip.Entries) {
				if (entry.Name == "Updater" + fileExtension)
					continue;

				using FileStream decompressedFileStream = new (entry.Name, FileMode.Create);
				using Stream compressedFileStream = entry.Open();
				compressedFileStream.CopyTo(decompressedFileStream);
			}
		}

		File.Delete(args[0]);
		
		Process.Start("SecureChat" + fileExtension);
	}
}