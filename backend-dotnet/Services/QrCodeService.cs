using QRCoder;
using System.Text;

namespace BackendDotnet.Services
{
    public class QrCodeService
    {
        private readonly IWebHostEnvironment _env;

        public QrCodeService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string GenerateQrCodeBase64(string data)
        {
            using var generator = new QRCodeGenerator();
            var qrData = generator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            var svg = new SvgQRCode(qrData);
            var svgContent = svg.GetGraphic(4);
            var bytes = Encoding.UTF8.GetBytes(svgContent);
            return Convert.ToBase64String(bytes);
        }

        public async Task<string> SaveQrCodePngAsync(string data, string fileName)
        {
            using var generator = new QRCodeGenerator();
            var qrData = generator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            using var png = new PngByteQRCode(qrData);
            var bytes = png.GetGraphic(4);

            var dir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "qrcodes");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, fileName);
            await File.WriteAllBytesAsync(path, bytes);
            return $"/qrcodes/{fileName}";
        }

        public async Task<string> SaveTicketPdfAsync(string html, string fileName)
        {
            var dir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "tickets");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, fileName);
            await File.WriteAllTextAsync(path, html);
            return $"/tickets/{fileName}";
        }
    }
}
