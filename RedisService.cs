using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RedisConsoleApp
{
    public class RedisService
    {

        private readonly IDistributedCache RedisMan;
        private string RedisKey = "Logging";

        public RedisService(IDistributedCache distributedCache)
        {
            this.RedisMan = distributedCache;
        }

        public async Task<string> Run()
        {
            try
            {
                while (true)
                {
                    var delayTask = Task.Delay(10000);
                    var isCurrentlyRunning = await RedisMan.GetStringAsync(RedisKey);
                    if (!string.IsNullOrEmpty(isCurrentlyRunning))
                    {
                        Console.WriteLine("Function is currently running");
                        return $"Function is currently running";
                    }

                    await RedisMan.SetStringAsync(RedisKey, DateTime.Now.ToString("yyyyMMdd HHmmss"), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = new TimeSpan(48, 0, 0)
                    });

                    Console.WriteLine("Setted");

                    await RedisMan.SetStringAsync(RedisKey, string.Empty);

                    Console.WriteLine("Removed");
                    await delayTask;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Inner Exception : " + e.InnerException);
                Console.WriteLine("Inner Exception Message : " + e.InnerException.Message);
                Console.WriteLine("Message : " + e.Message);
            }
            return string.Empty;
        }
    }
}
