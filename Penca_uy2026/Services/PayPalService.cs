using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using System.Collections.Generic;
using System.Linq;

namespace Penca_uy2026.Services
{
    public class PayPalService
    {
        private readonly IConfiguration _config;
        private readonly PayPalHttpClient _client;

        public PayPalService(IConfiguration config)
        {
            _config = config;
            _client = CreateClient();
        }

        /// <summary>
        /// Crea el cliente de PayPal con las credenciales del sandbox o producción.
        /// </summary>
        private PayPalHttpClient CreateClient()
        {
            var clientId = _config["PayPal:ClientId"];
            var secret = _config["PayPal:Secret"];
            var mode = _config["PayPal:Mode"];

            Console.WriteLine($"[PAYPAL CONFIG] ClientId: {clientId?.Substring(0, 10)}... Secret: {secret?.Substring(0, 5)}... Mode: {mode}");

            PayPalEnvironment environment = mode == "live"
                ? new LiveEnvironment(clientId, secret)
                : new SandboxEnvironment(clientId, secret);

            return new PayPalHttpClient(environment);
        }

        /// <summary>
        /// Crea una orden de pago en PayPal.
        /// Devuelve el OrderId y la URL de aprobación que el usuario debe visitar.
        /// </summary>
        public async Task<(string OrderId, string ApprovalUrl)> CrearOrdenAsync(decimal monto, string currency = "USD")
        {
            var order = new OrderRequest
            {
                CheckoutPaymentIntent = "CAPTURE",
                PurchaseUnits = new List<PurchaseUnitRequest>
                {
                    new PurchaseUnitRequest
                    {
                        AmountWithBreakdown = new AmountWithBreakdown
                        {
                            CurrencyCode = currency,
                            Value = monto.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                        },
                        Description = "Pago de participación en penca"
                    }
                },
                ApplicationContext = new ApplicationContext
                {
                    // Estas URLs son las que PayPal usa para redirigir despues del pago.
                    // El mobile detecta estas URLs en el WebView y maneja el resultado.
                    ReturnUrl = "https://pencauy.com/pago-exitoso",
                    CancelUrl = "https://pencauy.com/pago-cancelado"
                }
            };

            var request = new OrdersCreateRequest();
            request.Prefer("return=representation");
            request.RequestBody(order);

            var response = await _client.Execute(request);
            var result = response.Result<Order>();

            // Buscar la URL de aprobacion en los links que devuelve PayPal
            var approvalLink = result.Links?.FirstOrDefault(l => l.Rel == "approve");
            var approvalUrl = approvalLink?.Href ?? "";

            return (result.Id, approvalUrl);
        }

        /// <summary>
        /// Captura (confirma) una orden de pago en PayPal.
        /// </summary>
        public async Task<(bool Success, string Status)> CapturarOrdenAsync(string orderId)
        {
            var request = new OrdersCaptureRequest(orderId);
            request.RequestBody(new OrderActionRequest());

            try
            {
                var response = await _client.Execute(request);
                var result = response.Result<Order>();

                return (result.Status == "COMPLETED", result.Status);
            }
            catch
            {
                return (false, "FAILED");
            }
        }
    }
}