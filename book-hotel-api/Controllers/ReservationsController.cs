﻿using AutoMapper;
using book_hotel_api.DTOs;
using book_hotel_api.Entities;
using book_hotel_api.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace book_hotel_api.Controllers
{
    [ApiController]
    [Route("api/hotel/room/reservations")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ReservationsController : Controller
    {
        private readonly ILogger<ReservationsController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IFileStorageService _fileStorageService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly string containerName = "hotels";

        public ReservationsController(ILogger<ReservationsController> logger, ApplicationDbContext context, IMapper mapper, IFileStorageService fileStorageService, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _context = context;
            _mapper = mapper;
            _fileStorageService = fileStorageService;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsHotelOwner")]
        async public Task<ActionResult<List<ReservationDTO>>> Get(int hotelId)
        { 
            var queryable = _context.Reservations.AsQueryable();
            var reservations = await queryable.OrderBy(x => x.Confirmation).Where(x => x.HotelId == hotelId).Where(x => x.Canceled == false).ToListAsync();

            return _mapper.Map<List<ReservationDTO>>(reservations);
        }

        [HttpGet("myreservations")]
        async public Task<ActionResult<List<ReservationDTO>>> Get()
        {
            var claim = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "email");

            if (claim == null)
            {
                return BadRequest("You are not logged in");
            }

            var email = claim.Value;
            var user = await _userManager.FindByEmailAsync(email);

            var queryable = _context.Reservations.AsQueryable();
            var reservations = await queryable.OrderBy(x => x.Confirmation).Where(x => x.UserId == user.Id).ToListAsync();

            return _mapper.Map<List<ReservationDTO>>(reservations);
        }

        [HttpGet("reservation/{Id:int}")]
        async public Task<ActionResult<ReservationDTO>> GetRes(int id)
        {
            var reservation = await _context.Reservations.FirstOrDefaultAsync(x => x.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            return _mapper.Map<ReservationDTO>(reservation);
        }

        [HttpPost("create")]
        async public Task<ActionResult> Post([FromForm] ReservationCreationDTO reservationCreationDTO)
        {
            var claim = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "email");

            if (claim == null)
            {
                return BadRequest("You are not logged in");
            }

            var email = claim.Value;
            var user = await _userManager.FindByEmailAsync(email);

            var reservation = _mapper.Map<Reservation>(reservationCreationDTO);
            reservation.UserId = user.Id;

            _context.Add(reservation);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("confirmation/{Id:int}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsHotelOwner")]
        async public Task<ActionResult> Put(int id, [FromForm] ReservationCreationDTO reservationCreationDTO)
        {
            var resevation = await _context.Reservations.FirstOrDefaultAsync(x => x.Id == id);

            if (resevation == null)
            {
                return NotFound();
            }

            resevation = _mapper.Map(reservationCreationDTO, resevation);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("delete/{Id:int}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "IsHotelOwner")]
        async public Task<ActionResult> Delete(int id)
        {
            var reservation = await _context.Reservations.FirstOrDefaultAsync(x => x.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            _context.Remove(reservation);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
