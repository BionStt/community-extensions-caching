﻿using App.Metrics;
using App.Metrics.Gauge;
using App.Metrics.Meter;
using App.Metrics.Timer;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Comminity.Extensions.Caching.AppMetrics
{
    public class DistributedCacheWithMetrics<TCacheInstance> : IDistributedCache<TCacheInstance>
    {
        private static MetricTags MetricsTags { get; } = new MetricTags("inst",
             typeof(TCacheInstance).Name.Split('.').Last());

        public static TimerOptions ReadTimer { get; } =
            new TimerOptions
            {
                Name = "r_time",
                MeasurementUnit = Unit.Requests,
                RateUnit = TimeUnit.Milliseconds,
                Context = "Cache.Distributed"
            };

        public static TimerOptions WriteTimer { get; } =
            new TimerOptions
            {
                Name = "w_time",
                MeasurementUnit = Unit.Requests,
                RateUnit = TimeUnit.Milliseconds,
                Context = "Cache.Distributed"
            };

        private static MeterOptions Hit = new MeterOptions
        {
            MeasurementUnit = Unit.Calls,
            Context = "Cache.Distributed",
            Name = "h_count"
        };

        private static GaugeOptions HitRatio = new GaugeOptions
        {
            MeasurementUnit = Unit.Calls,
            Context = "Cache.Distributed",
            Name = "h_ratio"
        };

        public static TimerOptions FactoryTimer { get; } =
            new TimerOptions
            {
                Name = "f_time",
                MeasurementUnit = Unit.Requests,
                RateUnit = TimeUnit.Milliseconds,
                Context = "Cache.Distributed"
            };

        public byte[] Get(string key)
        {
            using (var timer = this._metrics.Measure.Timer.Time(ReadTimer, MetricsTags))
            {
                try
                {
                    byte[] result = this._inner.Get(key);

                    if (result != null)
                    {
                        timer.TrackUserValue("hit");
                        this._metrics.Measure.Meter.Mark(Hit, MetricsTags);
                    }
                    else
                    {
                        timer.TrackUserValue("miss");
                    }

                    return result;
                }
                catch
                {
                    timer.TrackUserValue("error");
                    throw;
                }
            }
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = new CancellationToken())
        {
            using (var timer = this._metrics.Measure.Timer.Time(ReadTimer, MetricsTags))
            {
                try
                {
                    byte[] result = await _inner.GetAsync(key, token);

                    if (result != null)
                    {
                        timer.TrackUserValue("hit");
                    }
                    else
                    {
                        timer.TrackUserValue("miss");
                    }

                    return result;
                }
                catch
                {
                    timer.TrackUserValue("error");
                    throw;
                }                
            }
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            using (var timer = this._metrics.Measure.Timer.Time(WriteTimer, MetricsTags))
            {
                try
                {
                    _inner.Set(key, value, options);
                    timer.TrackUserValue("ok");
                }
                catch
                {
                    timer.TrackUserValue("error");
                    throw;
                }
                
            }
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
            CancellationToken token = new CancellationToken())
        {
            using (var timer = this._metrics.Measure.Timer.Time(WriteTimer, MetricsTags))
            {
                try
                {
                    await _inner.SetAsync(key, value, options, token);
                    timer.TrackUserValue("ok");
                }
                catch
                {
                    timer.TrackUserValue("error");
                    throw;
                }

            }
        }

        public void Refresh(string key)
        {
            _inner.Refresh(key);
        }

        public Task RefreshAsync(string key, CancellationToken token = new CancellationToken())
        {
            return _inner.RefreshAsync(key, token);
        }

        public void Remove(string key)
        {
            _inner.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = new CancellationToken())
        {
            return _inner.RemoveAsync(key, token);
        }

        private readonly IDistributedCache<TCacheInstance> _inner;
        private readonly IMetrics _metrics;
        private readonly System.Timers.Timer timer = new System.Timers.Timer() { Interval = 30000 };

        public DistributedCacheWithMetrics(IDistributedCache<TCacheInstance> inner, IMetrics metrics)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            this._metrics = metrics ?? throw new ArgumentNullException(nameof(metrics)); timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _metrics.Measure.Gauge.SetValue(HitRatio, () => new HitRatioGauge(_metrics.Provider.Meter.Instance(Hit), _metrics.Provider.Timer.Instance(ReadTimer), m => m.OneMinuteRate));
        }
    }
}
