using Microsoft.EntityFrameworkCore;

namespace api.Middleware
{
    public class DbTransactionMiddleware<TDbContext> where TDbContext : DbContext
    {
        private readonly RequestDelegate next;
        public DbTransactionMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext httpContext, TDbContext dbContext)
        {
            string requestMethod = httpContext.Request.Method;

            if (HttpMethods.IsPost(requestMethod) || HttpMethods.ISPut(requestMethod) || HttpMethods.IsDelete(requestMethod))
            {
                var strategy = dbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync<object, object>(null!, operation: async (dbctx, state, cancellationToken) =>
                {
                    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                    await next(httpContext);
                    await transaction.CommitAsync(cancellationToken);
                    return null!;
                }, null!);
            }
            else
            {
                await next(httpContext);
            }
        }
    }

    public static class DbTransactionMiddlewareExtensions
    {
        public static IApplicationBuilder UseDbTransaction<TDbContext>(this IApplicationBuilder builder) where TDbContext : DbContext
        {
            return builder.UseMiddleware<DbTransactionMiddleware<TDbContext>>();
        }
    }
}