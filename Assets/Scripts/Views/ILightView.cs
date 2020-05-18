namespace Lunra.Hothouse.Views
{
	public interface ILightView
	{
		float LightFuelNormal { set; }
		
		bool IsLight { get; }
		float LightRange { get; }
		float LightIntensity { get; }
	}
}