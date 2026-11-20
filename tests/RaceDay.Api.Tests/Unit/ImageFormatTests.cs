using RaceDay.Api.Storage;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Unit;

public class ImageFormatTests
{
    [Fact]
    public void Png_is_recognised_by_its_signature() =>
        Assert.Equal(ImageFormat.Png, ImageFormat.Detect(TestImages.Png));

    [Fact]
    public void Jpeg_is_recognised_by_its_start_of_image_marker() =>
        Assert.Equal(ImageFormat.Jpeg, ImageFormat.Detect(TestImages.Jpeg));

    [Theory]
    [InlineData("GIF87a")]
    [InlineData("GIF89a")]
    public void Both_gif_versions_are_recognised(string signature) =>
        Assert.Equal(ImageFormat.Gif, ImageFormat.Detect(System.Text.Encoding.ASCII.GetBytes(signature + "\x01\x00\x01\x00")));

    [Fact]
    public void Webp_is_recognised_inside_its_riff_container()
    {
        byte[] header = [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WEBP"u8];

        Assert.Equal(ImageFormat.Webp, ImageFormat.Detect(header));
    }

    [Fact]
    public void A_riff_file_that_is_not_webp_is_refused()
    {
        byte[] wave = [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WAVE"u8];

        Assert.Null(ImageFormat.Detect(wave));
    }

    [Fact]
    public void Text_pdf_and_empty_files_are_refused()
    {
        Assert.Null(ImageFormat.Detect(TestImages.NotAnImage));
        Assert.Null(ImageFormat.Detect("%PDF-1.7\n"u8));
        Assert.Null(ImageFormat.Detect([]));
    }

    [Fact]
    public void Each_format_has_a_matching_content_type_and_extension()
    {
        Assert.Equal(("image/jpeg", ".jpg"), (ImageFormat.Jpeg.ContentType, ImageFormat.Jpeg.Extension));
        Assert.Equal(("image/png", ".png"), (ImageFormat.Png.ContentType, ImageFormat.Png.Extension));
        Assert.Equal(("image/gif", ".gif"), (ImageFormat.Gif.ContentType, ImageFormat.Gif.Extension));
        Assert.Equal(("image/webp", ".webp"), (ImageFormat.Webp.ContentType, ImageFormat.Webp.Extension));
    }
}
