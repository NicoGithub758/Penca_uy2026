using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Penca_uy2026.Hubs
{
    /// <summary>
    /// Hub centralizado para manejar las actualizaciones en tiempo real de las pencas.
    /// Permite a los clientes suscribirse a salas (grupos) específicas para recibir
    /// actualizaciones de la tabla de posiciones (Leaderboard) sin afectar a otras pencas.
    /// </summary>
    public class PencaHub : Hub
    {
        /// <summary>
        /// Suscribe la conexión actual al grupo de una instancia de penca específica.
        /// El cliente de React invocará esto al entrar a la vista de la penca.
        /// </summary>
        public async Task JoinPencaGroup(string pencaInstanciaId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"penca-{pencaInstanciaId}");
        }

        /// <summary>
        /// Desuscribe la conexión actual del grupo.
        /// El cliente de React invocará esto al salir de la vista de la penca.
        /// </summary>
        public async Task LeavePencaGroup(string pencaInstanciaId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"penca-{pencaInstanciaId}");
        }
    }
}
