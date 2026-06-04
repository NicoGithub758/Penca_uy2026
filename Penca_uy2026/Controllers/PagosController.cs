using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Data;
using Penca_uy2026.DTOs;
using Penca_uy2026.Models;
using Penca_uy2026.Services;
using System.Security.Claims;

namespace Penca_uy2026.Controllers
{
    [ApiController]
    [Route("api/pagos")]
    [Authorize(Roles = "Jugador")]
    public class PagosController : ControllerBase
    {
        private readonly MyDbContext _context;
        private readonly PayPalService _paypalService;

        public PagosController(MyDbContext context, PayPalService paypalService)
        {
            _context = context;
            _paypalService = paypalService;
        }

        /// <summary>
        /// Crea una orden de pago en PayPal para participar en una penca.
        /// POST /api/pagos/crear-orden
        /// </summary>
        [HttpPost("crear-orden")]
        public async Task<IActionResult> CrearOrden([FromBody] CrearPagoRequest request)
        {
            // 1. Obtener el usuario del JWT
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdClaim, out int usuarioSitioId))
                return Unauthorized();

            // 2. Verificar que la penca instancia existe
            var pencaInstancia = await _context.PencaInstancias
                .Include(p => p.Penca)
                .FirstOrDefaultAsync(p => p.Id == request.PencaInstanciaId);

            if (pencaInstancia == null)
                return NotFound("La penca no existe.");

            // 3. Verificar que el usuario no esté ya participando
            var yaParticipa = await _context.Participaciones
                .AnyAsync(p => p.UsuarioSitioId == usuarioSitioId && p.PencaInstanciaId == request.PencaInstanciaId);

            if (yaParticipa)
                return BadRequest("Ya estás participando en esta penca.");

            // 4. Crear la participación en estado "no pagado"
            var participacion = new Participacion
            {
                UsuarioSitioId = usuarioSitioId,
                PencaInstanciaId = request.PencaInstanciaId,
                SitioId = pencaInstancia.SitioId,
                EstaPagado = false,
                PuntajeTotal = 0
            };

            _context.Participaciones.Add(participacion);
            await _context.SaveChangesAsync();

            // 5. Crear registro de pago en estado PENDING
            var pago = new Pago
            {
                Monto = pencaInstancia.Costo,
                Estado = "PENDING",
                MetodoPago = "PayPal",
                ParticipacionId = participacion.Id,
                SitioId = pencaInstancia.SitioId,
                FechaPago = DateTime.UtcNow
            };

            _context.Pagos.Add(pago);
            await _context.SaveChangesAsync();

            // 6. Crear orden en PayPal
            try
            {
                var orderId = await _paypalService.CrearOrdenAsync(pencaInstancia.Costo);

                // Guardar el Order ID en el pago
                pago.IdTransaccionExterna = orderId;
                await _context.SaveChangesAsync();

                return Ok(new CrearPagoResponse
                {
                    OrderId = orderId,
                    PagoId = pago.Id
                });
            }
            catch (Exception ex)
            {
                _context.Pagos.Remove(pago);
                _context.Participaciones.Remove(participacion);
                await _context.SaveChangesAsync();

                // Log detallado del error
                var errorMsg = ex.InnerException?.Message ?? ex.Message;
                var errorDetail = ex.ToString();
                Console.WriteLine($"[ERROR PAYPAL] {errorDetail}");

                return StatusCode(500, new
                {
                    error = errorMsg,
                    detalle = "Error al crear orden en PayPal"
                });
            }
        }

        /// <summary>
        /// Confirma un pago después de que el usuario aprobó en PayPal.
        /// POST /api/pagos/confirmar
        /// </summary>
        [HttpPost("confirmar")]
        public async Task<IActionResult> ConfirmarPago([FromBody] ConfirmarPagoRequest request)
        {
            // 1. Buscar el pago
            var pago = await _context.Pagos
                .Include(p => p.Participacion)
                .FirstOrDefaultAsync(p => p.Id == request.PagoId);

            if (pago == null)
                return NotFound("Pago no encontrado.");

            if (pago.Estado == "COMPLETED")
                return BadRequest("Este pago ya fue confirmado.");

            // 2. Capturar la orden en PayPal
            var (success, status) = await _paypalService.CapturarOrdenAsync(request.OrderId);

            if (!success)
            {
                pago.Estado = "FAILED";
                await _context.SaveChangesAsync();
                return BadRequest("El pago fue rechazado por PayPal.");
            }

            // 3. Marcar el pago como completado
            pago.Estado = "COMPLETED";
            pago.Participacion.EstaPagado = true;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Pago confirmado exitosamente. Ya podés hacer tus predicciones.",
                participacionId = pago.ParticipacionId
            });
        }

        /// <summary>
        /// Obtiene el historial de pagos del usuario autenticado.
        /// GET /api/pagos/mis-pagos
        /// </summary>
        [HttpGet("mis-pagos")]
        public async Task<IActionResult> MisPagos()
        {
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(usuarioIdClaim, out int usuarioSitioId))
                return Unauthorized();

            var pagos = await _context.Pagos
                .Include(p => p.Participacion)
                    .ThenInclude(pa => pa.PencaInstancia)
                        .ThenInclude(pi => pi.Penca)
                .Where(p => p.Participacion.UsuarioSitioId == usuarioSitioId)
                .OrderByDescending(p => p.FechaPago)
                .Select(p => new
                {
                    p.Id,
                    p.Monto,
                    p.Estado,
                    p.MetodoPago,
                    p.FechaPago,
                    Penca = p.Participacion.PencaInstancia.Penca.Nombre
                })
                .ToListAsync();

            return Ok(pagos);
        }
    }
}
