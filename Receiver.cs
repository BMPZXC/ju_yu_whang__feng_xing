using Godot;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using EmbedIO;
using EmbedIO.WebApi;
using EmbedIO.Routing;
// using EmbedIO.Files;
using HttpMultipartParser;
using Swan.Logging;
using System.Threading;

#pragma warning disable CA1050 // 在命名空间中声明类型

public partial class Receiver : Node
#pragma warning restore CA1050 // 在命名空间中声明类型

{
	private WebServer server;
	private CancellationTokenSource cts;

	[Signal]
	public delegate void suaxingEventHandler();

	private string uploadPath = "";

	public string Up
	{
		get => uploadPath;
		set
		{
			if (value == uploadPath) { return; }
			uploadPath = value;
			Sezi("upload", "path", uploadPath);
		}
	}

	public string Fx
	{
		get => Duqu("fx", version.ToString());
		set
		{
			version = (version + 1) % 10;
			Sezi("fx", version.ToString(), value);
			Sezi("fx", "jisu", version.ToString());
		}
	}

	public int version = 0;
	public string ip = "";

	public override void _Ready()
	{
		// 禁用 EmbedIO 的日志输出
		Logger.NoLogging();

		uploadPath = Duqu("upload", "path");
		if (!Directory.Exists(uploadPath)) { uploadPath = ""; }

		string va = Duqu("fx", "jisu").Trim();
		version = string.IsNullOrEmpty(va) ? 0 : int.Parse(va);

		string lanIP = GetLanIPAddress();
		string port = Duqu("server", "port");
		try
		{
			using var listener = new TcpListener(IPAddress.Parse(lanIP), int.Parse(port));
			listener.Start();
			listener.Stop();
		}
		catch
		{
			port = "";
		}
		if (port == "")
		{
			port = GetAvailablePort().ToString();
			Sezi("server", "port", port);
		}

		ip = $"http://{lanIP}:{port}/";

		// 创建 EmbedIO 服务器
		server = CreateWebServer(lanIP, int.Parse(port));
		cts = new CancellationTokenSource();

		StartServer();
	}

	private WebServer CreateWebServer(string ip, int port)
	{
		var url = $"http://{ip}:{port}/";
		var server = new WebServer(o => o
						.WithUrlPrefix(url)
						.WithMode(HttpListenerMode.EmbedIO))
				.WithLocalSessionManager()
				.WithWebApi("/", m => m
						.WithController(() => new ApiController(this, cts)))
				.HandleHttpException((ctx, ex) =>
				{
					// 把真实异常信息输出到响应里（仅开发/调试阶段使用）
					return ctx.SendStandardHtmlAsync(500, writer => writer.Write(ex.ToString()));

				});
		return server;

	}

	private async void StartServer()
	{
		try
		{
			await server.RunAsync(cts.Token);
			GD.Print($"HTTP Server started on {ip}");
		}
		catch (OperationCanceledException)
		{
			// 服务器正常停止
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Server error: {ex.Message}");
		}
	}

	// API 控制器
	public class ApiController(Receiver parent, CancellationTokenSource cts) : WebApiController
	{
		private readonly Receiver parent = parent;
		private readonly CancellationTokenSource cts = cts;

		// 

		[Route(HttpVerbs.Get, "/{id}")]
		public async Task DownloadFile_html(int id)
		{
			// GD.Print($"DownloadFile: {id}");
			try
			{
				var html = Godot.FileAccess.Open("res://xiazai.html", Godot.FileAccess.ModeFlags.Read);
				if (html != null)
				{
					HttpContext.Response.ContentType = "text/html; charset=utf-8";
					await HttpContext.SendStringAsync(html.GetAsText(), "text/html", Encoding.UTF8);
				}
				else
				{
					HttpContext.Response.StatusCode = 404;
					await HttpContext.SendStringAsync("{\"error\":\"HTML file not found\"}", "application/json", Encoding.UTF8);
				}
			}
			catch (Exception ex)
			{
				HttpContext.Response.StatusCode = 500;
				await HttpContext.SendStringAsync($"{{\"error\":\"{ex.Message}\"}}", "application/json", Encoding.UTF8);
			}
		}

		[Route(HttpVerbs.Get, "/lujing/{id}")]
		public async Task DownloadFile_lujing(int id)
		{
			// GD.Print($"DownloadFile: {id}");
			try
			{
				// 使用父类的 Duqu 方法
				string filePath = Duqu("fx", id.ToString());

				if (string.IsNullOrEmpty(filePath))
				{
					HttpContext.Response.StatusCode = 404;
					await HttpContext.SendStringAsync("{\"error\":\"{未选择文件}\"}", "application/json", Encoding.UTF8);
					return;
				}

				if (!File.Exists(filePath))
				{
					if (id == parent.version)
					{
						// 使用父类的 Sezi 方法
						Sezi("fx", id.ToString(), "");
						parent.CallDeferred("emit_signal", "suaxing");
					}
					HttpContext.Response.StatusCode = 404;
					await HttpContext.SendStringAsync("{\"error\":\"文件不存在\"}", "application/json", Encoding.UTF8);
					return;
				}
				HttpContext.Response.StatusCode = 200;
				var fileName = Path.GetFileName(filePath);
				await HttpContext.SendStringAsync($"{{\"error\":\"{fileName}\"}}", "application/json", Encoding.UTF8);
			}
			catch (Exception ex)
			{
				HttpContext.Response.StatusCode = 500;
				await HttpContext.SendStringAsync($"{{\"error\":\"{ex.Message}\"}}", "application/json", Encoding.UTF8);
			}
		}

		[Route(HttpVerbs.Get, "/{id}/{lujing}")]
		public async Task DownloadFile(int id, string lujing)
		{
			// GD.Print($"/{id}/xiazai");
			try
			{
				// 使用父类的 Duqu 方法
				string filePath = Duqu("fx", id.ToString());

				if (string.IsNullOrEmpty(filePath))
				{
					HttpContext.Response.StatusCode = 404;
					await HttpContext.SendStringAsync("{\"error\":\"未选择文件\"}", "application/json", Encoding.UTF8);
					return;
				}

				if (!File.Exists(filePath))
				{
					if (id == parent.version)
					{
						// 使用父类的 Sezi 方法
						Sezi("fx", id.ToString(), "");
						parent.CallDeferred("emit_signal", "suaxing");
					}
					HttpContext.Response.StatusCode = 404;
					await HttpContext.SendStringAsync("{\"error\":\"文件不存在\"}", "application/json", Encoding.UTF8);
					return;
				}

				var fileInfo = new FileInfo(filePath);
				HttpContext.Response.ContentLength64 = fileInfo.Length;
				// var fileName = Path.GetFileName(filePath);
				HttpContext.Response.Headers.Add("Content-Disposition",
						$"inline; filename=\"{Uri.EscapeDataString(lujing)}\"");
				HttpContext.Response.ContentType = GetMimeType(Path.GetExtension(filePath));
				using var fileStream = File.OpenRead(filePath);
				await fileStream.CopyToAsync(HttpContext.Response.OutputStream, cts.Token);
			}
			catch (Exception ex)
			{
				HttpContext.Response.StatusCode = 500;
				await HttpContext.SendStringAsync($"{{\"error\":\"{ex.Message}\"}}", "application/json", Encoding.UTF8);
			}
		}

		// 
		[Route(HttpVerbs.Get, "/")]
		public async Task GetHomepage()
		{
			try
			{
				var html = Godot.FileAccess.Open("res://zuye.html", Godot.FileAccess.ModeFlags.Read);
				if (html != null)
				{
					HttpContext.Response.ContentType = "text/html; charset=utf-8";
					await HttpContext.SendStringAsync(html.GetAsText(), "text/html", Encoding.UTF8);
				}
				else
				{
					HttpContext.Response.StatusCode = 404;
					await HttpContext.SendStringAsync("{\"error\":\"HTML file not found\"}", "application/json", Encoding.UTF8);
				}
			}
			catch (Exception ex)
			{
				HttpContext.Response.StatusCode = 500;
				await HttpContext.SendStringAsync($"{{\"error\":\"{ex.Message}\"}}", "application/json", Encoding.UTF8);
			}
		}

		[Route(HttpVerbs.Post, "/up")]
		public async Task<string> UploadFile()
		{
			try
			{
				if (string.IsNullOrEmpty(parent.uploadPath))
					return "{\"error\":\"接收方 未指定文件夹\"}";

				if (!Directory.Exists(parent.uploadPath))
				{
					parent.uploadPath = "";
					parent.CallDeferred("emit_signal", "suaxing");
					return "{\"error\":\"接收方的文件夹不存在\"}";
				}

				// 检查 Content-Type
				var contentType = HttpContext.Request.ContentType;
				if (string.IsNullOrEmpty(contentType) || !contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
					return "{\"error\":\"只能用于上传\"}";

				// 获取边界字符串
				var boundaryMatch = Regex.Match(contentType, @"boundary=(.+)");
				if (!boundaryMatch.Success)
					return "{\"error\":\"无效的multipart格式\"}";

				var boundary = boundaryMatch.Groups[1].Value.Trim();
				var uploadedFiles = new List<string>();

				// 使用 HttpMultipartParser 解析请求
				var parser = await MultipartFormDataParser.ParseAsync(HttpContext.Request.InputStream, boundary, Encoding.UTF8).ConfigureAwait(false);

				foreach (var file in parser.Files)
				{
					if (file.FileName == null) continue;

					string safeFileName = Path.GetFileName(file.FileName);
					if (string.IsNullOrEmpty(safeFileName))
						continue;

					string baseFilePath = Path.Combine(parent.uploadPath, safeFileName);
					string filePath = baseFilePath;
					int count = 1;

					while (File.Exists(filePath))
					{
						string nameWithoutExt = Path.GetFileNameWithoutExtension(safeFileName);
						string ext = Path.GetExtension(safeFileName);
						filePath = Path.Combine(parent.uploadPath, $"{nameWithoutExt}({count}){ext}");
						count++;
					}

					// 保存文件
					using (var fileStream = File.Create(filePath))
					{
						await file.Data.CopyToAsync(fileStream).ConfigureAwait(false);
					}

					uploadedFiles.Add(Path.GetFileName(filePath));
				}

				// 返回成功响应
				return JsonSerializer.Serialize(new
				{
					success = true,
					count = uploadedFiles.Count,
					files = uploadedFiles
				});
			}
			catch (Exception ex)
			{
				HttpContext.Response.StatusCode = 500;
				return $"{{\"error\":\"{ex.Message}\"}}";
			}
		}

		// 在控制器内部定义辅助方法，或者通过 parent 调用
		private static string GetMimeType(string fileExtension) => fileExtension.ToLower() switch
		{
			".txt" => "text/plain",
			".html" => "text/html",
			".htm" => "text/html",
			".css" => "text/css",
			".js" => "application/javascript",
			".json" => "application/json",
			".jpg" => "image/jpeg",
			".jpeg" => "image/jpeg",
			".png" => "image/png",
			".gif" => "image/gif",
			".pdf" => "application/pdf",
			".zip" => "application/zip",
			".doc" => "application/msword",
			".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
			".xls" => "application/vnd.ms-excel",
			".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
			".ppt" => "application/vnd.ms-powerpoint",
			".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
			_ => "application/octet-stream"
		};
	}


	public static void Sezi(string a, string b, string c)
	{
		var config = new ConfigFile();
		Error error = config.Load("user://ip.cfg");
		if (error != Error.Ok)
		{
			config = new ConfigFile();
		}
		config.SetValue(a, b, c);
		config.Save("user://ip.cfg");
	}

	public static string Duqu(string a, string b)
	{
		var config = new ConfigFile();
		Error error = config.Load("user://ip.cfg");
		if (error != Error.Ok)
		{
			config = new ConfigFile();
		}
		return (string)config.GetValue(a, b, "");
	}

	private static int GetAvailablePort()
	{
		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		int port = ((IPEndPoint)listener.LocalEndpoint).Port;
		listener.Stop();
		return port;
	}

	private static string GetLanIPAddress()
	{
		return NetworkInterface.GetAllNetworkInterfaces()
				.Where(nic => nic.OperationalStatus == OperationalStatus.Up)
				.SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
				.Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork &&
										 !IPAddress.IsLoopback(ip.Address))
				.FirstOrDefault()?.Address.ToString() ?? "127.0.0.1";
	}

	public override void _ExitTree()
	{
		cts?.Cancel();
		server?.Dispose();
	}


}

