using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Services;

namespace RaceDay.Api.Dtos;

/// <summary>An image file sent as multipart/form-data.</summary>
public class ImageUploadRequest
{
    public const long MaxRequestBytes = 10 * 1024 * 1024;

    /// <summary>A JPEG, PNG, GIF or WebP image of up to 5 MB.</summary>
    [FromForm(Name = ImageService.Field)]
    [Required(ErrorMessage = "Choose an image to upload.")]
    public IFormFile? File { get; set; }

    public ImageUpload ToUpload() => new(File!.OpenReadStream(), File.Length, File.FileName);
}
