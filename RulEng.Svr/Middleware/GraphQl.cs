using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Http;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using RulEng.States;

namespace RulEng.Svr.Middleware
{
    public class GraphQl
    {
        private readonly RequestDelegate _next;
        private readonly IRulEngStore _rulEngStore;

        public GraphQl(RequestDelegate next, IRulEngStore rulEngStore)
        {
            _next = next;
            _rulEngStore = rulEngStore;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var sent = false;

            if (httpContext.Request.Path.StartsWithSegments("/graph"))
            {
                using (var sr = new StreamReader(httpContext.Request.Body))
                {
                    var query = await sr.ReadToEndAsync();

                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        //var schema = new Schema { Query = new BooksQuery(_rulEngStore) };

                        var result = await new DocumentExecuter()
                            .ExecuteAsync(options =>
                            {
                                //options.Schema = schema;
                                options.Query = query;
                            }).ConfigureAwait(false);

                        CheckForErrors(result);

                        await WriteResult(httpContext, result);
                        sent = true;
                    }
                }
            }

            if (!sent)
            {
                await _next(httpContext);
            }
        }

        private async Task WriteResult(HttpContext httpContext, ExecutionResult result)
        {
            var json = new DocumentWriter(indent: true).Write(result);
            httpContext.Response.StatusCode = 200;
            httpContext.Response.ContentType = "application/json";

            await httpContext.Response.WriteAsync(json);
        }

        private void CheckForErrors(ExecutionResult result)
        {
            if (!(result.Errors?.Count > 0))
            {
                return;
            }

            var errors = new List<Exception>();
            foreach (var error in result.Errors)
            {
                var ex = new Exception(error.Message);
                if (error.InnerException != null)
                {
                    ex = new Exception(error.Message, error.InnerException);
                }
                errors.Add(ex);
            }

            throw new AggregateException(errors);
        }
    }

    public static class GraphQlMiddlewareExtensions
    {
        public static IApplicationBuilder UseGraphQl(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GraphQl>();
        }
    }
}
