using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        public AuthController(IAuthRepository repo, IConfiguration config)
        {
            this._config = config;
            _repo = repo;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
        //[FromBody] is not needed as APIController infers the parameters automatically
        {
            //Validate request. (Below code is not required since [APIController] does the validation based on Data annotation.. )
            //if(!ModelState.IsValid)
            //{
            //    return BadRequest(ModelState);
            //}

            userForRegisterDto.UserName = userForRegisterDto.UserName.ToLower();
            if (await _repo.UserExist(userForRegisterDto.UserName))
            {
                return BadRequest("User Name already exists");
            }

            var userToCreate = new User { UserName = userForRegisterDto.UserName };

            var createdUser = await _repo.Register(userToCreate, userForRegisterDto.Password);

            //return CreatedAtRoute() TODO
            return StatusCode(201);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            var userFromRepo = await _repo.Login(userForLoginDto.UserName.ToLower(), userForLoginDto.password);
            if (userFromRepo == null)
                return Unauthorized();

            //Claims containing Name and ID
            var claims = new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier,userFromRepo.Id.ToString()),
                new Claim(ClaimTypes.Name,userFromRepo.UserName)
            };
            //key containing Secret
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value)); //key
           
           //Signing credentails using key and the Algo used for signing.
            var creds = new SigningCredentials(key,SecurityAlgorithms.HmacSha512Signature); //Signing credentials for hasing key

            //create token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var toeknHandler = new JwtSecurityTokenHandler();
            var token = toeknHandler.CreateToken(tokenDescriptor);

            return Ok( new {
                token = toeknHandler.WriteToken(token)
            });
        }
    }
}