namespace KaboomeBackend.Couch
{
    using KaboomeBackend.Options;
    using MyCouch;
    using MyCouch.Requests;
    using Newtonsoft.Json;

    public class AdminCouchClient
    {
        private readonly Uri connectionUrl;

        public AdminCouchClient(KaboomeOptions kaboomeOptions)
        {
            var builder = new UriBuilder(kaboomeOptions.CouchDbUri)
            {
                UserName = Uri.EscapeDataString(kaboomeOptions.CouchDbAdminUsername),
                Password = Uri.EscapeDataString(kaboomeOptions.CouchDbAdminPassword)
            };
            this.connectionUrl = builder.Uri;
        }

        public async Task CreateUserAndDatabases(string name, string password)
        {
            await this.CreateUser(name, password);
            // Db for events
            await this.CreateDbAndSetPermissions(name, EventDb(name));
            // Db for configs
            await this.CreateDbAndSetPermissions(name, ConfigDb(name));
            // Db for secrets
            (await this.CreateDb(SecretDb(name))).Dispose();
        }

        private static string EventDb(string name) => $"kaboome_{name}";
        private static string ConfigDb(string name) => $"kaboome_{name}_config";
        private static string SecretDb(string name) => $"kaboome_{name}_secret";

        public async Task AddUserSecret(string name, string id, string secretDoc)
        {
            using var secretclient = new MyCouchClient(this.connectionUrl, SecretDb(name));
            await secretclient.Documents.PutAsync(id, secretDoc);
        }
        public async Task<List<string>> GetUserSecrets(string name, string idPrefix = "")
        {
            using var secretclient = new MyCouchClient(this.connectionUrl, SecretDb(name));
            var request = new QueryViewRequest("_all_docs").Configure(q => q.StartKey(idPrefix).EndKey(idPrefix + "~").IncludeDocs(true));
            var result = await secretclient.Views.QueryAsync(request);
            var docs = result.Rows.Select(r => r.IncludedDoc).ToList();
            return docs;
        }
        public async Task WriteUserConfig(string name, string id, string configDoc)
        {
            using var configclient = new MyCouchClient(this.connectionUrl, ConfigDb(name));
            var res = await configclient.Documents.PutAsync(id, configDoc);
        }
        public async Task<string> GetUserConfig(string name, string id)
        {
            using var configclient = new MyCouchClient(this.connectionUrl, ConfigDb(name));
            return (await configclient.Documents.GetAsync(id)).Content;
        }
        public async Task WriteKaboomeEvent(string name, string id, string kaboomeEvent)
        {
            using var client = new MyCouchClient(this.connectionUrl, EventDb(name));
            var res = await client.Documents.PutAsync(id, kaboomeEvent);
        }
        public async Task DeleteKaboomeEvent(string name, string id, string rev)
        {
            using var client = new MyCouchClient(this.connectionUrl, EventDb(name));
            var res = await client.Documents.DeleteAsync(id, rev);
        }
        public async Task<string> GetKaboomeEvent(string name, string id)
        {
            using var client = new MyCouchClient(this.connectionUrl, EventDb(name));
            return (await client.Documents.GetAsync(id)).Content;
        }
        public async Task<List<string>> GetEventChanges(string name, string? since = null)
        {
            var client = new MyCouchClient(this.connectionUrl, EventDb(name));
            var getChangesRequest = new GetChangesRequest { Feed = ChangesFeed.Normal, IncludeDocs = true, Since = since };
            var changes = await client.Changes.GetAsync(getChangesRequest);
            return changes.Results.Select(c => c.IncludedDoc).ToList();
        }
        public async Task<string> GetLatestSeqNumberOfEventDb(string name) {
            using var client = new MyCouchClient(this.connectionUrl, EventDb(name));
            var info = await client.Database.GetAsync();
            return info.UpdateSeq;
        }
        private async Task CreateDbAndSetPermissions(string name, string databaseName)
        {
            using var client = await this.CreateDb(databaseName);
            // give permission to db to user
            await client.Documents.PutAsync("_security",
                "{\"members\":{\"roles\":[\"_admin\"],\"names\":[\"" + name + "\"]},\"admins\":{\"roles\":[\"_admin\"],\"names\":[\"" + name + "\"]}}");
        }

        private async Task CreateUser(string name, string password)
        {
            using var userclient = new MyCouchClient(this.connectionUrl, $"_users");
            var user = new { type = "user", name, roles = new[] { "kaboome" }, password };
            var res = await userclient.Documents.PutAsync($"org.couchdb.user:{name}", JsonConvert.SerializeObject(user));
            if (!res.IsSuccess)
            {
                throw new Exception("User creation failed. Try a different username.");
            }
        }

        private async Task<MyCouchClient> CreateDb(string name)
        {
            var client = new MyCouchClient(this.connectionUrl, name);
            await client.Database.PutAsync();
            return client;
        }
    }
}
