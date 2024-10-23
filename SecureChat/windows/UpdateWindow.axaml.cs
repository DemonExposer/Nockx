using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace SecureChat.windows;

public partial class UpdateWindow : Window {
	public UpdateWindow() {
		InitializeComponent();
	}

	public async Task Update(JsonObject releaseFileObject) {
		ProgressBar progressBar = this.FindControl<ProgressBar>("DownloadProgress")!;
		progressBar.Maximum = releaseFileObject["size"]!.GetValue<long>();
		
		await Task.Run(() => {
			try {
				using HttpClient httpClient = new ();
				using Task<Stream> streamTask = httpClient.GetStreamAsync(releaseFileObject["browser_download_url"]!.GetValue<string>());
				using FileStream fileStream = new (releaseFileObject["name"]!.GetValue<string>(), FileMode.Create);
				new Timer(_ => {
					if (fileStream.CanRead)
						Dispatcher.UIThread.InvokeAsync(() => progressBar.Value = fileStream.Position);
				}, null, 0, 100);
				streamTask.Result.CopyTo(fileStream);
				fileStream.Position = 0;
				ZipArchive zip = new (fileStream);
				foreach (ZipArchiveEntry entry in zip.Entries) { // TODO: fix self-overwriting issue
					using FileStream decompressedFileStream = new (entry.Name, FileMode.Create);
					using Stream compressedFileStream = entry.Open();
					compressedFileStream.CopyTo(decompressedFileStream);
				}
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		});
	}
}