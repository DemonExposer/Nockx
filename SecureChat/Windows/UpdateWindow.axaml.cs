using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace SecureChat.Windows;

public partial class UpdateWindow : Window {
	public UpdateWindow() {
		InitializeComponent();
	}

	public async Task Update(JsonObject releaseFileObject) {
		ProgressBar progressBar = this.FindControl<ProgressBar>("DownloadProgress")!;
		progressBar.Maximum = releaseFileObject["size"]!.GetValue<long>();

		string fileExtension = Environment.ProcessPath!.EndsWith(".exe") ? ".exe" : "";
        
		await Task.Run(() => {
			try {
				using HttpClient httpClient = new ();
				httpClient.DefaultRequestHeaders.Add("Accept", "application/octet-stream");
				httpClient.DefaultRequestHeaders.Add("User-Agent", "SecureChat");
				using Task<Stream> streamTask = httpClient.GetStreamAsync(releaseFileObject["url"]!.GetValue<string>());
				using FileStream fileStream = new (releaseFileObject["name"]!.GetValue<string>(), FileMode.Create);
				new Timer(_ => {
					if (fileStream.CanRead)
						Dispatcher.UIThread.InvokeAsync(() => progressBar.Value = fileStream.Position);
				}, null, 0, 100);
				streamTask.Result.CopyTo(fileStream);
				fileStream.Position = 0;
				
				ZipArchive zip = new (fileStream);
				foreach (ZipArchiveEntry entry in zip.Entries) {
					if (entry.Name != "Updater" + fileExtension)
						continue;
						
					using FileStream decompressedFileStream = new (entry.Name, FileMode.Create);
					using Stream compressedFileStream = entry.Open();
					compressedFileStream.CopyTo(decompressedFileStream);
				}
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		});
		
		Process.Start("Updater" + fileExtension, $"{releaseFileObject["name"]!.GetValue<string>()} {Environment.ProcessId}");
		Environment.Exit(0);
	}
}