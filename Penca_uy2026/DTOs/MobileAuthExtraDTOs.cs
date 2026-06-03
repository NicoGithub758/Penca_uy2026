namespace Penca_uy2026.DTOs
{
    /// <summary>
    /// Request para login interno desde mobile (email + password).
    /// El SitioId se manda explícitamente porque mobile no tiene slug en URL.
    /// </summary>
    public class MobileLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int SitioId { get; set; }
    }

    /// <summary>
    /// Request para registro desde mobile.
    /// TokenInvitacion es opcional, solo para sitios con TipoRegistro = SoloConInvitacion.
    /// </summary>
    public class MobileRegisterRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int SitioId { get; set; }
        public string? TokenInvitacion { get; set; }
    }

    /// <summary>
    /// Respuesta unificada para login interno, registro abierto y login social desde mobile.
    /// Si el registro queda pendiente de aprobacion, Jwt viene vacio y EstadoSolicitud = "Pendiente".
    /// </summary>
    public class MobileAuthResponse
    {
        public string Jwt { get; set; } = string.Empty;
        public int UsuarioSitioId { get; set; }
        public int SitioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Indica el estado del flujo:
        /// - "Activo": login exitoso, JWT valido
        /// - "Pendiente": solicitud enviada, esperando aprobacion de admin
        /// </summary>
        public string EstadoSolicitud { get; set; } = "Activo";

        /// <summary>
        /// Mensaje para mostrar al usuario (ej: "Tu solicitud ha sido enviada").
        /// </summary>
        public string? Mensaje { get; set; }
    }
}
