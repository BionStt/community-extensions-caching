﻿using System;
using Community.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Community.Extensions.Caching.Redis
{
    public static class BuilderExtensions
    {
        public static IServiceCollection AddRedisDistributedCache<TCacheInstance>(this IServiceCollection services, Action<RedisCacheOptions> setupInner = null,Action<DistributedCacheOptions<TCacheInstance>> setup = null)
        {
            RedisCacheOptions innerOptions = new RedisCacheOptions();
            setupInner?.Invoke(innerOptions);

            DistributedCacheOptions<TCacheInstance> options = new DistributedCacheOptions<TCacheInstance>(new RedisCache(Options.Create(innerOptions)));
            setup?.Invoke(options);

            services.TryAddSingleton(options);
            services.TryAddSingleton<IDistributedCache<TCacheInstance>, DistributedCache<TCacheInstance>>();

            return services;
        }
    }
}
