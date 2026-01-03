using StackExchange.Redis;

namespace Tuna.SuperTemplate.HybridCache.Redis
{
    public class RedisCacheManager : IRedisCacheManager
    {
        private readonly IDatabase _database;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IServer _server;
        public RedisCacheManager(RedisServerSettings redisServerSetting)
        {

            if (redisServerSetting is null) throw new ArgumentNullException(nameof(redisServerSetting));

            var connectionParts = redisServerSetting.ConnectionString?.Split(':');
            if (connectionParts == null || connectionParts.Length != 2)
            {
                throw new ArgumentException("Invalid Redis connection string format. Expected format: host:port", nameof(redisServerSetting.ConnectionString));
            }

            string host = connectionParts[0];
            if (!int.TryParse(connectionParts[1], out int port))
            {
                throw new ArgumentException("Invalid port number in Redis connection string", nameof(redisServerSetting.ConnectionString));
            }
            var config = new ConfigurationOptions
            {
                AbortOnConnectFail = redisServerSetting.AbortOnConnectFail,
                AsyncTimeout = redisServerSetting.AsyncTimeoutMilliSecond,
                ConnectTimeout = redisServerSetting.ConnectTimeoutMilliSecond,
                Password = redisServerSetting.Password,
                AllowAdmin = redisServerSetting.AllowAdmin,
                DefaultDatabase = redisServerSetting.DefaultDatabase,
                EndPoints = { { host, port } }
            };
            _connectionMultiplexer = ConnectionMultiplexer.Connect(config);
            _database = _connectionMultiplexer.GetDatabase();
            _server = _connectionMultiplexer.GetServer(host, port);
        }
        public async Task<bool> BulkSetAsync(IEnumerable<KeyValuePair<RedisKey, RedisValue>> items)
        {
           var pairs =items as KeyValuePair<RedisKey, RedisValue>[] ?? [..items];
           return await _database.StringSetAsync(pairs).ConfigureAwait(false);
        }

        public async Task ClearAsync()
        {
          await _server.FlushDatabaseAsync(-1);
        }

        public Task<List<string>> GetAllKeysAsync()
        {
           var keys = _server.Keys(-1,"*").ToList();
            return Task.FromResult(keys.Select(k => k.ToString()).ToList());
        }

        public Task<T> GetAllKeysByPatternAsync<T>(string pattern)
        {
            var keys = _server.Keys(-1, pattern).ToList();
            var result = keys.Select(k => k.ToString()).ToList();
            return Task.FromResult(System.Text.Json.JsonSerializer.Deserialize<T>(System.Text.Json.JsonSerializer.Serialize(result)));
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var serializedValue = await _database.StringGetAsync(key);
            if (!serializedValue.HasValue)
            {
                return default(T);
            }
            var item = System.Text.Json.JsonSerializer.Deserialize<T>(serializedValue.ToString());
            if (Equals(item, default(T)))
                return default(T);
            return item;
        }

        public async Task<List<T>> GetManyAsync<T>(string key)
        {
            var serializedValue = await _database.StringGetAsync(key);
            if (!serializedValue.HasValue)
            {
                return default(List<T>);
            }
            var item = System.Text.Json.JsonSerializer.Deserialize<List<T>>(serializedValue.ToString());
            if (item == null || item.Count == 0)
                return default(List<T>);
            return item;
        }

        public async Task<bool> IsSetAsync(string key)
        {
           return await _database.KeyExistsAsync(key);
        }

        public async Task RemoveAsync(string key)
        {
           await _database.KeyDeleteAsync(key);
        }

        public async Task SetAsync<T>(string key, object data, TimeSpan? expiry = null)
        {
            if (data is null)
                return;

            var serializedData = System.Text.Json.JsonSerializer.Serialize(data);
            await _database.StringSetAsync(key, serializedData, (Expiration)expiry);
        }

        public async Task SetAsync<T>(string key, object data)
        {
            if (data is null)
                return;

            var serializedData = System.Text.Json.JsonSerializer.Serialize(data);
            await _database.StringSetAsync(key, serializedData);
        }
    }
}
