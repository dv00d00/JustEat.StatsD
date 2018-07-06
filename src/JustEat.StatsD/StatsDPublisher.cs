using System;
using JustEat.StatsD.EndpointLookups;

namespace JustEat.StatsD
{
    /// <summary>
    ///     Will synchronously publish stats at statsd as you make calls; will not batch sends.
    /// </summary>
    public class StatsDPublisher : IStatsDPublisher
    {
        private readonly StatsDMessageFormatter _formatter;
        private readonly IStatsDTransport _transport;
        private readonly Func<Exception, bool> _onError;
        private SpanStatsDMessageFormatter _spanFormatter;

        public StatsDPublisher(StatsDConfiguration configuration, IStatsDTransport transport)
        {
            if (configuration == null)
            {
               throw new ArgumentNullException(nameof(configuration));
            }

            _transport = transport ?? throw new ArgumentNullException(nameof(transport));

            _formatter = new StatsDMessageFormatter(configuration.Prefix);
            _spanFormatter = new SpanStatsDMessageFormatter(configuration.Prefix);
            _onError = configuration.OnError;
        }

        public StatsDPublisher(StatsDConfiguration configuration)
        {
            if (configuration == null)
            {
               throw new ArgumentNullException(nameof(configuration));
            }

            if (string.IsNullOrWhiteSpace(configuration.Host))
            {
                throw new ArgumentNullException(nameof(configuration.Host));
            }

            _formatter = new StatsDMessageFormatter(configuration.Prefix);

            var endpointSource = EndpointParser.MakeEndPointSource(
                configuration.Host, configuration.Port, configuration.DnsLookupInterval);
            _transport = new UdpTransport(endpointSource);
            _onError = configuration.OnError;
        }

        public void Increment(string bucket)
        {
            Span<byte> buffer = stackalloc byte[512];
            var writer = new Writer(buffer);
            _spanFormatter.Increment(bucket, ref writer);
            Send(writer.Get());
        }

        public void Increment(long value, string bucket)
        {
            Span<byte> buffer = stackalloc byte[512];
            var writer = new Writer(buffer);
            _spanFormatter.Increment(value, bucket, ref writer);
            Send(writer.Get());
        }

        public void Increment(long value, double sampleRate, string bucket)
        {
            Span<byte> buffer = stackalloc byte[512];
            var writer = new Writer(buffer);
            _spanFormatter.Increment(value, sampleRate, bucket, ref writer);
            Send(writer.Get());
        }

        public void Increment(long value, double sampleRate, params string[] buckets)
        {
            Send(_formatter.Increment(value, sampleRate, buckets));
        }

        public void Decrement(string bucket)
        {
            Send(_formatter.Decrement(bucket));
        }

        public void Decrement(long value, string bucket)
        {
            Send(_formatter.Decrement(value, bucket));
        }

        public void Decrement(long value, double sampleRate, string bucket)
        {
            Send(_formatter.Decrement(value, sampleRate, bucket));
        }

        public void Decrement(long value, double sampleRate, params string[] buckets)
        {
            Send(_formatter.Decrement(value, sampleRate, buckets));
        }
        public void Gauge(double  value, string bucket)
        {
            Send(_formatter.Gauge(value, bucket));
        }
        public void Gauge(double value, string bucket, DateTime timestamp)
        {
            Send(_formatter.Gauge(value, bucket, timestamp));
        }
        public void Gauge(long value, string bucket)
        {
            Send(_formatter.Gauge(value, bucket));
        }

        public void Gauge(long value, string bucket, DateTime timestamp)
        {
            Send(_formatter.Gauge(value, bucket, timestamp));
        }

        public void Timing(TimeSpan duration, string bucket)
        {
            Send(_formatter.Timing(Convert.ToInt64(duration.TotalMilliseconds), bucket));
        }

        public void Timing(TimeSpan duration, double sampleRate, string bucket)
        {
            Send(_formatter.Timing(Convert.ToInt64(duration.TotalMilliseconds), sampleRate, bucket));
        }

        public void Timing(long duration, string bucket)
        {
            Send(_formatter.Timing(duration, bucket));
        }

        public void Timing(long duration, double sampleRate, string bucket)
        {
            Send(_formatter.Timing(duration, sampleRate, bucket));
        }

        public void MarkEvent(string name)
        {
            Send(_formatter.Event(name));
        }

        private void Send(ReadOnlySpan<byte> metric)
        {
            try
            {
                //_transport.Send(metric);
            }
            catch (Exception ex)
            {
                var handled = _onError?.Invoke(ex) ?? true;
                if (!handled)
                {
                    throw;
                }
            }
        }

        private void Send(string metric)
        {
            try
            {
                _transport.Send(metric);
            }
            catch (Exception ex)
            {
                var handled = _onError?.Invoke(ex) ?? true;
                if (!handled)
                {
                    throw;
                }
            }
        }
    }
}
