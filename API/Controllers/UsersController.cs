using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;
        private readonly IUserRepository _userRepository;
        public UsersController(IUserRepository userRepository, IMapper mapper,
            IPhotoService photoService)
        {
            _mapper=mapper;
            _photoService = photoService;
            _userRepository = userRepository;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers()
        {
            var users = await _userRepository.GetMembersAsync();

            return Ok(users);
        }

        [HttpGet("{username}")]

        public async Task<ActionResult<MemberDto>> GetUser(string username)
        {
            return await _userRepository.GetMemberAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
        {
            var user=await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            if(user==null) return NotFound();

            _mapper.Map(memberUpdateDto, user);

            if(await _userRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to update user");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            if(user==null) return NotFound();

            var url= await _photoService.AddPhotoAsync(file);

            if (url == null) return BadRequest("Problem uploading photo");

            var photo= new Photo
            {
                Url=url,
                IsMain = user.Photos.Count == 0
            };

            user.Photos.Add(photo);

            if (await _userRepository.SaveAllAsync())
           return CreatedAtAction(
                nameof(GetUser),
                new { username = user.UserName },
                _mapper.Map<PhotoDto>(photo));
            return BadRequest("Problem adding photo");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var user= await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            if(user==null) return NotFound();

            var photo =user.Photos.FirstOrDefault(x=>x.Id==photoId);

            if(photo==null)return NotFound();

            if(photo.IsMain) return BadRequest("this is already your main photo");

            var currentMain= user.Photos.FirstOrDefault(x=>x.IsMain);
            if(currentMain != null) currentMain.IsMain=false;
            photo.IsMain =true;

            if(await _userRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Problem setting the main photo");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            var photo= user.Photos.FirstOrDefault(x=>x.Id==photoId);

            if(photo==null) return NotFound();

            if(photo.IsMain) return BadRequest("You cannot delete your main photo");

            var fileName = Path.GetFileName(photo.Url);
            await _photoService.DeletePhotoAsync(fileName);

            user.Photos.Remove(photo);

            if(await _userRepository.SaveAllAsync()) return Ok();

            return BadRequest("Problem deleting photo");
        }
    }
}