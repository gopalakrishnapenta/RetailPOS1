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
                var catResponse = await client.GetAsync("http://localhost:5000/gateway/catalog/categories");
                if (catResponse.IsSuccessStatusCode) {
                    var cats = JsonConvert.DeserializeObject<List<object>>(await catResponse.Content.ReadAsStringAsync());
                    storeContext += $"We currently have {cats?.Count ?? 0} categories. ";
                }
                
                var prodResponse = await client.GetAsync("http://localhost:5000/gateway/catalog/products");
                if (prodResponse.IsSuccessStatusCode) {
                    var prods = JsonConvert.DeserializeObject<List<object>>(await prodResponse.Content.ReadAsStringAsync());
                    storeContext += $"We have {prods?.Count ?? 0} products in our inventory. ";
                }
            } catch { /* Fail gracefully if other services are down */ }

            var systemPrompt = "You are the RetailPOS AI Store Assistant. Be helpful, professional and concise. " +
                               "Here is some current store data: " + storeContext + 
                               "Use this data to answer questions. If you don't know the answer, just say you don't know.";

            var geminiRequest = new
            {
                contents = new[]
                {
                    new { 
                        role = "user",
                        parts = new[] { new { text = $"{systemPrompt}\n\nUser Question: {request.Message}" } } 
                    }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(geminiRequest), Encoding.UTF8, "application/json");
            
            try 
            {
                var response = await client.PostAsync(url, content);
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
}
