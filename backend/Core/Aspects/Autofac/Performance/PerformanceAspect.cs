using System.Diagnostics;
using Castle.DynamicProxy;
using Core.Utilities.Interceptors;

namespace Core.Aspects.Autofac.Performance
{
    public class PerformanceAspect : MethodInterception
    {
        private readonly int _interval;
        public PerformanceAspect(int interval) => _interval = interval;

        public override void Intercept(IInvocation invocation)
        {
            var stopwatch = Stopwatch.StartNew();
            try { invocation.Proceed(); }
            finally
            {
                stopwatch.Stop();
                if (stopwatch.Elapsed.TotalSeconds > _interval)
                    Debug.WriteLine($"Performance : {invocation.Method.DeclaringType?.FullName}.{invocation.Method.Name}-->{stopwatch.Elapsed.TotalSeconds}");
            }
        }
    }
}
