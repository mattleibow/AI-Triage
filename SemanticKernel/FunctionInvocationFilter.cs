#pragma warning disable SKEXP0110 // Suppress experimental warnings
#pragma warning disable SKEXP0001 // Suppress experimental warnings

using Microsoft.SemanticKernel;

public class FunctionInvocationFilter : IFunctionInvocationFilter, IAutoFunctionInvocationFilter, IPromptRenderFilter
{
    public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        Console.WriteLine($"OnAutoFunctionInvocationAsync: {context.Function.PluginName}.{context.Function.Name}");

        await next(context);
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Console.WriteLine($"OnFunctionInvocationAsync: {context.Function.PluginName}.{context.Function.Name}");

        await next(context);
    }

    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        Console.WriteLine($"OnPromptRenderAsync: {context.Function.PluginName}.{context.Function.Name}");

        await next(context);
    }
}
