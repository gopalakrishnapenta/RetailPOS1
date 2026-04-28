using Microsoft.AspNetCore.Mvc;
using System.Text;
using Newtonsoft.Json;

namespace AIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;

        public ChatController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return Ok(new { response = "AI Assistant is currently offline (API Key missing). Please add GEMINI_API_KEY to your .env file." });
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";

            // Fetch some context from other services (Simplified for now)
            string storeContext = "";
            try {
                // Pass the authorization token to the internal service calls
                var authHeader = Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader)) {
                    client.DefaultRequestHeaders.Add("Authorization", authHeader);
                }

                var catResponse = await client.GetAsync("http://localhost:5000/gateway/catalog/categories");
                if (catResponse.IsSuccessStatusCode) {
                    var cats = JsonConvert.DeserializeObject<List<CategoryInfo>>(await catResponse.Content.ReadAsStringAsync());
                    if (cats != null && cats.Any()) {
                        storeContext += "CATEGORIES: " + string.Join(", ", cats.Select(c => c.Name)) + ". ";
                    }
                }
                
                var prodResponse = await client.GetAsync("http://localhost:5000/gateway/catalog/products");
                if (prodResponse.IsSuccessStatusCode) {
                    var prods = JsonConvert.DeserializeObject<List<ProductInfo>>(await prodResponse.Content.ReadAsStringAsync());
                    if (prods != null && prods.Any()) {
                        var outOfStock = prods.Where(p => p.StockQuantity <= 0).Select(p => p.Name).ToList();
                        var lowStock = prods.Where(p => p.StockQuantity > 0 && p.StockQuantity <= p.ReorderLevel).Select(p => p.Name).ToList();

                        if (outOfStock.Any()) storeContext += "CRITICAL: OUT OF STOCK: " + string.Join(", ", outOfStock) + ". ";
                        if (lowStock.Any()) storeContext += "LOW STOCK ALERT: " + string.Join(", ", lowStock) + ". ";

                        storeContext += "### LIVE INVENTORY TABLE (Sorted by Price DESC):\n" +
                                        "| Product Name | Price (INR) | Stock | Min Stock | Status |\n" +
                                        "|--------------|-------------|-------|-----------|--------|\n";
                        
                        foreach(var p in prods.OrderByDescending(p => p.SellingPrice).Take(25)) {
                            string status = p.StockQuantity <= 0 ? "OUT OF STOCK" : (p.StockQuantity <= p.ReorderLevel ? "LOW STOCK" : "OK");
                            storeContext += $"| {p.Name} | {p.SellingPrice} | {p.StockQuantity} | {p.ReorderLevel} | {status} |\n";
                        }
                    }
                }
            } catch { /* Fail gracefully if other services are down */ }

            var systemPrompt = "You are the RetailPOS AI Store Assistant. Be helpful, professional and concise.\n\n" +
                               "CONTEXT DATA:\n" + storeContext + 
                               "\n\nINSTRUCTIONS:\n" +
                               "1. ALWAYS check the table above before answering price or stock questions.\n" +
                               "2. To find the 'highest rate', look at the first few items in the table (it is sorted by price).\n" +
                               "3. To find 'out of stock', look for the 'OUT OF STOCK' status in the table.\n" +
                               "4. Answer in a friendly, helpful way.";

            var geminiRequest = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = new[]
                {
                    new { 
                        role = "user",
                        parts = new[] { new { text = request.Message } } 
                    }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(geminiRequest), Encoding.UTF8, "application/json");
            
            try 
            {
                // Create a fresh client for the Gemini call to avoid sending internal Auth headers to Google
                var geminiClient = _httpClientFactory.CreateClient();
                var response = await geminiClient.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // Return a graceful message to the user instead of a 400 error
                    return Ok(new { response = "I'm sorry, I'm having trouble thinking right now. (Technical details: " + response.StatusCode + ")" });
                }

                dynamic? geminiResponse = JsonConvert.DeserializeObject(responseString);
                
                if (geminiResponse?.candidates == null || geminiResponse.candidates.Count == 0)
                {
                    return Ok(new { response = "I couldn't generate a response. Please try again." });
                }

                string aiText = geminiResponse.candidates[0].content.parts[0].text;
                return Ok(new { response = aiText });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class CategoryInfo {
        public string Name { get; set; } = "";
    }

    public class ProductInfo {
        public string Name { get; set; } = "";
        public decimal SellingPrice { get; set; }
        public int StockQuantity { get; set; }
        public int ReorderLevel { get; set; }
    }
}
