namespace Dman.LSystem.SystemRuntime.ThreadBouncer
{
    public static class CompletableExtensions
    {
        public static ICompletable<T> StepNextTyped<T>(this ICompletable<T> completable)
        {
            return (ICompletable<T>)completable.StepNext();
        }
    }
}
