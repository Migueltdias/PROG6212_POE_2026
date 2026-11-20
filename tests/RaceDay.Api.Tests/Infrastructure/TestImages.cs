using System.Net.Http.Headers;

namespace RaceDay.Api.Tests.Infrastructure;

public static class TestImages
{
    public static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0xFF, 0xD9];

    public static readonly byte[] NotAnImage = "This is a text file, not a picture."u8.ToArray();

    public static byte[] PngOfSize(int bytes)
    {
        var data = new byte[bytes];
        Png.AsSpan(0, Math.Min(Png.Length, bytes)).CopyTo(data);
        return data;
    }

    public static MultipartFormDataContent Form(byte[] bytes, string fileName = "picture.png", string contentType = "image/png")
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", fileName } };
    }
}
