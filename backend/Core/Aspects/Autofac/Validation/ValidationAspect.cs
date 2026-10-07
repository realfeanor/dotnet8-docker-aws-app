using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.DynamicProxy;
using Core.CrossCuttingConcerns.Validation;
using Core.Utilities.Interceptors;
using Core.Utilities.Messages;
using FluentValidation;

namespace Core.Aspects.Autofac.Validation
{
    public class ValidationAspect : MethodInterception
    {
        private Type _validatorType;
        public ValidationAspect(Type validatorType)
        {
            if (!typeof(IValidator).IsAssignableFrom(validatorType))
            {
                throw new System.Exception(AspectMessages.WrongValidationType);
            }

            _validatorType = validatorType;
        }
        protected override void OnBefore(IInvocation invocation)
        {
            var validator = Activator.CreateInstance(_validatorType) as IValidator
                ?? throw new InvalidOperationException("The validator could not be created.");
            var validatorBaseType = _validatorType.BaseType
                ?? throw new InvalidOperationException("The validator has no base type.");
            var entityType = validatorBaseType.GetGenericArguments()[0];
            if (invocation.Arguments.Select((argument, index) => new { argument, index })
                .Any(item => item.argument == null && entityType.IsAssignableFrom(invocation.Method.GetParameters()[item.index].ParameterType)))
                throw new ValidationException("Request cannot be null.");
            var entities = invocation.Arguments.Where(t => t != null && entityType.IsInstanceOfType(t));
            foreach (var entity in entities)
            {
                ValidationTool.Validate(validator, entity);
            }
        }
    }
}
