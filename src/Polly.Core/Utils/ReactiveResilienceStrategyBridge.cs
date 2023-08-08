﻿namespace Polly.Utils;

internal sealed class ReactiveResilienceStrategyBridge<T> : ResilienceStrategy
{
    public ReactiveResilienceStrategyBridge(ReactiveResilienceStrategy<T> strategy) => Strategy = strategy;

    public ReactiveResilienceStrategy<T> Strategy { get; }

    protected internal override ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
        Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
        ResilienceContext context,
        TState state)
    {
        // Check if we can cast directly, thus saving some cycles and improving the performance
        if (callback is Func<ResilienceContext, TState, ValueTask<Outcome<T>>> casted)
        {
            return TaskHelper.ConvertValueTask<T, TResult>(
                Strategy.ExecuteCore(casted, context, state),
                context);
        }
        else
        {
            var valueTask = Strategy.ExecuteCore(
                static async (context, state) =>
                {
                    var outcome = await state.callback(context, state.state).ConfigureAwait(context.ContinueOnCapturedContext);
                    return outcome.AsOutcome<T>();
                },
                context,
                (callback, state));

            return TaskHelper.ConvertValueTask<T, TResult>(valueTask, context);
        }
    }
}