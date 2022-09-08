using NSwag;
using NSwag.Generation.Processors.Contexts;
using NSwag.Generation.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JobService.Components;

namespace JobService.Service
{
    public class AddRequiredHeaderParameter : IOperationProcessor
    {
        public bool Process(OperationProcessorContext context)
        {
            context.OperationDescription.Operation.Parameters.Add(new OpenApiParameter
            {
                Name = Constants.MyValueKey,
                Kind = OpenApiParameterKind.Header,
                Type = NJsonSchema.JsonObjectType.String,
                IsRequired = false,
            });

            return true;
        }
    }
}
