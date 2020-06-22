namespace Lunra.Hothouse.Models
{
	public abstract class ClearablePoolModel<M> : BasePrefabPoolModel<M>
		where M : ClearableModel_old, new() 
	{
		protected virtual void Reset(M model)
		{
			model.Clearable.SelectionState.Value = SelectionStates.NotSelected;
			model.Clearable.ClearancePriority.Value = null;
		}
	}
}