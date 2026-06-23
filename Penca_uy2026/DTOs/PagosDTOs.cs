namespace Penca_uy2026.DTOs
{
    /// <summary>
    /// Request para iniciar un pago de participación en una penca.
    /// </summary>
    public class CrearPagoRequest
    {
        public int PencaInstanciaId { get; set; }
    }

    /// <summary>
    /// Response con el ID de la orden de PayPal y la URL de aprobación.
    /// El mobile usa ApprovalUrl para abrir el WebView.
    /// </summary>
    public class CrearPagoResponse
    {
        public string OrderId { get; set; } = string.Empty;
        public int PagoId { get; set; }
        public string ApprovalUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request para confirmar un pago después de que el usuario aprobó en PayPal.
    /// </summary>
    public class ConfirmarPagoRequest
    {
        public int PagoId { get; set; }
        public string OrderId { get; set; } = string.Empty;
    }
}