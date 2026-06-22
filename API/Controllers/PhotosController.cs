using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class PhotosController : BaseApiController
    {
        private readonly IPhotoService _photoService;

        public PhotosController(IPhotoService photoService)
        {
            _photoService = photoService;
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<string>> AddPhoto(IFormFile file)
        {
            var url = await _photoService.AddPhotoAsync(file);
            return Ok(url);
        }
    }
}