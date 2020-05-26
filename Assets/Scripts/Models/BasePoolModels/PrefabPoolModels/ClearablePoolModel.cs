namespace Lunra.Hothouse.Models
{
	public abstract class ClearablePoolModel<M> : BasePrefabPoolModel<M>
		where M : ClearableModel, new() 
	{
		protected virtual void Reset(M model)
		{
			model.SelectionState.Value = SelectionStates.NotSelected;
			model.ClearancePriority.Value = null;
		}
	}
}