namespace Lunra.Hothouse.Views
{
	public interface ILightView
	{
		float LightFuelNormal { set; }
		
		bool IsLight { get; }
		float LightRadius { get; }
		float LightIntensity { get; }
	}
}