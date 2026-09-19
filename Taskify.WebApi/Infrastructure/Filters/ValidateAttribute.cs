using Microsoft.AspNetCore.Mvc;

namespace Taskify.WebApi.Infrastructure.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ValidateAttribute(Type type) : TypeFilterAttribute(type);