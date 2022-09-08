namespace JobService.Components
{
    using System;
    using System.Threading.Tasks;
    using MassTransit;
    using Microsoft.Extensions.Logging;


    public class ConvertVideoJobConsumer :
        IJobConsumer<ConvertVideo>
    {
        private readonly ScopedObject _scopedObject;
        readonly ILogger<ConvertVideoJobConsumer> _logger;

        public ConvertVideoJobConsumer(
            ScopedObject scopedObject,
            ILogger<ConvertVideoJobConsumer> logger)
        {
            _scopedObject = scopedObject;
            _logger = logger;
        }

        public async Task Run(JobContext<ConvertVideo> context)
        {
            Console.WriteLine(_scopedObject.Value);

            var rng = new Random();

            var variance = TimeSpan.FromMilliseconds(rng.Next(8399, 28377));

            await Task.Delay(variance);

            await context.Publish<VideoConverted>(context.Job);
        }
    }
}