﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DTO;
using Notifications;
using DataAccess.Models;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketDTOController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notification;

        public TicketDTOController(AppDbContext context, NotificationService notification)
        {
            _context = context;
            _notification = notification;
        }

        // GET: api/TicketDTO
        [HttpGet]
        public async Task<ActionResult<List<TicketDTO>>> GetTickets()
        {
            var tickets = await _context.Tickets.ToListAsync();

            var ticketDTOs = tickets.Select(t => new TicketDTO
            {
                Id = t.Id,
                UserId = t.UserId,
                Latitude = t.Latitude,
                Longitude = t.Longitude,
                Description = t.Description,
                Amount = t.Amount,
                Date = t.Date,
                Status = t.Status,
                OfficerId = t.OfficerId,
                Plate = t.Plate
            }).ToList();

            return Ok(ticketDTOs);
        }

        // GET: api/TicketDTO/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TicketDTO>> GetTicket(string id)
        {
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
            {
                return NotFound();
            }

            var ticketDTO = new TicketDTO
            {
                Id = ticket.Id,
                UserId = ticket.UserId,
                Latitude = ticket.Latitude,
                Longitude = ticket.Longitude,
                Description = ticket.Description,
                Amount = ticket.Amount,
                Date = ticket.Date,
                Status = ticket.Status,
                OfficerId = ticket.OfficerId,
                Plate = ticket.Plate
            };

            return ticketDTO;
        }

        // GET: api/TicketDTO/search
        [HttpGet("search")]
        public async Task<ActionResult<List<TicketDTO>>> GetTicketsByPlate([FromQuery] string placa)
        {
            if (string.IsNullOrEmpty(placa))
            {
                return BadRequest("El parámetro placa es requerido.");
            }

            // Realizar la consulta sin usar StringComparison
            var tickets = await _context.Tickets
                .Where(t => t.Plate != null && EF.Functions.Like(t.Plate, placa))
                .ToListAsync();

            if (!tickets.Any())
            {
                return NotFound("No se encontraron multas asociadas a esta placa.");
            }

            // Mapear a DTO
            var ticketDTOs = tickets.Select(t => new TicketDTO
            {
                Id = t.Id,
                UserId = t.UserId,
                Latitude = t.Latitude,
                Longitude = t.Longitude,
                Description = t.Description,
                Amount = t.Amount,
                Date = t.Date,
                Status = t.Status,
                OfficerId = t.OfficerId,
                Plate = t.Plate
            }).ToList();

            return Ok(ticketDTOs);
        }

        // PUT: api/TicketDTO/{id}/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateTicketStatus(string id, [FromBody] TicketStatusUpdateDto statusUpdate)
        {
            if (statusUpdate == null || string.IsNullOrEmpty(statusUpdate.Status))
            {
                return BadRequest("Invalid status update request.");
            }

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            ticket.Status = statusUpdate.Status;

            _context.Entry(ticket).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                var user = await _context.Users.FirstAsync(u => u.UserId == ticket.UserId);

                _notification.StatusChangesNotification(ticket.Status, user.Email, ticket.Id, "Cambio de estado de multa");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TicketExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // PUT: api/TicketDTO/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTicket(string id, [FromBody] TicketUpdateDto ticketUpdate)
        {
            if (ticketUpdate == null || string.IsNullOrEmpty(ticketUpdate.Description) || ticketUpdate.Amount <= 0)
            {
                return BadRequest("Invalid ticket update request.");
            }

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            // Actualizar los campos Description y Amount
            ticket.Description = ticketUpdate.Description;
            ticket.Amount = ticketUpdate.Amount;

            // Marcar la entidad como modificada
            _context.Entry(ticket).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TicketExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        private decimal GetAmountForDescription(string description)
        {
            var ticketsInfo = new Dictionary<string, decimal>
            {
                { "Exceso de velocidad", 68000 },
                { "Mal estacionamiento", 52000 },
                { "Conducir en estado de ebriedad", 240000 },
                { "Conducir sin licencia", 27000 }
            };

            return ticketsInfo.ContainsKey(description) ? ticketsInfo[description] : 0;
        }

        // GET: api/TicketDTO/heatmap
        [HttpGet("heatmap")]
        public async Task<ActionResult<List<HeatMapData>>> GetHeatMapData()
        {
            // Obtenemos todos los tickets
            var tickets = await _context.Tickets.ToListAsync();

            // Extraemos solo las coordenadas (latitud, longitud) de los tickets
            var heatMapData = tickets.Select(t => new HeatMapData
            {
                Latitude = t.Latitude,
                Longitude = t.Longitude
            }).ToList();

            return Ok(heatMapData);
        }

        // POST: api/TicketDTO
        [HttpPost]
        public async Task<ActionResult<CreateTicketDTO>> PostTicket(CreateTicketDTO ticket)
        {
            var ticketEntity = new Ticket
            {
                Id = ticket.Id,
                UserId = ticket.UserId,
                Latitude = ticket.Latitude,
                Longitude = ticket.Longitude,
                Description = ticket.Description,
                Amount = ticket.Amount,
                Date = ticket.Date,
                Status = ticket.Status,
                OfficerId = ticket.OfficerId,
                Plate = ticket.Plate
            };

            _context.Tickets.Add(ticketEntity);
            try
            {
                await _context.SaveChangesAsync();

                var user = _context.Users.First(u => u.UserId == ticket.UserId);
                _notification.AutomaticUserNotification(ticket.Amount.ToString(), ticket.Plate, user.Email, ticket.Id, "", ticket.Date.ToString());

            }
            catch (DbUpdateException)
            {
                if (TicketExists(ticket.Id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }
            return CreatedAtAction("GetTicket", new { id = ticket.Id }, ticket);
        }

        // DELETE: api/TicketDTO/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTicket(string id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TicketExists(string id)
        {
            return _context.Tickets.Any(e => e.Id == id);
        }
    }



    public class TicketStatusUpdateDto
    {
        public string Status { get; set; }
    }

    public class TicketUpdateDto
    {
        public string Description { get; set; }
        public double Amount { get; set; }
    }

    public class CreateTicketDTO
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public DateTime Date { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string OfficerId { get; set; }
        public double Amount { get; set; }
        public string Plate { get; set; }
    }
}
