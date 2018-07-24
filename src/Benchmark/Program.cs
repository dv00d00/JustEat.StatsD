using BenchmarkDotNet.Running;

namespace Benchmark
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            //BenchmarkRunner.Run<StatSendingBenchmark>();
            BenchmarkRunner.Run<UdpTransportBenchmark>();
            //BenchmarkRunner.Run<UdpTransportBenchmark>();
            //BenchmarkRunner.Run<UdpStatSendingBenchmark>();
        }
    }
}
