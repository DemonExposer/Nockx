using System.IO;
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

		// TODO: Add code to unpack the zip file
		await Task.Run(() => {
			using HttpClient httpClient = new ();
			using Task<Stream> streamTask = httpClient.GetStreamAsync(releaseFileObject["browser_download_url"]!.GetValue<string>());
			using FileStream fileStream = new (releaseFileObject["name"]!.GetValue<string>(), FileMode.Create);
			new Timer(_ => {
				if (fileStream.CanRead)
					Dispatcher.UIThread.InvokeAsync(() => progressBar.Value = fileStream.Position);
			}, null, 0, 100);
			streamTask.Result.CopyTo(fileStream);
		});
	}
}