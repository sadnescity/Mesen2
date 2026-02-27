using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Mesen.Mcp
{
	public class MesenMcpServer
	{
		private static readonly MesenMcpServer _instance = new();
		public static MesenMcpServer Instance => _instance;

		private WebApplication? _app;
		private volatile bool _isRunning;
		private ILogger? _logger;

		public bool IsRunning => _isRunning;

		private MesenMcpServer() { }

		public string? LastError { get; private set; }

		public async Task StartAsync(int port)
		{
			if(_isRunning) {
				return;
			}

			try {
				WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions {
					Args = Array.Empty<string>()
				});

				builder.Logging.ClearProviders();
				builder.Logging.AddConsole(options => {
					options.LogToStandardErrorThreshold = LogLevel.Trace;
				});

				builder.WebHost.UseUrls($"http://localhost:{port}");

				builder.Services
					.AddMcpServer(options => {
						options.ServerInfo = new() {
							Name = "Mesen Emulator",
							Version = Interop.EmuApi.GetMesenVersion().ToString(3)
						};
					})
					.WithHttpTransport()
					.WithToolsFromAssembly()
					.WithPromptsFromAssembly()
					.WithResourcesFromAssembly();

				_app = builder.Build();
				_app.MapMcp("/mcp");

				_logger = _app.Services.GetService<ILoggerFactory>()?.CreateLogger<MesenMcpServer>();

				await _app.StartAsync();
				_isRunning = true;
				LastError = null;
				_logger?.LogInformation("MCP server started on port {Port}", port);
			} catch(Exception ex) {
				LastError = ex.ToString();
				_isRunning = false;
				_logger?.LogError(ex, "Failed to start MCP server");
			}
		}

		public async Task StopAsync()
		{
			if(!_isRunning || _app == null) {
				return;
			}

			try {
				await _app.StopAsync();
				await _app.DisposeAsync();
				_logger?.LogInformation("MCP server stopped");
			} catch {
				// Ignore shutdown errors
			}
			_app = null;
			_isRunning = false;
			_logger = null;
		}
	}
}
