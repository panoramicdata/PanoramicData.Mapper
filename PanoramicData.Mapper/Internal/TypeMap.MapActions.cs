namespace PanoramicData.Mapper.Internal;

/// <summary>
/// Runs the configured BeforeMap and AfterMap actions.
/// </summary>
public sealed partial class TypeMap
{
	private void ExecuteBeforeMapActions(object source, object destination)
	{
		foreach (var action in BeforeMapActions)
		{
			action.DynamicInvoke(source, destination);
		}

		var context = new ResolutionContext();
		InvokeMappingActionTypes(BeforeMapActionTypes, source, destination, context);
	}

	private static void InvokeMappingActionTypes(
		List<Type> actionTypes,
		object source,
		object destination,
		ResolutionContext context)
	{
		foreach (var actionType in actionTypes)
		{
			var instance = Activator.CreateInstance(actionType)
				?? throw new InvalidOperationException($"Cannot create instance of mapping action {actionType.FullName}");
			var processMethod = actionType.GetMethod("Process")
				?? throw new InvalidOperationException($"Mapping action {actionType.FullName} does not have a Process method");
			processMethod.Invoke(instance, [source, destination, context]);
		}
	}

	private void ExecuteAfterMapActions(object source, object destination)
	{
		var context = new ResolutionContext();

		foreach (var action in AfterMapActions)
		{
			action.DynamicInvoke(source, destination);
		}

		InvokeMappingActionTypes(AfterMapActionTypes, source, destination, context);
	}
}
