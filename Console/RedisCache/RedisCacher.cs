using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisCache
{
    public class RedisCacher
    {
        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            string cacheConnection = ConfigurationManager.AppSettings["CacheConnection"] ?? "";
            return ConnectionMultiplexer.Connect(cacheConnection);
        });

        public static void RemoveCacheByPrefix(string prefix)
        {
            // get the target server
            var server = Connection.GetServer(ConfigurationManager.AppSettings["CacheConnection"] ?? "");
            var db = Connection.GetDatabase();
            // show all keys in database 0 that include "foo" in their name
            foreach (var key in server.Keys(pattern: prefix))
            {
                db.KeyDelete(key);
            }
        }

        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }

        public static long StringIncrement(string key, long value)
        {
            var db = Connection.GetDatabase();
            return db.StringIncrement(key, value);
        }

        public static bool Set(string key, RedisValue value)
        {
            var db = Connection.GetDatabase();
            return db.StringSet(key, value);
        }

        public static bool KeyExists(string key)
        {
            var db = Connection.GetDatabase();
            return db.KeyExists(key);
        }

        public static RedisValue Get(string key)
        {
            var db = Connection.GetDatabase();
            var redisObject = db.StringGet(key);
            if (redisObject.HasValue)
            {
                return redisObject;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Set string value with an timeout in seconds
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="timeoutInSecond"></param>
        /// <returns></returns>
        public static bool SetStringWithTimeoutSeconds(string key, string val, int timeoutInSecond)
        {
            var db = Connection.GetDatabase();
            return db.StringSet(key, val, new TimeSpan(0, 0, timeoutInSecond));
        }

        public static string GetStringValue(string key)
        {
            var db = Connection.GetDatabase();
            var redisObject = db.StringGet(key);
            if (redisObject.HasValue)
            {
                return redisObject;
            }

            return "";
        }

        public static bool KeyDelete(string key)
        {
            var db = Connection.GetDatabase();
            return db.KeyDelete(key);
        }

        public bool Delete(string key)
        {
            var db = Connection.GetDatabase();
            return db.KeyDelete(key);
        }

        public static bool Set(string key, RedisValue value, int minutes)
        {
            var db = Connection.GetDatabase();
            return db.StringSet(key, value, new TimeSpan(0, minutes, 0));
        }

        /// <summary>
        /// Set value with TTL counted in seconds
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static bool SetWithTTLSeconds(string key, RedisValue value, int seconds)
        {
            var db = Connection.GetDatabase();
            return db.StringSet(key, value, new TimeSpan(0, 0, seconds));
        }

        public static bool SetWithTTLMinutes(string key, RedisValue value, int minutes)
        {
            var db = Connection.GetDatabase();
            return db.StringSet(key, value, new TimeSpan(0, minutes, 0));
        }

        public static bool SetObject<T>(string key, T obj) where T : class
        {
            try
            {
                var db = Connection.GetDatabase();
                return db.StringSet(key, JsonConvert.SerializeObject(obj));
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool SetObject<T>(string key, T obj, int minutes) where T : class
        {
            try
            {
                var db = Connection.GetDatabase();
                return db.StringSet(key, JsonConvert.SerializeObject(obj), new TimeSpan(0, minutes, 0));
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool SetObject<T>(string key, T obj, int minutes, int second) where T : class
        {
            try
            {
                var db = Connection.GetDatabase();
                return db.StringSet(key, JsonConvert.SerializeObject(obj), new TimeSpan(0, minutes, second));
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static T GetObject<T>(string key) where T : class
        {
            try
            {
                var db = Connection.GetDatabase();
                var redisObject = db.StringGet(key);
                if (redisObject.HasValue)
                {
                    return JsonConvert.DeserializeObject<T>(redisObject);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static HashEntry[] HashGetAll(string key)
        {
            var db = Connection.GetDatabase();
            var redisObject = db.HashGetAll(key);
            return redisObject;
        }

        public static void HashSet(string key, HashEntry[] value)
        {
            var db = Connection.GetDatabase();
            db.HashSet(key, value);
        }

        public static long HashInCrement(RedisKey hashKey, RedisValue key, long value)
        {
            var db = Connection.GetDatabase();
            return db.HashIncrement(hashKey, key, value);
        }

        public static long HashDecrement(RedisKey hashKey, RedisValue key, long value)
        {
            var db = Connection.GetDatabase();
            return db.HashDecrement(hashKey, key, value);
        }

        public static long HashCount(string key)
        {
            var db = Connection.GetDatabase();
            return db.HashLength(key);
        }

        public static RedisValue HashGet(string hashKey, RedisValue key)
        {
            var db = Connection.GetDatabase();
            if (db.HashExists(hashKey, key))
            {
                return db.HashGet(hashKey, key);
            }
            return 0;
        }

        public static RedisValue[] HashKeys(string hashKey)
        {
            var db = Connection.GetDatabase();
            return db.HashKeys(hashKey);
        }

        public static long LoginInCrement(string key)
        {
            var db = Connection.GetDatabase();
            var redisObject = db.StringGet(key);
            if (redisObject.HasValue)
            {
                return db.StringIncrement(key, 1);
            }
            else
            {
                db.StringSet(key, 1, new TimeSpan(0, 60, 0));
                return 1;
            }
        }

        public static int CountLogin(string key)
        {
            var db = Connection.GetDatabase();
            var redisObject = db.StringGet(key);
            if (redisObject.HasValue)
            {
                return (int)redisObject;
            }
            else
            {
                return 0;
            }
        }
    }
}
