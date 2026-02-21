using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CompuGear.Services
{
    /// <summary>
    /// PayMongo Payment Service - Handles creating checkout sessions and verifying payments
    /// Uses PayMongo API v1: https://developers.paymongo.com/reference
    /// </summary>
    public interface IPayMongoService
    {
        /// <summary>
        /// Creates a PayMongo Checkout Session and returns the checkout URL
        /// </summary>
        Task<PayMongoCheckoutResult> CreateCheckoutSessionAsync(PayMongoCheckoutRequest request);

        /// <summary>
        /// Retrieves a checkout session to verify payment status
        /// </summary>
        Task<PayMongoSessionStatus> GetCheckoutSessionAsync(string checkoutSessionId);
    }

    public class PayMongoService : IPayMongoService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;
        private readonly ILogger<PayMongoService> _logger;
        private const string BaseUrl = "https://api.paymongo.com/v1";

        public PayMongoService(HttpClient httpClient, IConfiguration configuration, ILogger<PayMongoService> logger)
        {
            _httpClient = httpClient;
            _secretKey = configuration["PayMongo:SecretKey"] ?? throw new InvalidOperationException("PayMongo:SecretKey is not configured");
            _logger = logger;

            // Set up Basic Auth: base64(sk_test_xxx:)
            var authBytes = Encoding.ASCII.GetBytes($"{_secretKey}:");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<PayMongoCheckoutResult> CreateCheckoutSessionAsync(PayMongoCheckoutRequest request)
        {
            try
            {
                // PayMongo expects amounts in centavos (smallest currency unit)
                var amountInCentavos = (int)(request.Amount * 100);

                var payload = new
                {
                    data = new
                    {
                        attributes = new
                        {
                            send_email_receipt = true,
                            show_description = true,
                            show_line_items = true,
                            description = request.Description,
                            line_items = new[]
                            {
                                new
                                {
                                    currency = "PHP",
                                    amount = amountInCentavos,
                                    name = request.PlanName + " Plan - CompuGear ERP",
                                    description = request.Description,
                                    quantity = 1
                                }
                            },
                            payment_method_types = new[] { "gcash", "grab_pay", "paymaya", "card" },
                            success_url = request.SuccessUrl,
                            cancel_url = request.CancelUrl,
                            metadata = new Dictionary<string, string>
                            {
                                { "plan_name", request.PlanName },
                                { "billing_cycle", request.BillingCycle },
                                { "registration_id", request.RegistrationId }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                _logger.LogInformation("Creating PayMongo checkout session for plan: {Plan}, amount: {Amount} PHP", request.PlanName, request.Amount);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BaseUrl}/checkout_sessions", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayMongo API error: {StatusCode} - {Body}", response.StatusCode, responseBody);
                    return new PayMongoCheckoutResult
                    {
                        Success = false,
                        ErrorMessage = $"Payment gateway error: {response.StatusCode}"
                    };
                }

                using var doc = JsonDocument.Parse(responseBody);
                var data = doc.RootElement.GetProperty("data");
                var checkoutSessionId = data.GetProperty("id").GetString()!;
                var checkoutUrl = data.GetProperty("attributes").GetProperty("checkout_url").GetString()!;

                _logger.LogInformation("PayMongo checkout session created: {SessionId}", checkoutSessionId);

                return new PayMongoCheckoutResult
                {
                    Success = true,
                    CheckoutSessionId = checkoutSessionId,
                    CheckoutUrl = checkoutUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create PayMongo checkout session");
                return new PayMongoCheckoutResult
                {
                    Success = false,
                    ErrorMessage = "Failed to initialize payment. Please try again."
                };
            }
        }

        public async Task<PayMongoSessionStatus> GetCheckoutSessionAsync(string checkoutSessionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/checkout_sessions/{checkoutSessionId}");
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PayMongo GET session error: {StatusCode} - {Body}", response.StatusCode, responseBody);
                    return new PayMongoSessionStatus { Success = false, Status = "error" };
                }

                using var doc = JsonDocument.Parse(responseBody);
                var attributes = doc.RootElement.GetProperty("data").GetProperty("attributes");

                var status = "active";
                // Check payment_intent status if available
                if (attributes.TryGetProperty("payment_intent", out var paymentIntent) &&
                    paymentIntent.ValueKind != JsonValueKind.Null)
                {
                    var piAttributes = paymentIntent.GetProperty("attributes");
                    status = piAttributes.GetProperty("status").GetString() ?? "unknown";
                }

                // Check payments array for completed payments
                var payments = attributes.GetProperty("payments");
                var isPaid = false;
                string? paymentId = null;

                if (payments.GetArrayLength() > 0)
                {
                    var firstPayment = payments[0];
                    var paymentAttributes = firstPayment.GetProperty("attributes");
                    var paymentStatus = paymentAttributes.GetProperty("status").GetString();
                    isPaid = paymentStatus == "paid";
                    paymentId = firstPayment.GetProperty("id").GetString();

                    if (isPaid) status = "succeeded";
                }

                // Extract metadata
                var metadata = new Dictionary<string, string>();
                if (attributes.TryGetProperty("metadata", out var meta) && meta.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in meta.EnumerateObject())
                    {
                        metadata[prop.Name] = prop.Value.GetString() ?? "";
                    }
                }

                _logger.LogInformation("PayMongo session {SessionId} status: {Status}, paid: {IsPaid}",
                    checkoutSessionId, status, isPaid);

                return new PayMongoSessionStatus
                {
                    Success = true,
                    Status = status,
                    IsPaid = isPaid,
                    PaymentId = paymentId,
                    Metadata = metadata
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve PayMongo checkout session: {SessionId}", checkoutSessionId);
                return new PayMongoSessionStatus { Success = false, Status = "error" };
            }
        }
    }

    // ===== REQUEST / RESPONSE MODELS =====

    public class PayMongoCheckoutRequest
    {
        public string PlanName { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = "Monthly";
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        /// <summary>
        /// Unique ID to link this checkout to the pending registration stored in session
        /// </summary>
        public string RegistrationId { get; set; } = string.Empty;
    }

    public class PayMongoCheckoutResult
    {
        public bool Success { get; set; }
        public string? CheckoutSessionId { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PayMongoSessionStatus
    {
        public bool Success { get; set; }
        public string Status { get; set; } = "unknown";
        public bool IsPaid { get; set; }
        public string? PaymentId { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
