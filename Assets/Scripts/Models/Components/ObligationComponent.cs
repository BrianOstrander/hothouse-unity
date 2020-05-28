using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public interface IObligationModel : IEnterableModel
	{
		ObligationComponent Obligations { get; }
	}

	public class ObligationComponent : Model
	{
		#region Serialized
		[JsonProperty] Obligation[] all = new Obligation[0];
		[JsonIgnore] public ListenerProperty<Obligation[]> All { get; }
		#endregion

		public ObligationComponent()
		{
			All = new ListenerProperty<Obligation[]>(value => all = value, () => all);
		}

		public bool ContainsType(ObligationType type) => All.Value.Any(o => o.Type == type);

		public bool Remove(ObligationType type)
		{
			if (!ContainsType(type)) return false;
			All.Value = All.Value.Where(o => o.Type != type).ToArray();
			return true;
		}
	}

	public static class IObligationModelExtensions
	{
		public static IEnumerable<(IObligationModel Model, Obligation Obligation)> GetIndividualObligations(
			this IEnumerable<IObligationModel> elements
		)
		{
			return elements.SelectMany(e => e.Obligations.All.Value.Select(o => (e, o)));
		}
		
		public static IEnumerable<(IObligationModel Model, Obligation Obligation)> GetIndividualObligations(
			this IEnumerable<IObligationModel> elements,
			Func<IObligationModel, bool> predicate
		)
		{
			return elements.Where(predicate).SelectMany(e => e.Obligations.All.Value.Select(o => (e, o)));
		}
		
		public static IEnumerable<(IObligationModel Model, Obligation Obligation)> GetIndividualObligations(
			this IEnumerable<IObligationModel> elements,
			Func<Obligation, bool> predicate
		)
		{
			return elements.SelectMany(e => e.Obligations.All.Value.Where(predicate).Select(o => (e, o)));
		}
		
		public static IEnumerable<(IObligationModel Model, Obligation Obligation)> GetIndividualObligations(
			this IEnumerable<IObligationModel> elements,
			Func<IObligationModel, bool> modelPredicate,
			Func<Obligation, bool> obligationPredicate
		)
		{
			return elements.Where(modelPredicate).SelectMany(e => e.Obligations.All.Value.Where(obligationPredicate).Select(o => (e, o)));
		}
		
		public static IEnumerable<(IObligationModel Model, Obligation Obligation)> GetIndividualObligations(
			this IEnumerable<IObligationModel> elements,
			Func<(IObligationModel Model, Obligation Obligation), bool> predicate
		)
		{
			return elements.SelectMany(e => e.Obligations.All.Value.Select(o => (e, o))).Where(predicate);
		}
	}
}