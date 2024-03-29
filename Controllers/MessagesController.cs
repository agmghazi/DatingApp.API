using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers {
    [Authorize]
    [ServiceFilter (typeof (LogUserActivity))]
    [Route ("api/users/{userId}/[controller]")]

    public class MessagesController : Controller {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        public MessagesController (IDatingRepository repo, IMapper mapper) {
            _mapper = mapper;
            _repo = repo;
        }

        [HttpGet ("id", Name = "GetMessage")]
        public async Task<IActionResult> GetMessage (int userId, int id) {
            if (userId != int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value))
                return Unauthorized ();

            var messageFromRepo = await _repo.GetMessage (id);

            if (messageFromRepo == null)
                return NotFound ();

            return Ok (messageFromRepo);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessagesForUser (int userId, MessageParams messageParams)
        {
            if (userId != int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value))
                return Unauthorized ();

            var messagesFromRepo = await _repo.GetMessagesForUser(messageParams);    

            var messages = _mapper.Map<IEnumerable<MessageToReturnDto>>(messagesFromRepo);

            Response.AddPagination(messagesFromRepo.CurrentPage,
            messagesFromRepo.PageSize,messagesFromRepo
            .TotalCount,messagesFromRepo.TotalPages);

            return Ok(messages);
        }
        
        [HttpGet("thread/{id}")]
        public async Task<IActionResult> GetMessageThread(int userId, int id)
        {
            if (userId != int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value))
                return Unauthorized ();

           var messagesFromRepo = await _repo.GetMessagesThread(userId,id);

           var messagThread= _mapper.Map<IEnumerable<MessageToReturnDto>>(messagesFromRepo);
           
           return Ok (messagThread);     
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateMessage (int userId, [FromBody] MessageForCreationDto messageForCreationDto) {
            if (userId != int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value))
                return Unauthorized ();

            messageForCreationDto.SenderId = userId;

            var recipient = await _repo.GetUser (messageForCreationDto.RecipientId);

            if (recipient == null) {
                return BadRequest ("Could not find user");
            }

            var message = _mapper.Map<Message> (messageForCreationDto);

            _repo.Add (message);

            var messageToReturn = _mapper.Map<MessageForCreationDto> (message);

            if (await _repo.SaveAll ())
                return CreatedAtRoute ("GetMessage", new { id = message.Id }, messageToReturn);

            throw new Exception ("Creating the mesage failed on save");
        }
    }
}