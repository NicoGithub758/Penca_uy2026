using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Penca_uy2026.Models;
using Penca_uy2026.Data;
namespace Penca_uy2026.Hubs;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class ChatHub : Hub
{
    private readonly MyDbContext _context;

    private int UsuarioId => int.Parse(Context.User!.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    private string NombreUsuario => Context.User!.FindFirst(System.Security.Claims.ClaimTypes.Name)!.Value;
    private int SitioId => int.Parse(Context.User!.FindFirst("sitioId")!.Value);

    public ChatHub(MyDbContext context)
    {
        _context = context;
    }

    public async Task UnirseASala(int participacionId)
{
    try
    {
        Console.WriteLine($"SignalR UnirseASala llamado. participacionId={participacionId}");
        Console.WriteLine($"Usuario autenticado: {Context.User?.Identity?.IsAuthenticated}");

        foreach (var claim in Context.User?.Claims ?? [])
        {
            Console.WriteLine($"CLAIM: {claim.Type} = {claim.Value}");
        }

        var usuarioIdClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var nombreClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var sitioIdClaim = Context.User?.FindFirst("sitioId")?.Value;

        Console.WriteLine($"NameIdentifier={usuarioIdClaim}");
        Console.WriteLine($"Name={nombreClaim}");
        Console.WriteLine($"sitioId={sitioIdClaim}");

        if (!int.TryParse(usuarioIdClaim, out var usuarioId))
        {
            Console.WriteLine("ERROR: NameIdentifier no existe o no es int");
            return;
        }

        if (!int.TryParse(sitioIdClaim, out var sitioId))
        {
            Console.WriteLine("ERROR: sitioId no existe o no es int");
            return;
        }

        var participacion = await _context.Participaciones
            .Include(p => p.PencaInstancia)
            .FirstOrDefaultAsync(p =>
                p.Id == participacionId &&
                p.UsuarioSitioId == usuarioId &&
                p.SitioId == sitioId);

        if (participacion == null)
        {
            Console.WriteLine($"ERROR: No existe participacion. participacionId={participacionId}, usuarioId={usuarioId}, sitioId={sitioId}");
            return;
        }

        var pencaId = participacion.PencaInstancia.PencaId;

        Context.Items["pencaId"] = pencaId;

        var sala = NombreSala(sitioId, pencaId);
        await Groups.AddToGroupAsync(Context.ConnectionId, sala);

        Console.WriteLine($"OK: usuario unido a sala {sala}");
    }
    catch (Exception ex)
    {
        Console.WriteLine("ERROR en UnirseASala:");
        Console.WriteLine(ex.ToString());
        throw;
    }
}

    private static string NombreSala(int sitioId, int pencaId) => $"chat-{sitioId}-{pencaId}";

    public async Task HistorialMensajes()
    {
        var pencaId = (int)Context.Items["pencaId"]!;

        var mensajes = await _context.MensajesChat
            .Include(m => m.Participacion).ThenInclude(p => p.UsuarioSitio)
            .Include(m => m.Participacion).ThenInclude(m => m.PencaInstancia)
            .Where(m => m.Participacion.SitioId == SitioId &&
                        m.Participacion.PencaInstancia.PencaId == pencaId)
            .OrderByDescending(m => m.FechaEnvio)
            .Take(10)
            .Select(mensajeChat => new
            {
                mensajeChat.Id,
                mensajeChat.Contenido,
                mensajeChat.FechaEnvio,
                UsuarioId = mensajeChat.Participacion.UsuarioSitio.Id,
                mensajeChat.Participacion.UsuarioSitio.Nombre
            }).ToListAsync();

        var historialFinal = mensajes.OrderBy(m => m.FechaEnvio).ToList();
        await Clients.Caller.SendAsync("HistorialMensajes", historialFinal);
    }

    public async Task EnviarMensaje(string contenido)
{
    if (!Context.Items.TryGetValue("pencaId", out var pencaObj) || pencaObj is not int pencaId)
    {
        Console.WriteLine("❌ No hay pencaId en Context.Items, el usuario no está unido a ninguna sala.");
        return;
    }

    var sala = NombreSala(SitioId, pencaId);

    var participacion = await _context.Participaciones
        .Include(pa => pa.PencaInstancia).ThenInclude(pi => pi.Penca)
        .Where(pa => pa.SitioId == SitioId)
        .Where(pa => pa.PencaInstancia.Penca.Id == pencaId)
        .Where(pa => pa.UsuarioSitioId == UsuarioId)
        .FirstOrDefaultAsync();

    if (participacion == null)
    {
        Console.WriteLine($"❌ Participación no encontrada para usuario {UsuarioId} en penca {pencaId}");
        return;
    }

    var mensaje = new MensajeChat
    {
        Contenido = contenido,
        FechaEnvio = DateTime.UtcNow,
        ParticipacionId = participacion.Id,
        SitioId = SitioId,
        Participacion = participacion
    };

    _context.Add(mensaje);
    await _context.SaveChangesAsync();

    await Clients.Group(sala).SendAsync("RecibirMensaje", new
    {
        UsuarioId,
        Nombre = NombreUsuario,
        Contenido = contenido,
        FechaEnvio = DateTime.UtcNow
    });
}

}