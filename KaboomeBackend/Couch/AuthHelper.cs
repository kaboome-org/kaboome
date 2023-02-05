namespace KaboomeBackend.Couch
{
    using System.Net;

    public static class AuthHelper
    {
        public static async Task<string?> GetAndValidateUsername(HttpRequest req, Uri couchDbUri)
        {
            var couchDbAuth = req.Cookies["AuthSession"];
            if (couchDbAuth != null)
            {
                var cookieContainer = new CookieContainer();
                using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
                using (var client = new HttpClient(handler) { BaseAddress = couchDbUri })
                {
                    cookieContainer.Add(couchDbUri, new Cookie("AuthSession", couchDbAuth));
                    var couchResponse = await client.GetAsync("/_session");
                    couchResponse.EnsureSuccessStatusCode();
                    var result = await couchResponse.Content.ReadFromJsonAsync<CouchSessionResponse>();
                    if (result != null && result.Ok && result?.UserCtx?.Name != null)
                    {
                        return result.UserCtx.Name;
                    }
                }
            }
            return null;
        }
    }
}
