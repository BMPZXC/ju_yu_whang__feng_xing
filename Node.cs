using System;
using QRCoder;
using System.Drawing;
public partial class Node : Godot.Node
{
	// 定义一个方法来返回二维码图像的字节数组
	public static byte[] GenerateQRCodeBytes(string text){
		// 生成二维码数据
		if (string.IsNullOrEmpty(text))
		{
			throw new ArgumentException("输入文本不能为空", nameof(text));
		}

		// 创建 QRCodeGenerator 实例
		QRCodeGenerator qrGenerator = new();
		// 创建 QRCodeData 对象
		QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
		// 使用 PngByteQRCode 生成 PNG 格式的二维码字节数组
		using var qrCode = new PngByteQRCode(qrCodeData);
		// 深蓝模块 + 黄色背景
		Color moduleColor = ColorTranslator.FromHtml("#323e73"); 
		Color backgroundColor = ColorTranslator.FromHtml("#faede3");
		byte[] qrCodeBytes = qrCode.GetGraphic(20, moduleColor, backgroundColor);
		return qrCodeBytes;
  }
	//[GodotExport]
	
}
