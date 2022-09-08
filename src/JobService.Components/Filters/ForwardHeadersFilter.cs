using System.Threading.Tasks;
using MassTransit;

namespace JobService.Components.Filters
{
    public class ForwardHeadersFilter<T> :
        IFilter<SendContext<T>>,
        IFilter<PublishContext<T>>
        where T : class
    {
        private readonly ScopedObject _scopedObject;

        public ForwardHeadersFilter(ScopedObject scopedObject)
        {
            _scopedObject = scopedObject;
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("forwardHeadersFilter");
        }

        public Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
        {
            SetHeaders(context);
            return next.Send(context);
        }

        public Task Send(SendContext<T> context, IPipe<SendContext<T>> next)
        {
            SetHeaders(context);
            return next.Send(context);
        }

        private void SetHeaders(SendContext context)
        {
            context.Headers.Set(Constants.MyValueKey, _scopedObject.Value);
        }
    }
}