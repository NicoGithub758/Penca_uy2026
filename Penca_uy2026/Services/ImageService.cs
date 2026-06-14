using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Penca_uy2026.Services
{
    public class ImageService
    {
        private readonly Cloudinary _cloudinary;

        public ImageService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        /// <summary>
        /// Sube una imagen a Cloudinary y retorna la URL segura.
        /// </summary>
        /// <param name="fileStream">Stream del archivo a subir.</param>
        /// <param name="fileName">Nombre del archivo original.</param>
        /// <param name="folderPath">Carpeta destino en Cloudinary (opcional).</param>
        /// <returns>URL segura (HTTPS) de la imagen subida.</returns>
        public async Task<string?> UploadImageAsync(Stream fileStream, string fileName, string folderPath = "tupenca")
        {
            if (fileStream == null || fileStream.Length == 0)
                return null;

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(fileName, fileStream),
                Folder = folderPath,
                // Transformación: 500x500 píxeles, recorta rellenando ("fill"), 
                // y "auto" le dice a la IA de Cloudinary que busque la cara, 
                // o si no hay cara (ej. un escudo o mascota), centre en lo más relevante.
                Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Error uploading image to Cloudinary: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }
    }
}
