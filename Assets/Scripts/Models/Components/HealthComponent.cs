using System;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IHealthModel : IRoomTransformModel
	{
		HealthComponent Health { get; }
	}

	public class HealthComponent : ComponentModel<IHealthModel>
	{
		#region Serialized
		[JsonProperty] float current = -1f;
		ListenerProperty<float> currentListener;
		[JsonIgnore] public ReadonlyProperty<float> Current { get; }
		[JsonProperty] float maximum;
		[JsonIgnore] public ListenerProperty<float> Maximum { get; }
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public bool IsDamaged => !Mathf.Approximately(Current.Value, Maximum.Value);
		[JsonIgnore] public bool IsDestroyed => Mathf.Approximately(Current.Value, 0f) || Current.Value < 0f;
		[JsonIgnore] public float Normalized => Mathf.Approximately(0f, Maximum.Value) ? 1f : (Current.Value / Maximum.Value);

		[JsonIgnore] public Func<Damage.Request, float> GetDamageAbsorbed;
		[JsonIgnore] public Action<Damage.Result> Damaged = ActionExtensions.GetEmpty<Damage.Result>();
		#endregion

		public HealthComponent()
		{
			Current = new ReadonlyProperty<float>(
				value => current = value,
				() => current,
				out currentListener
			);
			
			Maximum = new ListenerProperty<float>(value => maximum = value, () => maximum);
		}

		public void ResetToMaximum(float? newMaximum = null)
		{
			if (newMaximum.HasValue) Maximum.Value = newMaximum.Value;
			currentListener.Value = Maximum.Value;
		}

		public Damage.Result Damage(Damage.Request request)
		{
			var damageAbsorbed = GetDamageAbsorbed?.Invoke(request) ?? request.Amount;

			var currentOld = Current.Value;
			
			var currentNew = Mathf.Clamp(
				currentOld - damageAbsorbed,
				0f,
				Maximum.Value
			);

			var damageApplied = currentOld - currentNew;
			
			var result = new Damage.Result(
				request,
				damageApplied,
				damageAbsorbed,
				Mathf.Approximately(0f, currentNew)
			);

			if (!request.Type.HasFlag(Models.Damage.Types.Query))
			{
				Damaged(result);
				currentListener.Value = currentNew;
			}

			return result;
		}

		public void Heal(float amount) => currentListener.Value = Mathf.Min(Current.Value + amount, Maximum.Value);

		public override string ToString()
		{
			var result = "Health: " + Current.Value + " / " + Maximum.Value;
			if (IsDestroyed) result += " - " + "Dead".Wrap("<color=red>", "</color>");
			return result;
		}
	}
}