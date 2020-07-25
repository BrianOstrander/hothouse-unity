namespace Lunra.Hothouse.Views
{
	public interface ICachableView : IPrefabView
	{
		void CalculateCachedData();
	}
}