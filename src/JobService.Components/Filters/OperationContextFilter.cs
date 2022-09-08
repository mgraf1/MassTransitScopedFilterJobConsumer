using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit;

namespace JobService.Components.Filters
{
    public class OperationContextFilter<T> :
        IFilter<ConsumeContext<T>>,
        IFilter<ExecuteContext<T>>
        where T : class
    {
        private readonly ScopedObject _scopedObject;

        public OperationContextFilter(ScopedObject scopedObject)
        {
            _scopedObject = scopedObject;
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("operationContextFilter");
        }

        public Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
        {
            this.InitializeOperationContext(context);
            return next.Send(context);
        }

        public Task Send(ExecuteContext<T> context, IPipe<ExecuteContext<T>> next)
        {
            this.InitializeOperationContext(context);
            return next.Send(context);
        }

        private void InitializeOperationContext(ConsumeContext context)
        {
            var myValue = context.Headers.Get<string>(Constants.MyValueKey);
            _scopedObject.Value = myValue;
        }
    }
}