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
		[JsonProperty] Obligation[] obligations = new Obligation[0];
		[JsonIgnore] public ListenerProperty<Obligation[]> Obligations { get; }
		#endregion

		public ObligationComponent()
		{
			Obligations = new ListenerProperty<Obligation[]>(value => obligations = value, () => obligations);
		}
	}

	public static class IObligationModelExtensions
	{
		public static IEnumerable<(IObligationModel Model, Obligation Obligation)> GetIndividualObligations(
			this IEnumerable<IObligationModel> elements
		)
		{
			return elements.SelectMany(e => e.Obligations.Obligations.Value.Select(o => (e, o)));
		}
		
		public static IEnumerable<(IObligationModel Model, Obligation Obligation)> GetIndividualObligations(
			this IEnumerable<IObligationModel> elements,
			Func<IObligationModel, bool> predicate
		)
		{
			return elements.Where(predicate).SelectMany(e => e.Obligations.Obligations.Value.Select(o => (e, o)));
		}
		
		public static IEnumerable<(IObligationModel Model, Obligation Obligation)> GetIndividualObligations(
			this IEnumerable<IObligationModel> elements,
			Func<Obligation, bool> predicate
		)
		{
			return elements.SelectMany(e => e.Obligations.Obligations.Value.Where(predicate).Select(o => (e, o)));
		}
		
		public static IEnumerable<(IObligationModel Model, Obligation Obligation)> GetIndividualObligations(
			this IEnumerable<IObligationModel> elements,
			Func<IObligationModel, bool> modelPredicate,
			Func<Obligation, bool> obligationPredicate
		)
		{
			return elements.Where(modelPredicate).SelectMany(e => e.Obligations.Obligations.Value.Where(obligationPredicate).Select(o => (e, o)));
		}
		
		public static IEnumerable<(IObligationModel Model, Obligation Obligation)> GetIndividualObligations(
			this IEnumerable<IObligationModel> elements,
			Func<(IObligationModel Model, Obligation Obligation), bool> predicate
		)
		{
			return elements.SelectMany(e => e.Obligations.Obligations.Value.Select(o => (e, o))).Where(predicate);
		}
	}
}